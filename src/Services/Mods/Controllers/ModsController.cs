using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModsService.Controllers
{
    [ApiController]
    [Route("api/v1/mods")]
    public class ModsController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMods([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string sortBy = "recent")
        {
            // Implémentation simplifiée pour permettre l'accès public
            return Ok(new { 
                Success = true, 
                Message = "Mods récupérés avec succès", 
                Data = new { 
                    Items = new List<object>(), 
                    TotalCount = 0,
                    PageIndex = page,
                    PageSize = pageSize
                }
            });
        }
    }
}
