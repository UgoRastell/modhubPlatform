using FileService.Models;
using FileService.Settings;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileService.Repositories
{
    public class ScanResultRepository : IScanResultRepository
    {
        private readonly IMongoCollection<ScanResult> _scanResults;
        private readonly ILogger<ScanResultRepository> _logger;

        public ScanResultRepository(MongoDbSettings settings, ILogger<ScanResultRepository> logger)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _scanResults = database.GetCollection<ScanResult>(settings.ScanResultsCollectionName);
            _logger = logger;

            // Create indexes
            CreateIndexes();
        }

        /// <summary>
        /// Creates indexes for the collection to improve query performance
        /// </summary>
        private void CreateIndexes()
        {
            try
            {
                // Index for faster lookup by file metadata ID
                var fileIdBuilder = Builders<ScanResult>.IndexKeys.Ascending(s => s.FileMetadataId);
                _scanResults.Indexes.CreateOne(new CreateIndexModel<ScanResult>(fileIdBuilder));

                // Index for faster queries by scan status
                var statusBuilder = Builders<ScanResult>.IndexKeys.Ascending(s => s.Status);
                _scanResults.Indexes.CreateOne(new CreateIndexModel<ScanResult>(statusBuilder));

                // Combined index for file ID and scan status (common query)
                var fileStatusBuilder = Builders<ScanResult>.IndexKeys
                    .Ascending(s => s.FileMetadataId)
                    .Ascending(s => s.Status);
                _scanResults.Indexes.CreateOne(new CreateIndexModel<ScanResult>(fileStatusBuilder));

                _logger.LogInformation("Scan results indexes created or verified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scan results indexes");
            }
        }

        /// <summary>
        /// Gets a scan result by its ID
        /// </summary>
        public async Task<ScanResult> GetByIdAsync(string id)
        {
            try
            {
                return await _scanResults.Find(scan => scan.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan result with ID {id}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets the latest scan result for a file
        /// </summary>
        public async Task<ScanResult> GetLatestForFileAsync(string fileMetadataId)
        {
            try
            {
                return await _scanResults
                    .Find(scan => scan.FileMetadataId == fileMetadataId)
                    .SortByDescending(scan => scan.ScanDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest scan result for file {fileId}", fileMetadataId);
                throw;
            }
        }

        /// <summary>
        /// Gets all scan results for a file
        /// </summary>
        public async Task<IEnumerable<ScanResult>> GetHistoryForFileAsync(string fileMetadataId)
        {
            try
            {
                return await _scanResults
                    .Find(scan => scan.FileMetadataId == fileMetadataId)
                    .SortByDescending(scan => scan.ScanDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan history for file {fileId}", fileMetadataId);
                throw;
            }
        }

        /// <summary>
        /// Creates a new scan result
        /// </summary>
        public async Task<string> CreateAsync(ScanResult scanResult)
        {
            try
            {
                await _scanResults.InsertOneAsync(scanResult);
                _logger.LogInformation("Created scan result with ID {id} for file {fileId}", 
                    scanResult.Id, scanResult.FileMetadataId);
                return scanResult.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scan result for file {fileId}", scanResult.FileMetadataId);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing scan result
        /// </summary>
        public async Task<bool> UpdateAsync(ScanResult scanResult)
        {
            try
            {
                var result = await _scanResults.ReplaceOneAsync(scan => scan.Id == scanResult.Id, scanResult);
                var success = result.IsAcknowledged && result.ModifiedCount > 0;
                
                if (success)
                {
                    _logger.LogInformation("Updated scan result with ID {id}", scanResult.Id);
                }
                else if (result.ModifiedCount == 0)
                {
                    _logger.LogWarning("No changes made to scan result with ID {id}", scanResult.Id);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating scan result with ID {id}", scanResult.Id);
                throw;
            }
        }

        /// <summary>
        /// Updates the status of a scan result
        /// </summary>
        public async Task<bool> UpdateStatusAsync(string id, ScanStatus status, string message = null)
        {
            try
            {
                var update = Builders<ScanResult>.Update
                    .Set(s => s.Status, status)
                    .Set(s => s.LastUpdated, DateTime.UtcNow);
                
                if (message != null)
                {
                    update = update.Set(s => s.StatusMessage, message);
                }
                
                var result = await _scanResults.UpdateOneAsync(scan => scan.Id == id, update);
                var success = result.IsAcknowledged && result.ModifiedCount > 0;
                
                if (success)
                {
                    _logger.LogInformation("Updated scan result status to {status} for ID {id}", status, id);
                }
                else
                {
                    _logger.LogWarning("Failed to update scan result status for ID {id}", id);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating scan result status for ID {id}", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a scan result
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var result = await _scanResults.DeleteOneAsync(scan => scan.Id == id);
                var success = result.IsAcknowledged && result.DeletedCount > 0;
                
                if (success)
                {
                    _logger.LogInformation("Deleted scan result with ID {id}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete scan result with ID {id}", id);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting scan result with ID {id}", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes all scan results for a file
        /// </summary>
        public async Task<long> DeleteByFileIdAsync(string fileMetadataId)
        {
            try
            {
                var result = await _scanResults.DeleteManyAsync(scan => scan.FileMetadataId == fileMetadataId);
                
                if (result.DeletedCount > 0)
                {
                    _logger.LogInformation("Deleted {count} scan results for file {fileId}", 
                        result.DeletedCount, fileMetadataId);
                }
                else
                {
                    _logger.LogInformation("No scan results found for file {fileId}", fileMetadataId);
                }
                
                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting scan results for file {fileId}", fileMetadataId);
                throw;
            }
        }

        /// <summary>
        /// Gets all files with a specific scan status
        /// </summary>
        public async Task<IEnumerable<ScanResult>> GetByStatusAsync(ScanStatus status)
        {
            try
            {
                return await _scanResults
                    .Find(scan => scan.Status == status)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan results with status {status}", status);
                throw;
            }
        }

        /// <summary>
        /// Gets all files with detected threats
        /// </summary>
        public async Task<IEnumerable<ScanResult>> GetFilesWithThreatsAsync()
        {
            try
            {
                return await _scanResults
                    .Find(scan => scan.Status == ScanStatus.Infected)
                    .SortByDescending(scan => scan.ScanDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan results with threats");
                throw;
            }
        }

        /// <summary>
        /// Gets statistics for scan results
        /// </summary>
        public async Task<ScanResultStatistics> GetScanStatisticsAsync()
        {
            try
            {
                var stats = new ScanResultStatistics();
                
                // Count files by status
                stats.CleanCount = await _scanResults.CountDocumentsAsync(s => s.Status == ScanStatus.Clean);
                stats.InfectedCount = await _scanResults.CountDocumentsAsync(s => s.Status == ScanStatus.Infected);
                stats.PendingCount = await _scanResults.CountDocumentsAsync(s => s.Status == ScanStatus.Pending);
                stats.FailedCount = await _scanResults.CountDocumentsAsync(s => s.Status == ScanStatus.Error);
                
                // Get latest scan date
                var latestScan = await _scanResults
                    .Find(_ => true)
                    .SortByDescending(s => s.ScanDate)
                    .Limit(1)
                    .FirstOrDefaultAsync();
                
                stats.LatestScanDate = latestScan?.ScanDate;
                stats.TotalScansCount = await _scanResults.CountDocumentsAsync(_ => true);
                
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan result statistics");
                throw;
            }
        }
    }

    /// <summary>
    /// Statistics for scan results
    /// </summary>
    public class ScanResultStatistics
    {
        public long CleanCount { get; set; }
        public long InfectedCount { get; set; }
        public long PendingCount { get; set; }
        public long FailedCount { get; set; }
        public DateTime? LatestScanDate { get; set; }
        public long TotalScansCount { get; set; }
    }
}
