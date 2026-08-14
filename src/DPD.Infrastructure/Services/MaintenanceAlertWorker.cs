using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DPD.Infrastructure.Services;

public sealed class MaintenanceAlertWorker : BackgroundService
{
    private readonly ILogger<MaintenanceAlertWorker> _logger;

    public MaintenanceAlertWorker(ILogger<MaintenanceAlertWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Background maintenance/capacity check executed at {Timestamp}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
