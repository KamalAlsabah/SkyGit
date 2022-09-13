using LibGit2Sharp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using SkyGit.Models.ApiResult;
using SkyGit.Models.Enums;
using SkyGit.Models.Git;
using SkyGit.Models.Services.Git;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Mime;

namespace SkyGit.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RepositryController : ControllerBase
    {
        private readonly IGitService gitService;

        public RepositryController(IGitService gitService)
        {
            this.gitService = gitService;
        }
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
        public async Task<IActionResult> Clone(string repositoryName,string service)
        {
            try
            {
                return await GetRepo(repositoryName, service);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }
        private async  Task<ActionResult> GetRepo(string repositoryName, string service)
        {
            try { 
            bool isPush = string.Equals("git-receive-pack", service, StringComparison.OrdinalIgnoreCase);

            //if (!RepositoryIsValid(repositoryName))
            //{
            //    //////Database And Identity
            //    //// This isn't a real repo - but we might consider allowing creation
            //    //if (isPush && UserConfiguration.Current.AllowPushToCreate)
            //    //{
            //    //    if (!RepositoryPermissionService.HasCreatePermission(User.Id()))
            //    //    {
            //    //        Log.Warning("GitC: User {UserId} is not allowed to do push-to-create", User.Id());
            //    //        return UnauthorizedResult();
            //    //    }
            //    //    if (!TryCreateOnPush(repositoryName))
            //    //    {
            //    //        return UnauthorizedResult();
            //    //    }
            //    //}
            //    //else
            //    //{
            //    return NotFound();
            //    //}
            //}

            var requiredLevel = isPush ? RepositoryAccessLevel.Push : RepositoryAccessLevel.Pull;
                //if (RepositoryPermissionService.HasPermission(User.Id(), repositoryName, requiredLevel))
                //{
                Response.Headers.TryAdd("Expires", "Fri, 01 Jan 1980 00:00:00 GMT");
                Response.Headers.TryAdd("Pragma", "no-cache");
                Response.Headers.TryAdd("Cache-Control", "no-cache, max-age=0, must-revalidate");
                Response.HttpContext.Features.Get<IHttpResponseBodyFeature>().DisableBuffering();
                Response.Headers.AcceptCharset = "";
                return await GetInfoRefs(repositoryName, service);
                //}
                //else
                //{

                //    return BadRequest();
                //}
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }


        //private static DirectoryInfo GetDirectoryInfo(String repositoryName)
        //{
        //    return new DirectoryInfo(Path.Combine(UserConfiguration.Current.Repositories, repositoryName));
        //}

        //private static bool RepositoryIsValid(string repositoryName)
        //{
        //    var directory = GetDirectoryInfo(repositoryName);
        //    var isValid = Repository.IsValid(directory.FullName);
           
        //    return isValid;
        //}

        private async Task< ActionResult> GetInfoRefs(string repositoryName, string service)
        {
            try { 
            string contentType = string.Format("application/x-{0}-advertisement", service);
                Response.ContentType = contentType;

                string serviceName = service.Substring(4);
            string advertiseRefsContent = FormatMessage(string.Format("# service={0}\n", service)) + FlushMessage();
                var _instream = GetInputStream();
                var GitCmdResult= new GitCmdResult(
                contentType,
                 (outStream) =>
                 {
                    
                     gitService.ExecuteServiceByName(
                         Guid.NewGuid().ToString("N"),
                         repositoryName,
                         serviceName,
                         new ExecutionOptions() { AdvertiseRefs = true },
                         _instream,
                         outStream
                     );
                 },
                advertiseRefsContent);

                return GitCmdResult;
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }
        private static string FormatMessage(string input)
        {
            return (input.Length + 4).ToString("X").PadLeft(4, '0') + input;
        }
        private static string FlushMessage()
        {
            return "0000";
        }
        private Stream GetInputStream(bool disableBuffer = false)
        {
            try
            {
                // For really large uploads we need to get a bufferless input stream and disable the max
                // request length.
                if (!disableBuffer)
                {
                    HttpContext.Request.EnableBuffering();
                }
                Stream stream= Request.Headers["Content-Encoding"] == "gzip" ?
                    new GZipStream(HttpContext.Request.Body, CompressionMode.Decompress) :
                    HttpContext.Request.Body;
                return stream;
            }
            catch(Exception ex)
            {
                return null;
            }
        }
    }
}
