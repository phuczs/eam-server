using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using EAM.Api.Extensions;
using EAM.Application;
using EAM.Infrastructure;
using EAM.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Integration tests run under the "Testing" environment and supply their own config.
// The rate limiter is skipped there so tests aren't throttled.
var isTesting = builder.Environment.IsEnvironment("Testing");

// ---- Logging ----
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ---- Layered DI (all wiring centralised in the *.AddXxx() extension methods) ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices();

// ---- AuthN/Z ----
builder.Services.AddJwtAuthentication(builder.Configuration);

// ---- CORS ----
const string SpaCors = "spa";
builder.Services.AddCorsSetup(builder.Configuration, SpaCors);

// ---- Rate limiting (Redis-backed w/ circuit breaker; degrades to in-process) ----
if (!isTesting)
{
    builder.Services.AddCustomRateLimiter();
}

// ---- Swagger ----
builder.Services.AddSwaggerSetup();

var app = builder.Build();

// ---- Dev: seed placeholder data + Swagger UI ----
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EamDbContext>();
    await DbSeeder.SeedAsync(db);

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(SpaCors);

// ---- Cross-cutting pipeline: perf → correlation → request log → exception ----
app.UseEamObservability();

app.UseAuthentication();

if (!isTesting)
{
    app.UseRateLimiter();
}

// Defence-in-depth role gate for admin route prefixes.
app.UseSecurity();

app.UseAuthorization();

// No controllers in the skeleton yet — MapControllers maps zero routes for now.
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// Exposed so WebApplicationFactory<Program> can boot the host in integration tests.
public partial class Program { }
