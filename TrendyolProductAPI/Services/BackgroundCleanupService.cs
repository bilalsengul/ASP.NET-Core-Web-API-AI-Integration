using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TrendyolProductAPI.Services
{
    public class BackgroundCleanupService : BackgroundService
    {
        private readonly ILogger<BackgroundCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public BackgroundCleanupService(
            ILogger<BackgroundCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Force garbage collection periodically to clean up any lingering WebDriver instances
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    _logger.LogInformation("Cleanup service ran at: {time}", DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while running cleanup service");
                }

                // Run cleanup every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
} 