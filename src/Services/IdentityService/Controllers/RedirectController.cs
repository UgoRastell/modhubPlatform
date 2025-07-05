using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly ILogger<RedirectController> _logger;

        public RedirectController(ILogger<RedirectController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("/signin-google")]
        public IActionResult GoogleSignIn([FromQuery] string code, [FromQuery] string state)
        {
            _logger.LogInformation("Received request on /signin-google, redirecting to /api/OAuth/google-callback");
            
            // Construire l'URL de redirection avec tous les paramètres de requête
            var queryString = Request.QueryString.Value;
            return Redirect($"/api/OAuth/google-callback{queryString}");
        }
    }
}
