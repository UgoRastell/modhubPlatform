using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using ModsService.Models;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ModsService.Repositories
{
    public class ModRepository : IModRepository
    {
        private readonly IMongoCollection<Mod> _mods;
        private readonly ILogger<ModRepository> _logger;

        public ModRepository(IMongoDatabase database, ILogger<ModRepository> logger)
        {
            _mods = database.GetCollection<Mod>("Mods");
            _logger = logger;
            
            // Créer des index pour optimiser les requêtes
            CreateIndexes();
        }
        
        private void CreateIndexes()
        {
            try
            {
                // Index sur les champs fréquemment recherchés
                var gameIdIndex = Builders<Mod>.IndexKeys.Ascending(m => m.GameId);
                var categoryIdIndex = Builders<Mod>.IndexKeys.Ascending(m => m.CategoryId);
                var creatorIdIndex = Builders<Mod>.IndexKeys.Ascending(m => m.CreatorId);
                
                // Index composé pour les requêtes de recherche courantes
                var searchIndex = Builders<Mod>.IndexKeys
                    .Ascending(m => m.GameId)
                    .Ascending(m => m.CategoryId)
                    .Text(m => m.Name)
                    .Text(m => m.Description);
                    
                // Index texte complet avec poids différents selon les champs
                var textSearchIndex = Builders<Mod>.IndexKeys
                    .Text(m => m.Name, new TextIndexOptions { Weight = 10 })
                    .Text(m => m.Description, new TextIndexOptions { Weight = 5 })
                    .Text("Tags", new TextIndexOptions { Weight = 8 });
                    
                // Index pour les tris fréquents
                var downloadCountIndex = Builders<Mod>.IndexKeys.Descending(m => m.DownloadCount);
                var ratingIndex = Builders<Mod>.IndexKeys.Descending(m => m.Rating);
                var createdAtIndex = Builders<Mod>.IndexKeys.Descending(m => m.CreatedAt);
                var updatedAtIndex = Builders<Mod>.IndexKeys.Descending(m => m.UpdatedAt);
                
                // Index sur les tags pour la recherche par tag
                var tagsIndex = Builders<Mod>.IndexKeys.Ascending("Tags");
                
                // Index pour la recherche par compatibilité de version
                var versionCompIndex = Builders<Mod>.IndexKeys
                    .Ascending(m => m.GameId)
                    .Ascending("Versions.GameVersion");
                    
                // Index pour les mods récemment mis à jour
                var recentUpdateIndex = Builders<Mod>.IndexKeys
                    .Ascending(m => m.GameId)
                    .Descending(m => m.LastStableReleaseAt);
                
                // Index pour les mods en maintenance active
                var activeMaintainedIndex = Builders<Mod>.IndexKeys
                    .Ascending(m => m.GameId)
                    .Ascending(m => m.IsHidden)
                    .Descending(m => m.UpdatedAt);
                
                // Créer les index s'ils n'existent pas déjà
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(gameIdIndex, new CreateIndexOptions { Name = "ix_gameId" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(categoryIdIndex, new CreateIndexOptions { Name = "ix_categoryId" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(creatorIdIndex, new CreateIndexOptions { Name = "ix_creatorId" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(searchIndex, new CreateIndexOptions { Name = "ix_search" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(textSearchIndex, new CreateIndexOptions { Name = "ix_text_search" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(downloadCountIndex, new CreateIndexOptions { Name = "ix_downloadCount" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(ratingIndex, new CreateIndexOptions { Name = "ix_rating" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(createdAtIndex, new CreateIndexOptions { Name = "ix_createdAt" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(updatedAtIndex, new CreateIndexOptions { Name = "ix_updatedAt" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(tagsIndex, new CreateIndexOptions { Name = "ix_tags" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(versionCompIndex, new CreateIndexOptions { Name = "ix_version_compatibility" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(recentUpdateIndex, new CreateIndexOptions { Name = "ix_recent_update" }));
                _mods.Indexes.CreateOne(new CreateIndexModel<Mod>(activeMaintainedIndex, new CreateIndexOptions { Name = "ix_active_maintained" }));
                
                _logger.LogInformation("MongoDB indexes created successfully for Mods collection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MongoDB indexes for Mods collection");
            }
        }

        public async Task<IEnumerable<Mod>> GetAllModsAsync()
        {
            return await _mods.Find(mod => true).ToListAsync();
        }

        public async Task<Mod> GetModByIdAsync(string id)
        {
            // Vérifier si l'ID est un ObjectId valide
            if (!ObjectId.TryParse(id, out _))
            {
                return null;
            }
            
            return await _mods.Find(mod => mod.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Mod>> GetModsByGameIdAsync(string gameId)
        {
            // Utilisation de l'index ix_gameId
            return await _mods.Find(mod => mod.GameId == gameId).ToListAsync();
        }

        public async Task<IEnumerable<Mod>> GetModsByCreatorIdAsync(string creatorId)
        {
            // Utilisation de l'index ix_creatorId
            return await _mods.Find(mod => mod.CreatorId == creatorId).ToListAsync();
        }

        public async Task<Mod> CreateModAsync(Mod mod)
        {
            await _mods.InsertOneAsync(mod);
            return mod;
        }

        public async Task<Mod> UpdateModAsync(Mod modToUpdate)
        {
            await _mods.ReplaceOneAsync(mod => mod.Id == modToUpdate.Id, modToUpdate);
            return modToUpdate;
        }

        public async Task<bool> DeleteModAsync(string id)
        {
            var result = await _mods.DeleteOneAsync(mod => mod.Id == id);
            return result.DeletedCount > 0;
        }
        
        public async Task<(IEnumerable<Mod> mods, int totalCount)> SearchModsAsync(
            string gameId,
            string categoryId,
            string searchTerm,
            string sortBy,
            int pageNumber,
            int pageSize)
        {
            try
            {
                var filterBuilder = Builders<Mod>.Filter;
                var filter = filterBuilder.Empty;

                // Appliquer les filtres si fournis
                if (!string.IsNullOrEmpty(gameId))
                {
                    filter &= filterBuilder.Eq(m => m.GameId, gameId);
                }

                if (!string.IsNullOrEmpty(categoryId))
                {
                    filter &= filterBuilder.Eq(m => m.CategoryId, categoryId);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchFilter = filterBuilder.Or(
                        filterBuilder.Regex(m => m.Name, new BsonRegularExpression(searchTerm, "i")),
                        filterBuilder.Regex(m => m.Description, new BsonRegularExpression(searchTerm, "i")),
                        filterBuilder.AnyIn(m => m.Tags, new[] { searchTerm })
                    );
                    filter &= searchFilter;
                }

                // Compter le nombre total de résultats pour la pagination
                var totalCount = await _mods.CountDocumentsAsync(filter);

                // Créer le tri selon le paramètre fourni
                var sortDefinition = sortBy?.ToLower() switch
                {
                    "downloads" => Builders<Mod>.Sort.Descending(m => m.DownloadCount),
                    "rating" => Builders<Mod>.Sort.Descending(m => m.Rating),
                    "newest" => Builders<Mod>.Sort.Descending(m => m.CreatedAt),
                    "updated" => Builders<Mod>.Sort.Descending(m => m.UpdatedAt),
                    _ => Builders<Mod>.Sort.Descending(m => m.DownloadCount) // Par défaut : téléchargements
                };

                // Appliquer la pagination
                var skip = (pageNumber - 1) * pageSize;
                
                // Exécuter la requête optimisée avec projection pour limiter les champs retournés si besoin
                var mods = await _mods
                    .Find(filter)
                    .Sort(sortDefinition)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync();

                return (mods, (int)totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching mods");
                return (Enumerable.Empty<Mod>(), 0);
            }
        }
        
        public async Task<bool> AddRatingAsync(string modId, int rating, string userId)
        {
            try
            {
                // Vérifier si l'utilisateur a déjà noté ce mod
                var mod = await _mods.Find(m => m.Id == modId).FirstOrDefaultAsync();
                
                if (mod == null)
                {
                    return false;
                }
                
                // Initialiser les collections si elles sont nulles
                mod.Ratings ??= new List<Rating>();
                
                // Vérifier si l'utilisateur a déjà noté ce mod
                var existingRating = mod.Ratings.FirstOrDefault(r => r.UserId == userId);
                
                if (existingRating != null)
                {
                    // Mettre à jour la note existante
                    existingRating.Value = rating;
                    existingRating.Date = DateTime.UtcNow;
                }
                else
                {
                    // Ajouter une nouvelle note
                    mod.Ratings.Add(new Rating
                    {
                        UserId = userId,
                        Value = rating,
                        Date = DateTime.UtcNow
                    });
                }
                
                // Calculer la moyenne des notes
                mod.Rating = mod.Ratings.Average(r => r.Value);
                mod.RatingCount = mod.Ratings.Count;
                
                // Mettre à jour le mod
                await _mods.ReplaceOneAsync(m => m.Id == modId, mod);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding rating to mod {modId}");
                return false;
            }
        }
    }
}
