using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Frontend.Services.Interfaces;

namespace Frontend.Services
{
    /// <summary>
    /// DelegatingHandler that automatically attaches the JWT stored in LocalStorage (key "authToken")
    /// to outgoing HTTP requests.
    /// </summary>
    public class JwtAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authProvider;

        public JwtAuthorizationMessageHandler(ILocalStorageService localStorage, AuthenticationStateProvider authProvider)
        {
            _localStorage = localStorage;
            _authProvider = authProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
