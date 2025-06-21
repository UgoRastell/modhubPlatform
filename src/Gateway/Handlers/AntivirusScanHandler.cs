using System.Net;

namespace Gateway.Handlers;

public class AntivirusScanHandler : DelegatingHandler
{
    private readonly ILogger<AntivirusScanHandler> _logger;

    public AntivirusScanHandler(ILogger<AntivirusScanHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AntivirusScanHandler: Traitement d'une demande d'upload");
        
        if (request.Content != null && request.Content.Headers.ContentType != null)
        {
            // Vérifier si le contenu est un multipart/form-data (fichier uploadé)
            if (request.Content.Headers.ContentType.MediaType == "multipart/form-data")
            {
                try
                {
                    // Dans une implémentation réelle, le contenu serait lu et envoyé à un service antivirus
                    // Pour la démonstration, nous allons simuler un scan
                    
                    _logger.LogInformation("AntivirusScanHandler: Simulation du scan antivirus...");
                    
                    // Simulation d'un délai de scan
                    await Task.Delay(500, cancellationToken);
                    
                    // Si nous avions un service d'antivirus réel, nous traiterions ici les fichiers infectés
                    // Pour l'exemple, nous utilisons une probabilité aléatoire de détecter un virus
                    var random = new Random();
                    bool virusDetected = random.Next(100) < 1; // 1% de chance de détecter un virus pour la démo
                    
                    if (virusDetected)
                    {
                        _logger.LogWarning("AntivirusScanHandler: Virus détecté dans le fichier uploadé!");
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent("Le fichier uploadé contient un virus ou un logiciel malveillant")
                        };
                    }
                    
                    _logger.LogInformation("AntivirusScanHandler: Aucun virus détecté, poursuite du traitement.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AntivirusScanHandler: Erreur lors du scan antivirus");
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("Erreur lors du scan antivirus du fichier")
                    };
                }
            }
        }
        
        // Si tout est clean ou s'il n'y a pas de contenu à scanner, poursuivre la requête
        return await base.SendAsync(request, cancellationToken);
    }
}
