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
                
                // Désactiver la création d'index en production pour éviter les erreurs d'authentification
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Production")
                {
                    try
                    {
                        // Créer des index pour optimiser les requêtes courantes (uniquement en développement)
                        var indexKeysName = Builders<Mod>.IndexKeys.Ascending(m => m.Name);
                        var indexKeysCreator = Builders<Mod>.IndexKeys.Ascending(m => m.CreatorId);
                        var indexKeysGame = Builders<Mod>.IndexKeys.Ascending(m => m.GameId);
                        
                        _modsCollection.Indexes.CreateOne(new CreateIndexModel<Mod>(indexKeysName));
                        _modsCollection.Indexes.CreateOne(new CreateIndexModel<Mod>(indexKeysCreator));
                        _modsCollection.Indexes.CreateOne(new CreateIndexModel<Mod>(indexKeysGame));
                        
                        _logger.LogInformation("Index MongoDB créés avec succès");
                    }
                    catch (Exception indexEx)
                    {
                        // Ne pas faire échouer l'initialisation si la création d'index échoue
                        _logger.LogWarning(indexEx, "Impossible de créer les index MongoDB");
                    }
                }
                
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

        public async Task CreateAsync(Mod mod)
        {
            try
            {
                _logger.LogInformation("Création d'un nouveau mod : {ModName}", mod.Name);
                await _modsCollection.InsertOneAsync(mod);
                _logger.LogInformation("Mod créé avec succès : {ModId}", mod.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du mod {ModName}", mod.Name);
                throw;
            }
        }

        public async Task UpdateRatingAsync(string id, double averageRating, int reviewCount)
        {
            try
            {
                var filter = Builders<Mod>.Filter.Eq(m => m.Id, id);
                var update = Builders<Mod>.Update
                    .Set(m => m.Rating, averageRating)
                    .Set(m => m.ReviewCount, reviewCount)
                    .Set(m => m.UpdatedAt, DateTime.UtcNow);

                await _modsCollection.UpdateOneAsync(filter, update);
                _logger.LogInformation("Mise à jour du rating du mod {ModId} -> Average={Average}, Count={Count}", id, averageRating, reviewCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du rating du mod {ModId}", id);
                throw;
            }
        }

        public async Task IncrementDownloadCountAsync(string id)
        {
            try
            {
                var filter = Builders<Mod>.Filter.Eq(m => m.Id, id);
                var update = Builders<Mod>.Update
                    .Inc(m => m.DownloadCount, 1)
                    .Set(m => m.UpdatedAt, DateTime.UtcNow);

                await _modsCollection.UpdateOneAsync(filter, update);
                _logger.LogDebug("Incrémentation du compteur de téléchargements pour le mod {ModId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'incrémentation du compteur de téléchargements pour le mod {ModId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var filter = Builders<Mod>.Filter.Eq(m => m.Id, id);
                var result = await _modsCollection.DeleteOneAsync(filter);
                _logger.LogInformation("Suppression du mod {ModId}: DeletedCount={Count}", id, result.DeletedCount);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du mod {ModId}", id);
                throw;
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
