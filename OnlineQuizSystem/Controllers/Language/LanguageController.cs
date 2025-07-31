using Application.DataTransferModels.ResponseModel;
using CommonOperations.Constants;
using CommonOperations.Methods;
using Microsoft.AspNetCore.Mvc;

namespace PresentationAPI.Controllers.Language
{
    [Route("api/language")]
    [ApiController]
    public class LanguageController : Controller
    {
        [HttpGet]
        public IActionResult GetLanguages()
        {
            ResponseVM response = ResponseVM.Instance;

            var results = Methods.ExecuteStoredProceduresList("SP_GetLanguages", null!);
            var languages = results.Result.Select(lang => (string)lang.Language).ToList();

            if (!results.Result.Any())
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "No languages found.";
                return NotFound(response);
            }
            response.StatusCode = ResponseCode.Success;
            response.Data = languages;
            response.ResponseMessage = "Success!";

            return Ok(response);
        }
    }
}
