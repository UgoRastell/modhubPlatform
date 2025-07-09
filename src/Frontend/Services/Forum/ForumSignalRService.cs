using Frontend.Models.Forum; // import ForumPost model
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Frontend.Services.Forum
{
    public class ForumSignalRService : IAsyncDisposable
    {
        private readonly NavigationManager _navigationManager;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private HubConnection? _hubConnection;
        private string _currentTopicId = string.Empty;
        private string _currentUsername = string.Empty;
        
        // Événements que les composants peuvent écouter
        public event Action<ForumPost>? OnMessageReceived;
        public event Action<string>? OnUserTyping;
        public event Action<string>? OnUserOnline;
        public event Action<string>? OnUserOffline;
        
        public ForumSignalRService(NavigationManager navigationManager, AuthenticationStateProvider authenticationStateProvider)
        {
            _navigationManager = navigationManager;
            _authenticationStateProvider = authenticationStateProvider;
        }
        
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        
        public async Task StartConnectionAsync(string topicId)
        {
            if (_hubConnection != null)
            {
                // Si déjà connecté à un topic, quitter ce groupe
                if (!string.IsNullOrEmpty(_currentTopicId) && _currentTopicId != topicId)
                {
                    await _hubConnection.InvokeAsync("LeaveTopic", _currentTopicId);
                }
                
                // Si déjà connecté au bon topic, ne rien faire
                if (_currentTopicId == topicId && IsConnected)
                {
                    return;
                }
            }
            
            // Obtenir l'utilisateur authentifié
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            
            _currentUsername = user.Identity?.Name ?? "Anonyme";
            _currentTopicId = topicId;
            
            // Créer la connexion au hub
            string hubUrl = _navigationManager.BaseUri.TrimEnd('/') + "/hubs/forum";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // Configurer l'authentification si nécessaire
                })
                .WithAutomaticReconnect()
                .Build();
                
            // Configurer la réception des messages
            _hubConnection.On<ForumPost>("ReceiveMessage", (post) =>
            {
                OnMessageReceived?.Invoke(post);
            });
            
            _hubConnection.On<string>("UserIsTyping", (username) =>
            {
                OnUserTyping?.Invoke(username);
            });
            
            _hubConnection.On<string>("UserOnline", (connectionId) =>
            {
                OnUserOnline?.Invoke(connectionId);
            });
            
            _hubConnection.On<string>("UserOffline", (connectionId) =>
            {
                OnUserOffline?.Invoke(connectionId);
            });
            
            // Démarrer la connexion
            await _hubConnection.StartAsync();
            
            // Rejoindre le groupe du topic
            await _hubConnection.InvokeAsync("JoinTopic", topicId);
        }
        
        public async Task SendMessageAsync(string message)
        {
            if (_hubConnection is not null && !string.IsNullOrEmpty(_currentTopicId))
            {
                await _hubConnection.InvokeAsync("SendMessage", _currentTopicId, message, _currentUsername);
            }
        }
        
        public async Task SendUserTypingAsync()
        {
            if (_hubConnection is not null && !string.IsNullOrEmpty(_currentTopicId))
            {
                await _hubConnection.InvokeAsync("UserTyping", _currentTopicId, _currentUsername);
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                // Quitter le topic avant de se déconnecter
                if (!string.IsNullOrEmpty(_currentTopicId))
                {
                    await _hubConnection.InvokeAsync("LeaveTopic", _currentTopicId);
                }
                
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
