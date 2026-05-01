using WarehouseIntegrationAPI.Services;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register MessageProducer as Singleton
builder.Services.AddSingleton<MessageProducer>();

// Register IntegrationWorker as a Hosted Service
builder.Services.AddHostedService<IntegrationWorker>();

var app = builder.Build();

app.UseRouting();
app.UseHttpMetrics(); // Prometheus HTTP metrics

// Minimal setup for prometheus endpoint
app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics(); // Expose /metrics
    endpoints.MapControllers();
});

app.Run();
