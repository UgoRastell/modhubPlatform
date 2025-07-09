using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

using CommunityService.Services.Forums;
using CommunityService.Models.Forum;

namespace CommunityService.Hubs
{
    /// <summary>
    /// Hub SignalR pour les fonctionnalités de forum en temps réel
    /// </summary>
    public class ForumHub : Hub
    {
        private readonly IForumService _forumService;

        public ForumHub(IForumService forumService)
        {
            _forumService = forumService;
        }
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
            // 1. Persister le message dans MongoDB via le service domaine
            ForumPost? forumPost = null;
    try
            {
                var topic = await _forumService.GetTopicByIdAsync(topicId);
                if (topic is null)
                {
                    await Clients.Caller.SendAsync("Error", "Topic introuvable");
                    return;
                }

                forumPost = new ForumPost
                {
                    TopicId = topicId,
                    CategoryId = topic.CategoryId,
                    Content = message,
                    CreatedByUsername = username,
                    CreatedByUserId = Context.ConnectionId
                };

                await _forumService.CreatePostAsync(forumPost);

        // 2. Diffuser le message aux membres du groupe après succès
        await Clients.Group($"topic_{topicId}").SendAsync("ReceiveMessage", forumPost);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            // 2. Diffuser le message aux membres du groupe
            await Clients.Group($"topic_{topicId}").SendAsync("ReceiveMessage", forumPost);
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
