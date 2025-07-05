using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CommunityService.Hubs
{
    /// <summary>
    /// Hub SignalR pour les fonctionnalités de forum en temps réel
    /// </summary>
    public class ForumHub : Hub
    {
        /// <summary>
        /// Rejoint un groupe spécifique à un sujet du forum
        /// </summary>
        /// <param name="topicId">Identifiant du sujet</param>
        public async Task JoinTopic(string topicId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"topic_{topicId}");
        }

        /// <summary>
        /// Quitte un groupe de sujet
        /// </summary>
        /// <param name="topicId">Identifiant du sujet</param>
        public async Task LeaveTopic(string topicId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"topic_{topicId}");
        }

        /// <summary>
        /// Envoie un nouveau message dans un sujet
        /// </summary>
        /// <param name="topicId">Identifiant du sujet</param>
        /// <param name="message">Contenu du message</param>
        /// <param name="username">Nom de l'utilisateur</param>
        public async Task SendMessage(string topicId, string message, string username)
        {
            // Notification en temps réel envoyée à tous les membres du groupe
            await Clients.Group($"topic_{topicId}").SendAsync("ReceiveMessage", username, message);
        }

        /// <summary>
        /// Notifie quand un utilisateur commence à taper un message
        /// </summary>
        /// <param name="topicId">Identifiant du sujet</param>
        /// <param name="username">Nom de l'utilisateur</param>
        public async Task UserTyping(string topicId, string username)
        {
            await Clients.GroupExcept($"topic_{topicId}", Context.ConnectionId)
                .SendAsync("UserIsTyping", username);
        }

        /// <summary>
        /// Gère la connexion d'un utilisateur
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("UserOnline", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Gère la déconnexion d'un utilisateur
        /// </summary>
        /// <param name="exception">Exception si présente</param>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Clients.All.SendAsync("UserOffline", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
