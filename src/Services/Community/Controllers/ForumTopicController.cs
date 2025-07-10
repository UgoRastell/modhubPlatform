using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Community.Controllers
{
    [ApiController]
    [Route("api/forum")]
    public class ForumTopicController : ControllerBase
    {
        // TODO: inject real IForumService when implemented
        public ForumTopicController()
        {
        }

        /// <summary>
        /// Recherche de sujets dans le forum (accès public).
        /// Pour l'instant renvoie une collection vide tant que l'implémentation du service n'est pas prête.
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public ActionResult<PagedResult<ForumTopicSearchViewModel>> SearchTopics(
            [FromQuery] string query = "",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // Placeholder : renvoyer une liste vide pour débloquer le frontend sans 401
            var result = new PagedResult<ForumTopicSearchViewModel>
            {
                Items = new List<ForumTopicSearchViewModel>(),
                Page = page,
                PageSize = pageSize,
                TotalItems = 0,
                TotalPages = 0
            };
            return Ok(result);
        }

        #region internal DTOs (temporaire)
        public class ForumTopicSearchViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string AuthorName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }

        public class PagedResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
        }
        #endregion
    }
}
