using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Visa2026.Blazor.Server.Services {
    public class TempFileCleanupService : BackgroundService {
        private readonly ILogger<TempFileCleanupService> logger;
        private readonly TimeSpan cleanupInterval = TimeSpan.FromHours(24);
        private readonly TimeSpan fileRetentionPeriod = TimeSpan.FromDays(2);

        public TempFileCleanupService(ILogger<TempFileCleanupService> logger) {
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            logger.LogInformation("TempFileCleanupService is starting.");

            while(!stoppingToken.IsCancellationRequested) {
                try {
                    CleanTempDirectory();
                }
                catch(Exception ex) {
                    logger.LogError(ex, "Error occurred while cleaning temporary files.");
                }

                // Wait for 24 hours before running again
                await Task.Delay(cleanupInterval, stoppingToken);
            }
        }

        private void CleanTempDirectory() {
            string tempPath = Path.GetTempPath();
            var directoryInfo = new DirectoryInfo(tempPath);

            if(directoryInfo.Exists) {
                var expirationDate = DateTime.Now.Subtract(fileRetentionPeriod);
                var filesToDelete = directoryInfo.GetFiles().Where(f => f.LastWriteTime < expirationDate);

                foreach(var file in filesToDelete) {
                    try {
                        file.Delete();
                        logger.LogInformation($"Deleted old temp file: {file.Name}");
                    }
                    catch(Exception) {
                        // Ignore errors (file might be in use)
                    }
                }
            }
        }
    }
}