﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Visa2026.Blazor.Server.Services {
    public class TempFileCleanupSettings
    {
        public bool Enabled { get; set; } = true;
        public int CleanupIntervalHours { get; set; } = 24;
        public int FileRetentionDays { get; set; } = 2;
    }

    public class TempFileCleanupService : BackgroundService {
        private readonly ILogger<TempFileCleanupService> logger;
        private readonly TimeSpan cleanupInterval;
        private readonly TimeSpan fileRetentionPeriod;

        public TempFileCleanupService(ILogger<TempFileCleanupService> logger, IOptions<TempFileCleanupSettings> settingsOptions) {
            this.logger = logger;
            var settings = settingsOptions.Value;
            this.cleanupInterval = TimeSpan.FromHours(settings.CleanupIntervalHours);
            this.fileRetentionPeriod = TimeSpan.FromDays(settings.FileRetentionDays);
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
                int count = 0;
                foreach(var file in filesToDelete) {
                    try {
                        file.Delete();
                        logger.LogInformation($"Deleted old temp file: {file.Name}");
                        count++;
                    }
                    catch(Exception) {
                        // Ignore errors (file might be in use)
                    }
                }
                logger.LogInformation($"Temporary file cleanup job finished. Deleted {count} files.");
            }
        }
    }
}