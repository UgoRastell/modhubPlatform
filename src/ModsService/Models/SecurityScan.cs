using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ModsService.Models
{
    /// <summary>
    /// Représente le résultat d'un scan de sécurité sur un fichier mod
    /// </summary>
    [BsonIgnoreExtraElements]
    public class SecurityScan
    {
        /// <summary>
        /// Identifiant unique du scan
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Date du scan
        /// </summary>
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Statut général du scan
        /// </summary>
        public ScanStatus Status { get; set; } = ScanStatus.Pending;
        
        /// <summary>
        /// Liste des menaces détectées
        /// </summary>
        public List<SecurityThreat> Threats { get; set; } = new List<SecurityThreat>();
        
        /// <summary>
        /// Liste des types de fichiers détectés dans l'archive
        /// </summary>
        public List<string> DetectedFileTypes { get; set; } = new List<string>();
        
        /// <summary>
        /// Nombre total de fichiers analysés
        /// </summary>
        public int FilesScanned { get; set; } = 0;
        
        /// <summary>
        /// Structure des dossiers de l'archive
        /// </summary>
        public Dictionary<string, int> FolderStructure { get; set; } = new Dictionary<string, int>();
        
        /// <summary>
        /// Nom du moteur de scan utilisé
        /// </summary>
        public string ScanEngine { get; set; } = string.Empty;
        
        /// <summary>
        /// Version du moteur de scan utilisé
        /// </summary>
        public string ScanEngineVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Commentaires sur le scan ou raisons des échecs
        /// </summary>
        public string Comments { get; set; } = string.Empty;
        
        /// <summary>
        /// Indique si le scan est complet ou partiel
        /// </summary>
        public bool IsComplete { get; set; } = false;
    }

    /// <summary>
    /// Représente une menace de sécurité détectée dans un fichier
    /// </summary>
    [BsonIgnoreExtraElements]
    public class SecurityThreat
    {
        /// <summary>
        /// Type de menace détectée
        /// </summary>
        public ThreatType Type { get; set; } = ThreatType.Unknown;
        
        /// <summary>
        /// Niveau de sévérité de la menace
        /// </summary>
        public SeverityLevel Severity { get; set; } = SeverityLevel.Unknown;
        
        /// <summary>
        /// Chemin du fichier infecté dans l'archive
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom de la menace (ex: "Trojan.Win32.Agent")
        /// </summary>
        public string ThreatName { get; set; } = string.Empty;
        
        /// <summary>
        /// Description détaillée de la menace
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statuts possibles d'un scan de sécurité
    /// </summary>
    public enum ScanStatus
    {
        /// <summary>
        /// En attente de scan
        /// </summary>
        Pending,
        
        /// <summary>
        /// Scan en cours
        /// </summary>
        Scanning,
        
        /// <summary>
        /// Scan complété avec succès, pas de menace détectée
        /// </summary>
        Clean,
        
        /// <summary>
        /// Scan complété avec menaces détectées
        /// </summary>
        Infected,
        
        /// <summary>
        /// Échec du scan
        /// </summary>
        Failed,
        
        /// <summary>
        /// Analyse suspendue ou limitée
        /// </summary>
        Limited
    }

    /// <summary>
    /// Types de menaces qui peuvent être détectées
    /// </summary>
    public enum ThreatType
    {
        /// <summary>
        /// Type inconnu
        /// </summary>
        Unknown,
        
        /// <summary>
        /// Virus
        /// </summary>
        Virus,
        
        /// <summary>
        /// Trojan
        /// </summary>
        Trojan,
        
        /// <summary>
        /// Spyware
        /// </summary>
        Spyware,
        
        /// <summary>
        /// Malware
        /// </summary>
        Malware,
        
        /// <summary>
        /// Adware
        /// </summary>
        Adware,
        
        /// <summary>
        /// Fichier suspect mais pas nécessairement malveillant
        /// </summary>
        Suspicious,
        
        /// <summary>
        /// Fichier non autorisé (ex: exécutable dans un contexte où ils sont interdits)
        /// </summary>
        Unauthorized,
        
        /// <summary>
        /// Format de fichier non supporté
        /// </summary>
        UnsupportedFormat
    }

    /// <summary>
    /// Niveaux de sévérité des menaces
    /// </summary>
    public enum SeverityLevel
    {
        /// <summary>
        /// Sévérité inconnue
        /// </summary>
        Unknown,
        
        /// <summary>
        /// Faible risque
        /// </summary>
        Low,
        
        /// <summary>
        /// Risque moyen
        /// </summary>
        Medium,
        
        /// <summary>
        /// Risque élevé
        /// </summary>
        High,
        
        /// <summary>
        /// Risque critique
        /// </summary>
        Critical
    }
}
