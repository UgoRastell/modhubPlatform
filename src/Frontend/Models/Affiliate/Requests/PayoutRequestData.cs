namespace Frontend.Models.Affiliate.Requests
{
    /// <summary>
    /// Représente une demande de paiement faite par un affilié
    /// </summary>
    public class PayoutRequestData
    {
        /// <summary>
        /// Montant demandé pour le paiement (en centimes)
        /// </summary>
        public required decimal Amount { get; set; }
        
        /// <summary>
        /// Méthode de paiement préférée
        /// </summary>
        public required string PaymentMethod { get; set; }
        
        /// <summary>
        /// Informations de compte pour le paiement (email PayPal, IBAN, etc.)
        /// </summary>
        public required string PaymentDetails { get; set; }
        
        /// <summary>
        /// Notes ou instructions supplémentaires pour le paiement
        /// </summary>
        public string? Notes { get; set; }
    }
}
