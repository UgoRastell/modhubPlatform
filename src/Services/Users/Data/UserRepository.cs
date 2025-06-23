using MongoDB.Driver;
using MongoDB.Driver.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UsersService.Models;

namespace UsersService.Data;

public interface IUserRepository
{
    // Méthodes de base CRUD
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task CreateAsync(User user);
    Task UpdateAsync(string id, User user);
    Task DeleteAsync(string id);
    
    // Méthodes pour l'authentification et la gestion des utilisateurs
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task UpdateLastLoginAsync(string id, DateTime loginTime);
    
    // Méthodes pour la réinitialisation de mot de passe
    Task SetResetTokenAsync(string email, string token, DateTime expires);
    Task<User?> GetByResetTokenAsync(string token);
    Task ClearResetTokenAsync(string id);
}

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _usersCollection;

    public UserRepository(MongoDbSettings settings)
    {
        try
        {
            var mongoClient = new MongoClient(settings.ConnectionString);
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _usersCollection = database.GetCollection<User>(settings.UsersCollectionName);
            
            // Désactiver la création d'index en production pour éviter les erreurs
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Production")
            {
                try
                {
                    // Créer des index pour optimiser les requêtes courantes
                    var keys = Builders<User>.IndexKeys.Ascending(u => u.Email);
                    var options = new CreateIndexOptions { Unique = true };
                    _usersCollection.Indexes.CreateOne(new CreateIndexModel<User>(keys, options));

                    // Index pour le nom d'utilisateur
                    var usernameKeys = Builders<User>.IndexKeys.Ascending(u => u.Username);
                    var usernameOptions = new CreateIndexOptions { Unique = true };
                    _usersCollection.Indexes.CreateOne(new CreateIndexModel<User>(usernameKeys, usernameOptions));

                    // Index pour le token de réinitialisation
                    _usersCollection.Indexes.CreateOne(
                        new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.ResetToken)));
                }
                catch (MongoCommandException ex) when (ex.Code is 85 or 11000)
                {
                    // L'index existe déjà ou est en conflit : on l'ignore pour éviter de planter le service.
                    Console.WriteLine($"Warning: Index already exists: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Log l'erreur mais continue l'exécution
                    Console.WriteLine($"Warning: Failed to create indexes: {ex.GetType().Name} - {ex.Message}");
                }
            }
        }
        catch (Exception ex) 
        {
            // Log complet de l'erreur critique
            Console.WriteLine($"CRITICAL: MongoDB initialization failed: {ex.GetType().Name} - {ex.Message}");
            throw; // On remonte l'erreur pour arrêter le service si la connexion est impossible
        }
    }
    
    public async Task<List<User>> GetAllAsync()
    {
        return await _usersCollection.Find(_ => true).ToListAsync();
    }
    
    public async Task<User?> GetByIdAsync(string id)
    {
        return await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
    }
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _usersCollection.Find(u => u.Email == email.ToLowerInvariant()).FirstOrDefaultAsync();
    }
    
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _usersCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
    }
    
    public async Task CreateAsync(User user)
    {
        // Normalisation de l'email
        user.Email = user.Email.ToLowerInvariant();
        await _usersCollection.InsertOneAsync(user);
    }
    
    public async Task UpdateAsync(string id, User user)
    {
        // Normalisation de l'email si présent
        if (!string.IsNullOrEmpty(user.Email))
            user.Email = user.Email.ToLowerInvariant();
            
        await _usersCollection.ReplaceOneAsync(u => u.Id == id, user);
    }
    
    public async Task DeleteAsync(string id)
    {
        await _usersCollection.DeleteOneAsync(u => u.Id == id);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _usersCollection.Find(u => u.Username == username).AnyAsync();
    }
    
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _usersCollection.Find(u => u.Email == email.ToLowerInvariant()).AnyAsync();
    }
    
    public async Task UpdateLastLoginAsync(string id, DateTime loginTime)
    {
        var update = Builders<User>.Update.Set(u => u.LastLogin, loginTime);
        await _usersCollection.UpdateOneAsync(u => u.Id == id, update);
    }
    
    public async Task SetResetTokenAsync(string email, string token, DateTime expires)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email.ToLowerInvariant());
        var update = Builders<User>.Update
            .Set(u => u.ResetToken, token)
            .Set(u => u.ResetTokenExpires, expires);
        
        await _usersCollection.UpdateOneAsync(filter, update);
    }
    
    public async Task<User?> GetByResetTokenAsync(string token)
    {
        return await _usersCollection.Find(u => 
            u.ResetToken == token && 
            u.ResetTokenExpires > DateTime.UtcNow).FirstOrDefaultAsync();
    }
    
    public async Task ClearResetTokenAsync(string id)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        var update = Builders<User>.Update
            .Unset(u => u.ResetToken)
            .Unset(u => u.ResetTokenExpires);
            
        await _usersCollection.UpdateOneAsync(filter, update);
    }
}
