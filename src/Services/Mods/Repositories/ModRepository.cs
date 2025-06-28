using ModsService.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModsService.Repositories
{
    public class ModRepository : IModRepository
    {
        private readonly IMongoCollection<Mod> _modsCollection;
        private readonly ILogger<ModRepository> _logger;

        public ModRepository(MongoDbSettings settings, ILogger<ModRepository> logger)
        {
            _logger = logger;
            try
            {
                var mongoClient = new MongoClient(settings.ConnectionString);
                var database = mongoClient.GetDatabase(settings.DatabaseName);
                _modsCollection = database.GetCollection<Mod>(settings.ModsCollectionName);
                
                // Créer des index pour optimiser les requêtes courantes
                var indexKeysName = Builders<Mod>.IndexKeys.Ascending(m => m.Name);
                var indexKeysCreator = Builders<Mod>.IndexKeys.Ascending(m => m.CreatorId);
                var indexKeysGame = Builders<Mod>.IndexKeys.Ascending(m => m.GameId);
                
                _modsCollection.Indexes.CreateOne(new CreateIndexModel<Mod>(indexKeysName));
                _modsCollection.Indexes.CreateOne(new CreateIndexModel<Mod>(indexKeysCreator));
                _modsCollection.Indexes.CreateOne(new CreateIndexModel<Mod>(indexKeysGame));
                
                _logger.LogInformation("Connection à MongoDB établie avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation de MongoDB");
                throw;
            }
        }

        public async Task<List<Mod>> GetAllAsync(int page = 1, int pageSize = 50, string sortBy = "recent")
        {
            try
            {
                var filter = Builders<Mod>.Filter.Empty;
                var sort = GetSortDefinition(sortBy);
                
                return await _modsCollection
                    .Find(filter)
                    .Sort(sort)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des mods");
                return new List<Mod>();
            }
        }

        public async Task<Mod> GetByIdAsync(string id)
        {
            try
            {
                return await _modsCollection.Find(m => m.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du mod {ModId}", id);
                return null;
            }
        }

        public async Task<List<Mod>> GetByCreatorIdAsync(string creatorId)
        {
            try
            {
                return await _modsCollection.Find(m => m.CreatorId == creatorId).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des mods du créateur {CreatorId}", creatorId);
                return new List<Mod>();
            }
        }

        public async Task<int> GetTotalCountAsync()
        {
            try
            {
                return (int)await _modsCollection.CountDocumentsAsync(Builders<Mod>.Filter.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des mods");
                return 0;
            }
        }

        private SortDefinition<Mod> GetSortDefinition(string sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "popularity" => Builders<Mod>.Sort.Descending(m => m.DownloadCount),
                "rating" => Builders<Mod>.Sort.Descending(m => m.Rating),
                "alphabetical" => Builders<Mod>.Sort.Ascending(m => m.Name),
                "recent" or _ => Builders<Mod>.Sort.Descending(m => m.UpdatedAt)
            };
        }
    }
}
