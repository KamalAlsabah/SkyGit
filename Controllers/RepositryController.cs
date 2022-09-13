using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SkyGit.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RepositryController : ControllerBase
    {
        [HttpGet]
        public IActionResult Create(string name)
        {
            string path = LibGit2Sharp.Repository.Init($"wwwroot\\Repositories\\{name}", true);
            return Ok(path);
        }
        [HttpGet]

        public IActionResult GetRepoUrl(string name)
        {
            string serverAddress = string.Format("{0}://{1}/{2}{3}",
                                    HttpContext.Request.Scheme,
                                     HttpContext.Request.Host,
                                     name,
                                     ".git"
                                     );

            return Ok(serverAddress);
        }
        [HttpGet("/{repositoryName}.git/info/refs")]
        public IActionResult CloneTo(string repositoryName)
        {
            return Ok();
        }
    }
}
