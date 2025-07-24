using CommonOperations.Methods;
using Microsoft.AspNetCore.Mvc;

namespace PresentationAPI.Controllers.Language
{
    [Route("api/[controller]")]
    [ApiController]
    public class LanguageController : Controller
    {
        [HttpGet]
        [HttpGet]
        public IActionResult GetLanguages()
        {
            var results = Methods.ExecuteStoredProceduresList("SP_GetLanguages", null!);

            if (!results.Result.Any())
            {
                return NotFound(new { Message = "No languages found." });
            }

            var languages = results.Result.Select(lang => (string)lang.Language).ToList();

            return Ok(languages);
        }

    }
}
