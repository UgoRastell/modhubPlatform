using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using CommunityService.Models.Forum;

namespace CommunityService.Services.Forums
{
    /// <summary>
    /// Service de gestion du forum avec persistance MongoDB
    /// </summary>
    public class ForumService : IForumService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<ForumCategory> _categoriesCollection;
        private readonly IMongoCollection<ForumTopic> _topicsCollection;
        private readonly IMongoCollection<ForumPost> _postsCollection;
        private readonly ILogger<ForumService> _logger;

        public ForumService(
            IMongoDatabase database,
            ILogger<ForumService> logger)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialisation des collections MongoDB
            _categoriesCollection = _database.GetCollection<ForumCategory>("forum_categories");
            _topicsCollection = _database.GetCollection<ForumTopic>("forum_topics");
            _postsCollection = _database.GetCollection<ForumPost>("forum_posts");
        }

        #region Catégories - Méthodes de base

        public async Task<List<ForumCategory>> GetAllCategoriesAsync()
        {
            try
            {
                _logger.LogInformation("Récupération de toutes les catégories du forum");
                var categories = await _categoriesCollection.Find(_ => true)
                    .SortBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                
                _logger.LogInformation("Récupéré {Count} catégories", categories.Count);
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des catégories");
                throw;
            }
        }

        public async Task<List<ForumCategory>> GetRootCategoriesAsync()
        {
            try
            {
                _logger.LogInformation("Récupération des catégories racines");
                var rootCategories = await _categoriesCollection.Find(c => c.ParentCategoryId == null)
                    .SortBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                
                _logger.LogInformation("Récupéré {Count} catégories racines", rootCategories.Count);
                return rootCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des catégories racines");
                throw;
            }
        }

        public async Task<ForumCategory> GetCategoryByIdAsync(string categoryId)
        {
            try
            {
                _logger.LogInformation("Récupération de la catégorie {CategoryId}", categoryId);
                var category = await _categoriesCollection.Find(c => c.Id == categoryId).FirstOrDefaultAsync();
                
                if (category == null)
                {
                    _logger.LogWarning("Catégorie {CategoryId} non trouvée", categoryId);
                }
                
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la catégorie {CategoryId}", categoryId);
                throw;
            }
        }

        #endregion

        #region Méthodes à implémenter (squelettes)

        public async Task<List<ForumCategory>> GetSubcategoriesAsync(string parentCategoryId)
        {
            try
            {
                _logger.LogInformation("Récupération des sous-catégories de {ParentCategoryId}", parentCategoryId);
                var subcategories = await _categoriesCollection.Find(c => c.ParentCategoryId == parentCategoryId)
                    .SortBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                
                _logger.LogInformation("Récupéré {Count} sous-catégories", subcategories.Count);
                return subcategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sous-catégories de {ParentCategoryId}", parentCategoryId);
                throw;
            }
        }

        public async Task<ForumCategory> CreateCategoryAsync(ForumCategory category)
        {
            try
            {
                _logger.LogInformation("Création d'une nouvelle catégorie : {CategoryName}", category.Name);
                
                // Génération d'un nouvel ID si non fourni
                if (string.IsNullOrEmpty(category.Id))
                {
                    category.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                }
                
                // Définir les timestamps
                category.CreatedAt = DateTime.UtcNow;
                category.UpdatedAt = DateTime.UtcNow;
                
                // Initialiser les statistiques si non définies
                if (category.Stats == null)
                {
                    category.Stats = new ForumCategoryStats();
                }
                
                await _categoriesCollection.InsertOneAsync(category);
                
                _logger.LogInformation("Catégorie créée avec succès : {CategoryId}", category.Id);
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la catégorie {CategoryName}", category.Name);
                throw;
            }
        }

        public async Task<bool> UpdateCategoryAsync(string categoryId, ForumCategory category)
        {
            try
            {
                _logger.LogInformation("Mise à jour de la catégorie {CategoryId}", categoryId);
                
                // Vérifier que la catégorie existe
                var existingCategory = await GetCategoryByIdAsync(categoryId);
                if (existingCategory == null)
                {
                    _logger.LogWarning("Tentative de mise à jour d'une catégorie inexistante : {CategoryId}", categoryId);
                    return false;
                }
                
                // Mettre à jour le timestamp
                category.UpdatedAt = DateTime.UtcNow;
                category.Id = categoryId; // S'assurer que l'ID est correct
                
                var result = await _categoriesCollection.ReplaceOneAsync(
                    c => c.Id == categoryId, 
                    category);
                
                var success = result.ModifiedCount > 0;
                _logger.LogInformation("Mise à jour catégorie {CategoryId} : {Success}", categoryId, success);
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la catégorie {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(string categoryId)
        {
            try
            {
                _logger.LogInformation("Suppression de la catégorie {CategoryId}", categoryId);
                
                // Vérifier qu'il n'y a pas de sous-catégories
                var subcategories = await GetSubcategoriesAsync(categoryId);
                if (subcategories.Any())
                {
                    _logger.LogWarning("Impossible de supprimer la catégorie {CategoryId} : elle contient des sous-catégories", categoryId);
                    throw new InvalidOperationException("Impossible de supprimer une catégorie qui contient des sous-catégories");
                }
                
                // Vérifier qu'il n'y a pas de sujets dans cette catégorie
                var topicsCount = await _topicsCollection.CountDocumentsAsync(t => t.CategoryId == categoryId);
                if (topicsCount > 0)
                {
                    _logger.LogWarning("Impossible de supprimer la catégorie {CategoryId} : elle contient {TopicsCount} sujets", categoryId, topicsCount);
                    throw new InvalidOperationException($"Impossible de supprimer une catégorie qui contient {topicsCount} sujet(s)");
                }
                
                var result = await _categoriesCollection.DeleteOneAsync(c => c.Id == categoryId);
                
                var success = result.DeletedCount > 0;
                _logger.LogInformation("Suppression catégorie {CategoryId} : {Success}", categoryId, success);
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la catégorie {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<List<ForumTopic>> GetTopicsByCategoryAsync(string categoryId, int page = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Récupération des sujets de la catégorie {CategoryId}, page {Page}", categoryId, page);
                
                var skip = (page - 1) * pageSize;
                var topics = await _topicsCollection.Find(t => t.CategoryId == categoryId)
                    .SortByDescending(t => t.IsPinned) // Sujets épinglés en premier
                    .ThenByDescending(t => t.LastActivityAt) // Puis par dernière activité
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync();
                
                _logger.LogInformation("Récupéré {Count} sujets pour la catégorie {CategoryId}", topics.Count, categoryId);
                return topics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sujets de la catégorie {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<ForumTopic> GetTopicByIdAsync(string topicId)
        {
            try
            {
                _logger.LogInformation("Récupération du sujet {TopicId}", topicId);
                
                var topic = await _topicsCollection.Find(t => t.Id == topicId).FirstOrDefaultAsync();
                
                if (topic != null)
                {
                    // Incrémenter le compteur de vues
                    await _topicsCollection.UpdateOneAsync(
                        t => t.Id == topicId,
                        Builders<ForumTopic>.Update.Inc(t => t.ViewCount, 1)
                    );
                    topic.ViewCount++; // Mettre à jour l'objet local
                    
                    _logger.LogInformation("Sujet {TopicId} trouvé et vue incrémentée", topicId);
                }
                else
                {
                    _logger.LogWarning("Sujet {TopicId} non trouvé", topicId);
                }
                
                return topic;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du sujet {TopicId}", topicId);
                throw;
            }
        }

        public async Task<List<ForumTopic>> SearchTopicsAsync(string query, int page = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Recherche de sujets avec la requête '{Query}', page {Page}", query, page);
                
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new List<ForumTopic>();
                }
                
                var skip = (page - 1) * pageSize;
                
                // Recherche dans le titre et la description
                var filter = Builders<ForumTopic>.Filter.Or(
                    Builders<ForumTopic>.Filter.Regex(t => t.Title, new MongoDB.Bson.BsonRegularExpression(query, "i")),
                    Builders<ForumTopic>.Filter.Regex(t => t.Description, new MongoDB.Bson.BsonRegularExpression(query, "i"))
                );
                
                var topics = await _topicsCollection.Find(filter)
                    .SortByDescending(t => t.LastActivityAt)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync();
                
                _logger.LogInformation("Trouvé {Count} sujets correspondant à '{Query}'", topics.Count, query);
                return topics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche de sujets avec '{Query}'", query);
                throw;
            }
        }

        public async Task<ForumTopic> CreateTopicAsync(ForumTopic topic, string initialPostContent)
        {
            try
            {
                _logger.LogInformation("Création d'un nouveau sujet : {TopicTitle}", topic.Title);
                
                // Génération d'un nouvel ID si non fourni
                if (string.IsNullOrEmpty(topic.Id))
                {
                    topic.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                }
                
                // Définir les timestamps
                var now = DateTime.UtcNow;
                topic.CreatedAt = now;
                topic.LastActivityAt = now;
                
                // Initialiser les compteurs
                topic.PostCount = 1; // Le message initial
                topic.ViewCount = 0;
                
                // Créer le sujet
                await _topicsCollection.InsertOneAsync(topic);
                
                // Créer le message initial
                var initialPost = new ForumPost
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    TopicId = topic.Id,
                    CategoryId = topic.CategoryId,
                    Content = initialPostContent,
                    CreatedByUserId = topic.CreatedByUserId,
                    CreatedByUsername = topic.CreatedByUsername,
                    CreatedAt = now,
                    Position = 1 // Premier message
                };
                
                await _postsCollection.InsertOneAsync(initialPost);
                
                // Mettre à jour les statistiques de la catégorie
                await _categoriesCollection.UpdateOneAsync(
                    c => c.Id == topic.CategoryId,
                    Builders<ForumCategory>.Update
                        .Inc("Stats.TopicCount", 1)
                        .Inc("Stats.PostCount", 1)
                        .Set("Stats.LastActivityAt", now)
                );
                
                _logger.LogInformation("Sujet créé avec succès : {TopicId}", topic.Id);
                return topic;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du sujet {TopicTitle}", topic.Title);
                throw;
            }
        }

        public async Task<bool> UpdateTopicAsync(string topicId, ForumTopic topic)
        {
            try
            {
                _logger.LogInformation("Mise à jour du sujet {TopicId}", topicId);
                
                // Vérifier que le sujet existe
                var existingTopic = await GetTopicByIdAsync(topicId);
                if (existingTopic == null)
                {
                    _logger.LogWarning("Tentative de mise à jour d'un sujet inexistant : {TopicId}", topicId);
                    return false;
                }
                
                // Préserver certains champs sensibles
                topic.Id = topicId;
                topic.CreatedAt = existingTopic.CreatedAt; // Ne pas modifier la date de création
                topic.PostCount = existingTopic.PostCount; // Préserver le compteur de posts
                topic.ViewCount = existingTopic.ViewCount; // Préserver le compteur de vues
                
                var result = await _topicsCollection.ReplaceOneAsync(
                    t => t.Id == topicId, 
                    topic);
                
                var success = result.ModifiedCount > 0;
                _logger.LogInformation("Mise à jour sujet {TopicId} : {Success}", topicId, success);
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du sujet {TopicId}", topicId);
                throw;
            }
        }

        public async Task<bool> DeleteTopicAsync(string topicId)
        {
            try
            {
                _logger.LogInformation("Suppression du sujet {TopicId}", topicId);
                
                // Récupérer le sujet pour obtenir les informations nécessaires
                var topic = await _topicsCollection.Find(t => t.Id == topicId).FirstOrDefaultAsync();
                if (topic == null)
                {
                    _logger.LogWarning("Tentative de suppression d'un sujet inexistant : {TopicId}", topicId);
                    return false;
                }
                
                // Compter les messages associés
                var postCount = await _postsCollection.CountDocumentsAsync(p => p.TopicId == topicId);
                
                // Supprimer tous les messages du sujet
                await _postsCollection.DeleteManyAsync(p => p.TopicId == topicId);
                
                // Supprimer le sujet
                var result = await _topicsCollection.DeleteOneAsync(t => t.Id == topicId);
                
                if (result.DeletedCount > 0)
                {
                    // Mettre à jour les statistiques de la catégorie
                    await _categoriesCollection.UpdateOneAsync(
                        c => c.Id == topic.CategoryId,
                        Builders<ForumCategory>.Update
                            .Inc("Stats.TopicCount", -1)
                            .Inc("Stats.PostCount", -(int)postCount)
                    );
                    
                    _logger.LogInformation("Sujet {TopicId} supprimé avec {PostCount} messages", topicId, postCount);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du sujet {TopicId}", topicId);
                throw;
            }
        }

        public async Task<bool> PinTopicAsync(string topicId, bool isPinned)
        {
            try
            {
                _logger.LogInformation("{Action} du sujet {TopicId}", isPinned ? "Épinglage" : "Désépinglage", topicId);
                
                var result = await _topicsCollection.UpdateOneAsync(
                    t => t.Id == topicId,
                    Builders<ForumTopic>.Update.Set(t => t.IsPinned, isPinned)
                );
                
                var success = result.ModifiedCount > 0;
                _logger.LogInformation("{Action} sujet {TopicId} : {Success}", 
                    isPinned ? "Épinglage" : "Désépinglage", topicId, success);
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'{Action} du sujet {TopicId}", 
                    isPinned ? "épinglage" : "désépinglage", topicId);
                throw;
            }
        }

        public async Task<bool> LockTopicAsync(string topicId, bool isLocked)
        {
            try
            {
                _logger.LogInformation("{Action} du sujet {TopicId}", isLocked ? "Verrouillage" : "Déverrouillage", topicId);
                
                var result = await _topicsCollection.UpdateOneAsync(
                    t => t.Id == topicId,
                    Builders<ForumTopic>.Update.Set(t => t.IsLocked, isLocked)
                );
                
                var success = result.ModifiedCount > 0;
                _logger.LogInformation("{Action} sujet {TopicId} : {Success}", 
                    isLocked ? "Verrouillage" : "Déverrouillage", topicId, success);
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du {Action} du sujet {TopicId}", 
                    isLocked ? "verrouillage" : "déverrouillage", topicId);
                throw;
            }
        }

        public async Task<bool> MarkTopicAsSolvedAsync(string topicId, string solutionPostId)
        {
            try
            {
                _logger.LogInformation("Marquage du sujet {TopicId} comme résolu avec le message {SolutionPostId}", topicId, solutionPostId);
                
                // Vérifier que le message existe et appartient au sujet
                var solutionPost = await _postsCollection.Find(p => p.Id == solutionPostId && p.TopicId == topicId).FirstOrDefaultAsync();
                if (solutionPost == null)
                {
                    _logger.LogWarning("Message solution {SolutionPostId} non trouvé dans le sujet {TopicId}", solutionPostId, topicId);
                    return false;
                }
                
                // Marquer le sujet comme résolu
                var topicResult = await _topicsCollection.UpdateOneAsync(
                    t => t.Id == topicId,
                    Builders<ForumTopic>.Update
                        .Set(t => t.IsSolved, true)
                        .Set(t => t.SolutionPostId, solutionPostId)
                );
                
                // Marquer le message comme solution
                var postResult = await _postsCollection.UpdateOneAsync(
                    p => p.Id == solutionPostId,
                    Builders<ForumPost>.Update.Set(p => p.IsSolution, true)
                );
                
                var success = topicResult.ModifiedCount > 0 && postResult.ModifiedCount > 0;
                _logger.LogInformation("Marquage sujet {TopicId} comme résolu : {Success}", topicId, success);
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage du sujet {TopicId} comme résolu", topicId);
                throw;
            }
        }

        public async Task<List<ForumPost>> GetPostsByTopicAsync(string topicId, int page = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Récupération des messages du sujet {TopicId}, page {Page}", topicId, page);
                
                var skip = (page - 1) * pageSize;
                var posts = await _postsCollection.Find(p => p.TopicId == topicId)
                    .SortBy(p => p.Position) // Tri par position dans le sujet
                    .ThenBy(p => p.CreatedAt) // Puis par date de création
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync();
                
                _logger.LogInformation("Récupéré {Count} messages pour le sujet {TopicId}", posts.Count, topicId);
                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des messages du sujet {TopicId}", topicId);
                throw;
            }
        }

        public async Task<ForumPost> GetPostByIdAsync(string postId)
        {
            try
            {
                _logger.LogInformation("Récupération du message {PostId}", postId);
                
                var post = await _postsCollection.Find(p => p.Id == postId).FirstOrDefaultAsync();
                
                if (post == null)
                {
                    _logger.LogWarning("Message {PostId} non trouvé", postId);
                }
                
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du message {PostId}", postId);
                throw;
            }
        }

        public async Task<ForumPost> CreatePostAsync(ForumPost post)
        {
            try
            {
                _logger.LogInformation("Création d'un nouveau message dans le sujet {TopicId}", post.TopicId);
                
                // Génération d'un nouvel ID si non fourni
                if (string.IsNullOrEmpty(post.Id))
                {
                    post.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                }
                
                // Définir le timestamp
                post.CreatedAt = DateTime.UtcNow;
                
                // Déterminer la position du message dans le sujet
                var lastPosition = await _postsCollection
                    .Find(p => p.TopicId == post.TopicId)
                    .SortByDescending(p => p.Position)
                    .Project(p => p.Position)
                    .FirstOrDefaultAsync();
                
                post.Position = lastPosition + 1;
                
                // Créer le message
                await _postsCollection.InsertOneAsync(post);
                
                // Mettre à jour les statistiques du sujet
                var now = DateTime.UtcNow;
                await _topicsCollection.UpdateOneAsync(
                    t => t.Id == post.TopicId,
                    Builders<ForumTopic>.Update
                        .Inc(t => t.PostCount, 1)
                        .Set(t => t.LastActivityAt, now)
                        .Set(t => t.LastActivityByUserId, post.CreatedByUserId)
                        .Set(t => t.LastActivityByUsername, post.CreatedByUsername)
                );
                
                // Mettre à jour les statistiques de la catégorie
                await _categoriesCollection.UpdateOneAsync(
                    c => c.Id == post.CategoryId,
                    Builders<ForumCategory>.Update
                        .Inc("Stats.PostCount", 1)
                        .Set("Stats.LastActivityAt", now)
                );
                
                _logger.LogInformation("Message créé avec succès : {PostId} (position {Position})", post.Id, post.Position);
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du message dans le sujet {TopicId}", post.TopicId);
                throw;
            }
        }

        public async Task<bool> UpdatePostAsync(string postId, string newContent, string editReason = null)
        {
            try
            {
                _logger.LogInformation("Mise à jour du message {PostId}", postId);
                
                // Récupérer le message existant
                var existingPost = await GetPostByIdAsync(postId);
                if (existingPost == null)
                {
                    _logger.LogWarning("Tentative de mise à jour d'un message inexistant : {PostId}", postId);
                    return false;
                }
                
                // Sauvegarder l'historique des modifications
                var editHistory = new PostEditHistory
                {
                    Content = existingPost.Content,
                    EditedAt = DateTime.UtcNow,
                    EditedByUserId = existingPost.CreatedByUserId, // À adapter selon le contexte
                    EditReason = editReason
                };
                
                // Mettre à jour le message
                var update = Builders<ForumPost>.Update
                    .Set(p => p.Content, newContent)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow)
                    .Push(p => p.EditHistory, editHistory);
                
                var result = await _postsCollection.UpdateOneAsync(
                    p => p.Id == postId,
                    update
                );
                
                var success = result.ModifiedCount > 0;
                _logger.LogInformation("Mise à jour message {PostId} : {Success}", postId, success);
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du message {PostId}", postId);
                throw;
            }
        }

        public async Task<bool> DeletePostAsync(string postId)
        {
            try
            {
                _logger.LogInformation("Suppression du message {PostId}", postId);
                
                // Récupérer le message pour obtenir les informations nécessaires
                var post = await GetPostByIdAsync(postId);
                if (post == null)
                {
                    _logger.LogWarning("Tentative de suppression d'un message inexistant : {PostId}", postId);
                    return false;
                }
                
                // Vérifier si c'est le premier message du sujet (ne peut pas être supprimé)
                if (post.Position == 1)
                {
                    _logger.LogWarning("Tentative de suppression du message initial du sujet {TopicId}", post.TopicId);
                    throw new InvalidOperationException("Le message initial d'un sujet ne peut pas être supprimé");
                }
                
                // Supprimer le message
                var result = await _postsCollection.DeleteOneAsync(p => p.Id == postId);
                
                if (result.DeletedCount > 0)
                {
                    // Mettre à jour les statistiques du sujet
                    await _topicsCollection.UpdateOneAsync(
                        t => t.Id == post.TopicId,
                        Builders<ForumTopic>.Update.Inc(t => t.PostCount, -1)
                    );
                    
                    // Mettre à jour les statistiques de la catégorie
                    await _categoriesCollection.UpdateOneAsync(
                        c => c.Id == post.CategoryId,
                        Builders<ForumCategory>.Update.Inc("Stats.PostCount", -1)
                    );
                    
                    _logger.LogInformation("Message {PostId} supprimé avec succès", postId);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du message {PostId}", postId);
                throw;
            }
        }

        public async Task<bool> AddReactionAsync(string postId, string userId, string reactionType)
        {
            try
            {
                _logger.LogInformation("Ajout de réaction {ReactionType} au message {PostId} par l'utilisateur {UserId}", reactionType, postId, userId);
                
                // Vérifier que le message existe
                var post = await GetPostByIdAsync(postId);
                if (post == null)
                {
                    _logger.LogWarning("Tentative d'ajout de réaction sur un message inexistant : {PostId}", postId);
                    return false;
                }
                
                // Vérifier si l'utilisateur a déjà réagi avec ce type de réaction
                var existingReaction = await _postsCollection.Find(
                    p => p.Id == postId && p.Reactions[reactionType].Contains(userId)
                ).FirstOrDefaultAsync();
                
                if (existingReaction != null)
                {
                    _logger.LogInformation("L'utilisateur {UserId} a déjà réagi avec {ReactionType} au message {PostId}", userId, reactionType, postId);
                    return true; // Déjà réagi, considéré comme succès
                }
                
                // Ajouter la réaction
                var result = await _postsCollection.UpdateOneAsync(
                    p => p.Id == postId,
                    Builders<ForumPost>.Update.AddToSet($"Reactions.{reactionType}", userId)
                );
                
                var success = result.ModifiedCount > 0;
                _logger.LogInformation("Ajout réaction {ReactionType} au message {PostId} : {Success}", reactionType, postId, success);
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout de réaction {ReactionType} au message {PostId}", reactionType, postId);
                throw;
            }
        }

        public async Task<bool> RemoveReactionAsync(string postId, string userId, string reactionType)
        {
            try
            {
                _logger.LogInformation("Suppression de réaction {ReactionType} du message {PostId} par l'utilisateur {UserId}", reactionType, postId, userId);
                
                // Vérifier que le message existe
                var post = await GetPostByIdAsync(postId);
                if (post == null)
                {
                    _logger.LogWarning("Tentative de suppression de réaction sur un message inexistant : {PostId}", postId);
                    return false;
                }
                
                // Supprimer la réaction
                var result = await _postsCollection.UpdateOneAsync(
                    p => p.Id == postId,
                    Builders<ForumPost>.Update.Pull($"Reactions.{reactionType}", userId)
                );
                
                var success = result.ModifiedCount > 0;
                _logger.LogInformation("Suppression réaction {ReactionType} du message {PostId} : {Success}", reactionType, postId, success);
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de réaction {ReactionType} du message {PostId}", reactionType, postId);
                throw;
            }
        }

        public async Task<bool> FlagPostAsync(string postId, string userId, string reason)
        {
            try
            {
                _logger.LogInformation("Signalement du message {PostId} par l'utilisateur {UserId} pour : {Reason}", postId, userId, reason);
                
                // Vérifier que le message existe
                var post = await GetPostByIdAsync(postId);
                if (post == null)
                {
                    _logger.LogWarning("Tentative de signalement d'un message inexistant : {PostId}", postId);
                    return false;
                }
                
                // Marquer le message comme signalé
                var result = await _postsCollection.UpdateOneAsync(
                    p => p.Id == postId,
                    Builders<ForumPost>.Update
                        .Set(p => p.IsFlagged, true)
                        .Set(p => p.FlagReason, reason)
                );
                
                var success = result.ModifiedCount > 0;
                _logger.LogInformation("Signalement du message {PostId} : {Success}", postId, success);
                
                // TODO: Notifier les modérateurs du signalement
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du signalement du message {PostId}", postId);
                throw;
            }
        }

        public async Task<ForumStatistics> GetForumStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("Génération des statistiques globales du forum");
                
                // Compter les catégories
                var categoriesCount = await _categoriesCollection.CountDocumentsAsync(_ => true);
                
                // Compter les sujets
                var topicsCount = await _topicsCollection.CountDocumentsAsync(_ => true);
                
                // Compter les messages
                var postsCount = await _postsCollection.CountDocumentsAsync(_ => true);
                
                // Compter les utilisateurs actifs (qui ont posté au moins un message)
                var activeUsers = await _postsCollection.Distinct<string>("CreatedByUserId", Builders<ForumPost>.Filter.Empty).ToListAsync();
                
                // Trouver le dernier message
                var lastPost = await _postsCollection.Find(_ => true)
                    .SortByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();
                
                var statistics = new ForumStatistics
                {
                    TotalCategories = (int)categoriesCount,
                    TotalTopics = (int)topicsCount,
                    TotalPosts = (int)postsCount,
                    TotalActiveUsers = activeUsers.Count,
                    LastActivityAt = lastPost?.CreatedAt ?? DateTime.MinValue,
                    LastActivityByUsername = lastPost?.CreatedByUsername ?? "Aucun"
                };
                
                _logger.LogInformation("Statistiques générées : {Categories} catégories, {Topics} sujets, {Posts} messages", 
                    statistics.TotalCategories, statistics.TotalTopics, statistics.TotalPosts);
                
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération des statistiques du forum");
                throw;
            }
        }

        public async Task<List<ForumTopic>> GetRecentlyActiveTopicsAsync(int pageSize = 20, int page = 1)
        {
            try
            {
                _logger.LogInformation("Récupération des sujets récemment actifs - page {Page}, taille {PageSize}", page, pageSize);
                
                var skip = (page - 1) * pageSize;
                var recentTopics = await _topicsCollection.Find(_ => true)
                    .SortByDescending(t => t.LastActivityAt)
                    .ThenByDescending(t => t.CreatedAt)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync();
                
                _logger.LogInformation("Récupéré {Count} sujets récemment actifs", recentTopics.Count);
                return recentTopics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sujets récemment actifs");
                throw;
            }
        }

        // Surcharge rétrocompatible : récupère les {count} sujets récemment actifs (page 1)
        public async Task<List<ForumTopic>> GetRecentlyActiveTopicsAsync(int count = 5)
        {
            return await GetRecentlyActiveTopicsAsync(count, 1);
        }

        public async Task<List<ForumTopic>> GetPopularTopicsAsync(int count = 5)
        {
            try
            {
                _logger.LogInformation("Récupération des {Count} sujets populaires", count);
                
                // Trier par nombre de vues et nombre de messages (pondéré)
                var popularTopics = await _topicsCollection.Find(_ => true)
                    .SortByDescending(t => t.ViewCount)
                    .ThenByDescending(t => t.PostCount)
                    .ThenByDescending(t => t.LastActivityAt)
                    .Limit(count)
                    .ToListAsync();
                
                _logger.LogInformation("Récupéré {Count} sujets populaires", popularTopics.Count);
                return popularTopics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sujets populaires");
                throw;
            }
        }

        #endregion
    }
}
