namespace Shared.Models;

/// <summary>
/// Classe de base pour tous les modèles partagés
/// </summary>
public abstract class BaseModel
{
    public string Id { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}
