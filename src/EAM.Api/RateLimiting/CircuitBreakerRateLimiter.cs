using System.Threading.RateLimiting;
using Polly;
using Polly.CircuitBreaker;

namespace EAM.Api.RateLimiting;

/// <summary>
/// Wraps a Redis-backed primary limiter with a Polly circuit breaker. When Redis is
/// healthy (circuit CLOSED) requests flow through the primary; when Redis is down
/// (circuit OPEN) requests fall back to the in-memory limiter with the same per-key
/// limits — fail-closed rather than fail-open.
///
/// Circuit: 3 consecutive Redis failures → OPEN for 10s → HALF-OPEN → CLOSED or OPEN.
/// </summary>
internal sealed class CircuitBreakerRateLimiter : RateLimiter
{
    private readonly RateLimiter _primary;
    private readonly RateLimiter _fallback;
    private readonly ResiliencePipeline _pipeline;

    internal CircuitBreakerRateLimiter(RateLimiter primary, RateLimiter fallback, ResiliencePipeline pipeline)
    {
        _primary = primary;
        _fallback = fallback;
        _pipeline = pipeline;
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics? GetStatistics() => _primary.GetStatistics();

    // Sync path not supported by the Redis limiter — use the in-memory fallback.
    protected override RateLimitLease AttemptAcquireCore(int permitCount)
        => _fallback.AttemptAcquire(permitCount);

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        try
        {
            RateLimitLease? lease = null;
            await _pipeline.ExecuteAsync(async token =>
            {
                lease = await _primary.AcquireAsync(permitCount, token);
            }, cancellationToken);
            return lease!;
        }
        catch (BrokenCircuitException)
        {
            // Circuit OPEN — Redis is known-down. Use the in-memory fallback.
            return await _fallback.AcquireAsync(permitCount, cancellationToken);
        }
        catch (StackExchange.Redis.RedisException)
        {
            // Circuit CLOSED but this call failed — fall back (Polly counted the failure).
            return await _fallback.AcquireAsync(permitCount, cancellationToken);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _primary.Dispose();
            _fallback.Dispose();
        }
    }
}
