using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using EAM.Api.RateLimiting;
using EAM.Application.Common;
using Polly;
using Polly.CircuitBreaker;
using RedisRateLimiting;
using StackExchange.Redis;

namespace EAM.Api.Extensions;

/// <summary>
/// Multi-tier rate limiting: sliding windows for auth endpoints, a role-tiered token
/// bucket for /api/*, Redis-backed when available (with a Polly circuit breaker falling
/// back to in-process limiters), and a standardised 429 envelope. Degrades to pure
/// in-process limiting when Redis isn't configured.
/// </summary>
public static class RateLimiterExtensions
{
    public const string AuthLoginPolicy = "AuthLoginPolicy";
    public const string AuthMfaPolicy = "AuthMfaPolicy";
    public const string ApiTieredPolicy = "ApiTieredPolicy";

    private static readonly HashSet<string> AdminRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ServiceAdmin", "TenantUserAdmin", "ServiceSupportAdmin"
    };

    public static IServiceCollection AddCustomRateLimiter(this IServiceCollection services)
    {
        // IConnectionMultiplexer is registered by Infrastructure DI only when a Redis
        // connection string exists. When absent, GetService returns null and every policy
        // degrades to an in-process limiter so the app still boots.
        var sp = services.BuildServiceProvider();
        var redis = sp.GetService<IConnectionMultiplexer>();

        // Shared circuit breaker: trips after 3 consecutive Redis failures, opens 10s.
        var circuitPipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<RedisConnectionException>()
                    .Handle<RedisTimeoutException>()
                    .Handle<RedisException>(),
                FailureRatio = 1.0,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(10)
            })
            .Build();

        static SlidingWindowRateLimiter LocalSlidingWindow(int permit, int windowSeconds) =>
            new(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                AutoReplenishment = true
            });

        static TokenBucketRateLimiter LocalTokenBucket(int tokenLimit, int tokensPerSecond) =>
            new(new TokenBucketRateLimiterOptions
            {
                TokenLimit = tokenLimit,
                TokensPerPeriod = tokensPerSecond,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // /auth/login — 5 req / 30s per IP.
            options.AddPolicy(AuthLoginPolicy, ctx =>
            {
                var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var key = $"rate:login:{ip}";
                return RateLimitPartition.Get(key, k => redis is null
                    ? (RateLimiter)LocalSlidingWindow(5, 30)
                    : new CircuitBreakerRateLimiter(
                        primary: new RedisSlidingWindowRateLimiter<string>(k, new RedisSlidingWindowRateLimiterOptions
                        {
                            ConnectionMultiplexerFactory = () => redis!,
                            PermitLimit = 5,
                            Window = TimeSpan.FromSeconds(30)
                        }),
                        fallback: LocalSlidingWindow(5, 30),
                        pipeline: circuitPipeline));
            });

            // /auth/mfa/verify — 3 req / 60s per IP.
            options.AddPolicy(AuthMfaPolicy, ctx =>
            {
                var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var key = $"rate:mfa:{ip}";
                return RateLimitPartition.Get(key, k => redis is null
                    ? (RateLimiter)LocalSlidingWindow(3, 60)
                    : new CircuitBreakerRateLimiter(
                        primary: new RedisSlidingWindowRateLimiter<string>(k, new RedisSlidingWindowRateLimiterOptions
                        {
                            ConnectionMultiplexerFactory = () => redis!,
                            PermitLimit = 3,
                            Window = TimeSpan.FromSeconds(60)
                        }),
                        fallback: LocalSlidingWindow(3, 60),
                        pipeline: circuitPipeline));
            });

            // /api/* — token bucket tiered by JWT role (anon < user < admin).
            options.AddPolicy(ApiTieredPolicy, ctx =>
            {
                var user = ctx.User;
                string key;
                long tokenLimit;
                int tokensPerSecond;

                if (user.Identity?.IsAuthenticated == true)
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? user.FindFirstValue("sub")
                              ?? "unknown";

                    var isAdmin = user.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Any(c => AdminRoles.Contains(c.Value));

                    if (isAdmin) { key = $"rate:api:admin:{userId}"; tokenLimit = 30; tokensPerSecond = 5; }
                    else { key = $"rate:api:user:{userId}"; tokenLimit = 10; tokensPerSecond = 2; }
                }
                else
                {
                    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    key = $"rate:api:anon:{ip}";
                    tokenLimit = 5;
                    tokensPerSecond = 1;
                }

                return RateLimitPartition.Get(key, k => redis is null
                    ? (RateLimiter)LocalTokenBucket((int)tokenLimit, tokensPerSecond)
                    : new CircuitBreakerRateLimiter(
                        primary: new RedisTokenBucketRateLimiter<string>(k, new RedisTokenBucketRateLimiterOptions
                        {
                            ConnectionMultiplexerFactory = () => redis!,
                            TokenLimit = (int)tokenLimit,
                            TokensPerPeriod = tokensPerSecond,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(1)
                        }),
                        fallback: LocalTokenBucket((int)tokenLimit, tokensPerSecond),
                        pipeline: circuitPipeline));
            });

            // Standardised 429 — project envelope + RFC rate-limit headers.
            options.OnRejected = async (context, ct) =>
            {
                var response = context.HttpContext.Response;
                var lease = context.Lease;

                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.ContentType = "application/json";

                int retrySeconds;
                if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retrySeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                }
                else
                {
                    var policy = context.HttpContext.GetEndpoint()?.Metadata
                        .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                    retrySeconds = policy switch
                    {
                        AuthLoginPolicy => 30,
                        AuthMfaPolicy => 60,
                        _ => 1,
                    };
                }

                response.Headers["Retry-After"] = retrySeconds.ToString();
                response.Headers["RateLimit-Reset"] = retrySeconds.ToString();
                response.Headers["RateLimit-Remaining"] = "0";

                var traceId = context.HttpContext.Items["CorrelationId"]?.ToString();
                var body = ApiResponse<object>.Fail(
                    new ErrorResponse
                    {
                        Code = "rate_limited",
                        Message = "Too many requests. Please slow down and retry later.",
                        TraceId = traceId
                    },
                    traceId);

                await response.WriteAsync(
                    JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                    ct);
            };
        });

        return services;
    }
}
