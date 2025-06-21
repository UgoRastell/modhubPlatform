using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace PaymentsService.Models.Affiliate
{
    /// <summary>
    /// Programme d'affiliation pour permettre aux utilisateurs de recommander des mods
    /// </summary>
    public class AffiliateProgram
    {
        /// <summary>
        /// Identifiant unique du programme d'affiliation
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du programme d'affiliation
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description du programme d'affiliation
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Pourcentage de commission pour l'affilié
        /// </summary>
        public decimal CommissionPercentage { get; set; } = 15;
        
        /// <summary>
        /// Période pendant laquelle une commission est attribuée après un clic (en jours)
        /// </summary>
        public int CookieDuration { get; set; } = 30;
        
        /// <summary>
        /// Montant minimum pour demander un paiement
        /// </summary>
        public decimal MinimumPayoutAmount { get; set; } = 25;
        
        /// <summary>
        /// Si le programme est actif
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Niveaux d'affiliation (standard, argent, or, etc.)
        /// </summary>
        public List<AffiliateLevel> Levels { get; set; } = new List<AffiliateLevel>();
        
        /// <summary>
        /// Types de commissions disponibles (vente, abonnement, etc.)
        /// </summary>
        public List<CommissionType> CommissionTypes { get; set; } = new List<CommissionType>();
        
        /// <summary>
        /// Date de création du programme
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de dernière mise à jour du programme
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Niveau d'affiliation avec avantages spécifiques
    /// </summary>
    public class AffiliateLevel
    {
        /// <summary>
        /// Nom du niveau (ex: Bronze, Silver, Gold)
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description du niveau
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Icône ou image représentative du niveau
        /// </summary>
        public string IconUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Nombre de ventes requises pour atteindre ce niveau
        /// </summary>
        public int RequiredSales { get; set; }
        
        /// <summary>
        /// Montant de ventes requis pour atteindre ce niveau
        /// </summary>
        public decimal RequiredSalesAmount { get; set; }
        
        /// <summary>
        /// Bonus de pourcentage de commission pour ce niveau
        /// </summary>
        public decimal BonusCommissionPercentage { get; set; } = 0;
        
        /// <summary>
        /// Avantages spécifiques à ce niveau
        /// </summary>
        public List<string> Benefits { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Type de commission pour différents produits ou services
    /// </summary>
    public class CommissionType
    {
        /// <summary>
        /// Nom du type de commission
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description détaillée
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Pourcentage de commission pour ce type spécifique
        /// </summary>
        public decimal CommissionPercentage { get; set; }
        
        /// <summary>
        /// Si c'est une commission récurrente (ex: abonnements)
        /// </summary>
        public bool IsRecurring { get; set; } = false;
        
        /// <summary>
        /// Nombre maximum de commissions récurrentes
        /// </summary>
        public int? MaxRecurrences { get; set; }
    }
}
