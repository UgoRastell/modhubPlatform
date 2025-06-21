using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FileService.Services
{
    /// <summary>
    /// Thread-safe in-memory implementation of the file processing queue
    /// </summary>
    public class InMemoryFileProcessingQueue : IFileProcessingQueue
    {
        private readonly ConcurrentQueue<FileProcessingItem> _processingQueue = new ConcurrentQueue<FileProcessingItem>();
        private readonly SemaphoreSlim _queueSemaphore = new SemaphoreSlim(0);
        private readonly ILogger<InMemoryFileProcessingQueue> _logger;

        public InMemoryFileProcessingQueue(ILogger<InMemoryFileProcessingQueue> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds a file to the processing queue
        /// </summary>
        public Task EnqueueFileForProcessingAsync(FileProcessingItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _logger.LogInformation("Enqueuing {operation} for file ID {fileId}", 
                item.Operation, item.FileMetadataId);
            
            _processingQueue.Enqueue(item);
            _queueSemaphore.Release();
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the next file from the processing queue
        /// </summary>
        public async Task<FileProcessingItem> DequeueAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Wait for an item to be available or until cancellation
                await _queueSemaphore.WaitAsync(cancellationToken);
                
                if (_processingQueue.TryDequeue(out var item))
                {
                    _logger.LogDebug("Dequeued {operation} for file ID {fileId}", 
                        item.Operation, item.FileMetadataId);
                    return item;
                }
                
                // This should not happen since the semaphore counts match the queue size
                _logger.LogWarning("Queue semaphore released but no item available");
                return null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Dequeue operation cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dequeuing file processing item");
                throw;
            }
        }

        /// <summary>
        /// Gets the current size of the processing queue
        /// </summary>
        public int GetQueueSize()
        {
            return _processingQueue.Count;
        }

        /// <summary>
        /// Gets all pending items in the processing queue without removing them
        /// </summary>
        public Task<IEnumerable<FileProcessingItem>> GetPendingItemsAsync()
        {
            // Create a snapshot of the queue
            var items = _processingQueue.ToArray();
            return Task.FromResult<IEnumerable<FileProcessingItem>>(items);
        }
    }
}
