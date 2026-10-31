using Campus.Canteen.Payments.Persistence;
using Campus.Canteen.Payments.Tracing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Режим колонок суммы на время релиза v1.9.0 (см. docs/adr/0009-canteen-check-amount.md)
var mode = builder.Configuration.GetValue("CanteenMigration:AmountMode", AmountColumnMode.DualWrite);
var connectionString = builder.Configuration.GetConnectionString("Payments");

builder.Services.AddSingleton(typeof(AmountColumnMode), mode);
builder.Services.AddDbContext<PaymentsDbContext>(o =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
        o.UseInMemoryDatabase("canteen");          // локальная разработка и sanity-тест в памяти
    else
        o.UseNpgsql(connectionString);             // боевой PostgreSQL
    o.ReplaceService<IModelCacheKeyFactory, AmountModeCacheKeyFactory>();
});

builder.Services.AddControllers();

// OpenTelemetry: HTTP-спаны ASP.NET Core + доменные спаны столовой
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("campus-canteen-payments", serviceVersion: "1.9.0"))
    .WithTracing(t =>
    {
        t.AddSource(CampusTracing.SourceName).AddAspNetCoreInstrumentation();
        if (builder.Configuration.GetValue("Tracing:Console", false))
            t.AddConsoleExporter();
    });

var app = builder.Build();
app.Logger.LogInformation("Campus.Canteen.Payments v1.9.0, режим суммы: {Mode}, БД: {Db}",
    mode, string.IsNullOrWhiteSpace(connectionString) ? "InMemory" : "PostgreSQL");

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", version = "1.9.0", amountMode = mode.ToString() }));
app.Run();

public partial class Program;
