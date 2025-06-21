using FileService.Models;
using FileService.Settings;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace FileService.Repositories
{
    public class FileMetadataRepository : IFileMetadataRepository
    {
        private readonly IMongoCollection<FileMetadata> _fileMetadata;
        private readonly ILogger<FileMetadataRepository> _logger;

        public FileMetadataRepository(MongoDbSettings settings, ILogger<FileMetadataRepository> logger)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _fileMetadata = database.GetCollection<FileMetadata>(settings.FileMetadataCollectionName);
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
                // Index for faster uploads queries by userId
                var userIdBuilder = Builders<FileMetadata>.IndexKeys.Ascending(f => f.UploadedBy);
                _fileMetadata.Indexes.CreateOne(new CreateIndexModel<FileMetadata>(userIdBuilder));

                // Index for faster entity id queries (for files attached to mods, profiles, etc)
                var entityIdBuilder = Builders<FileMetadata>.IndexKeys.Ascending(f => f.EntityId);
                _fileMetadata.Indexes.CreateOne(new CreateIndexModel<FileMetadata>(entityIdBuilder));
                
                // Index for filename search (using text index)
                var filenameBuilder = Builders<FileMetadata>.IndexKeys.Text(f => f.FileName);
                _fileMetadata.Indexes.CreateOne(new CreateIndexModel<FileMetadata>(filenameBuilder));
                
                // Combined index for content type and public status (common filtering)
                var contentTypePublicBuilder = Builders<FileMetadata>.IndexKeys
                    .Ascending(f => f.ContentType)
                    .Ascending(f => f.IsPublic);
                _fileMetadata.Indexes.CreateOne(new CreateIndexModel<FileMetadata>(contentTypePublicBuilder));
                
                _logger.LogInformation("File metadata indexes created or verified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file metadata indexes");
            }
        }

        /// <summary>
        /// Gets all file metadata items
        /// </summary>
        public async Task<IEnumerable<FileMetadata>> GetAllAsync()
        {
            try
            {
                return await _fileMetadata.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all file metadata");
                throw;
            }
        }

        /// <summary>
        /// Gets a file metadata item by its ID
        /// </summary>
        public async Task<FileMetadata> GetByIdAsync(string id)
        {
            try
            {
                return await _fileMetadata.Find(file => file.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file metadata with ID {id}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets all file metadata items for a user
        /// </summary>
        public async Task<IEnumerable<FileMetadata>> GetByUserIdAsync(string userId)
        {
            try
            {
                return await _fileMetadata.Find(file => file.UploadedBy == userId).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files uploaded by user {userId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets all file metadata items for an entity (mod, profile, etc)
        /// </summary>
        public async Task<IEnumerable<FileMetadata>> GetByEntityIdAsync(string entityId)
        {
            try
            {
                return await _fileMetadata.Find(file => file.EntityId == entityId).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for entity {entityId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// Gets all file metadata items that match the specified filter
        /// </summary>
        public async Task<IEnumerable<FileMetadata>> FindAsync(Expression<Func<FileMetadata, bool>> filter)
        {
            try
            {
                return await _fileMetadata.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding files with custom filter");
                throw;
            }
        }

        /// <summary>
        /// Creates a new file metadata item
        /// </summary>
        public async Task<string> CreateAsync(FileMetadata fileMetadata)
        {
            try
            {
                await _fileMetadata.InsertOneAsync(fileMetadata);
                _logger.LogInformation("Created file metadata with ID {id}", fileMetadata.Id);
                return fileMetadata.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file metadata for {filename}", fileMetadata.FileName);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing file metadata item
        /// </summary>
        public async Task<bool> UpdateAsync(FileMetadata fileMetadata)
        {
            try
            {
                var result = await _fileMetadata.ReplaceOneAsync(file => file.Id == fileMetadata.Id, fileMetadata);
                var success = result.IsAcknowledged && result.ModifiedCount > 0;
                
                if (success)
                {
                    _logger.LogInformation("Updated file metadata with ID {id}", fileMetadata.Id);
                }
                else if (result.ModifiedCount == 0)
                {
                    _logger.LogWarning("No changes made to file metadata with ID {id}", fileMetadata.Id);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file metadata with ID {id}", fileMetadata.Id);
                throw;
            }
        }

        /// <summary>
        /// Updates specific fields of a file metadata item
        /// </summary>
        public async Task<bool> UpdateFieldsAsync(string id, UpdateDefinition<FileMetadata> updateDefinition)
        {
            try
            {
                var result = await _fileMetadata.UpdateOneAsync(file => file.Id == id, updateDefinition);
                var success = result.IsAcknowledged && result.ModifiedCount > 0;
                
                if (success)
                {
                    _logger.LogInformation("Updated fields for file metadata with ID {id}", id);
                }
                else if (result.ModifiedCount == 0)
                {
                    _logger.LogWarning("No changes made to file metadata fields with ID {id}", id);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fields for file metadata with ID {id}", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a file metadata item
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var result = await _fileMetadata.DeleteOneAsync(file => file.Id == id);
                var success = result.IsAcknowledged && result.DeletedCount > 0;
                
                if (success)
                {
                    _logger.LogInformation("Deleted file metadata with ID {id}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete file metadata with ID {id}", id);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file metadata with ID {id}", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes all file metadata items for a user
        /// </summary>
        public async Task<long> DeleteByUserIdAsync(string userId)
        {
            try
            {
                var result = await _fileMetadata.DeleteManyAsync(file => file.UploadedBy == userId);
                
                if (result.DeletedCount > 0)
                {
                    _logger.LogInformation("Deleted {count} file metadata records for user {userId}", 
                        result.DeletedCount, userId);
                }
                else
                {
                    _logger.LogInformation("No file metadata found for user {userId}", userId);
                }
                
                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file metadata for user {userId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Deletes all file metadata items for an entity
        /// </summary>
        public async Task<long> DeleteByEntityIdAsync(string entityId)
        {
            try
            {
                var result = await _fileMetadata.DeleteManyAsync(file => file.EntityId == entityId);
                
                if (result.DeletedCount > 0)
                {
                    _logger.LogInformation("Deleted {count} file metadata records for entity {entityId}", 
                        result.DeletedCount, entityId);
                }
                else
                {
                    _logger.LogInformation("No file metadata found for entity {entityId}", entityId);
                }
                
                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file metadata for entity {entityId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// Searches for file metadata items by filename or content
        /// </summary>
        public async Task<IEnumerable<FileMetadata>> SearchAsync(string searchText, bool publicOnly = false)
        {
            try
            {
                var textFilter = Builders<FileMetadata>.Filter.Text(searchText);
                var filter = publicOnly 
                    ? Builders<FileMetadata>.Filter.And(textFilter, Builders<FileMetadata>.Filter.Eq(f => f.IsPublic, true))
                    : textFilter;
                
                return await _fileMetadata.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for file metadata with text: {searchText}", searchText);
                throw;
            }
        }

        /// <summary>
        /// Gets paged file metadata items for a user
        /// </summary>
        public async Task<(IEnumerable<FileMetadata> Items, long TotalCount)> GetPagedByUserIdAsync(
            string userId, int page, int pageSize, string sortBy = "UploadedAt", bool ascending = false)
        {
            try
            {
                var filter = Builders<FileMetadata>.Filter.Eq(f => f.UploadedBy, userId);
                var totalCount = await _fileMetadata.CountDocumentsAsync(filter);
                
                var sortDefinition = ascending 
                    ? Builders<FileMetadata>.Sort.Ascending(sortBy) 
                    : Builders<FileMetadata>.Sort.Descending(sortBy);
                
                var files = await _fileMetadata.Find(filter)
                    .Sort(sortDefinition)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();
                
                return (files, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged files for user {userId}", userId);
                throw;
            }
        }
        
        /// <summary>
        /// Gets file metadata items by their IDs
        /// </summary>
        public async Task<IEnumerable<FileMetadata>> GetByIdsAsync(IEnumerable<string> ids)
        {
            try
            {
                var filter = Builders<FileMetadata>.Filter.In(f => f.Id, ids);
                return await _fileMetadata.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file metadata by batch IDs");
                throw;
            }
        }

        /// <summary>
        /// Anonymizes file ownership for a user
        /// </summary>
        public async Task<long> AnonymizeUserFilesAsync(string userId)
        {
            try
            {
                var update = Builders<FileMetadata>.Update
                    .Set(f => f.UploadedBy, "anonymous")
                    .Set(f => f.UploadedByUsername, "Anonymous User")
                    .Set("Metadata.anonymized", "true")
                    .CurrentDate("Metadata.anonymized_at");
                
                var result = await _fileMetadata.UpdateManyAsync(
                    f => f.UploadedBy == userId, 
                    update);
                
                _logger.LogInformation("Anonymized {count} files for user {userId}", 
                    result.ModifiedCount, userId);
                
                return result.ModifiedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error anonymizing files for user {userId}", userId);
                throw;
            }
        }
    }
}
