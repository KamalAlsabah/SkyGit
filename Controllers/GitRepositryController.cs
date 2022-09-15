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
    [Route("api/[controller]")]
    [ApiController]
    public class GitRepositryController : ControllerBase
    {
        private readonly IGitService gitService;

        public GitRepositryController(IGitService gitService)
        {
            this.gitService = gitService;
        }
        [HttpGet("CreateRepo/{name}")]
        public IActionResult CreateRepo(string name)
        {
            string path = LibGit2Sharp.Repository.Init($"wwwroot\\Repositories\\{name}", true);
            return Ok(path);
        }
        [HttpGet("GetRepoUrl/{name}")]
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
        //عند عمل كلون او بوش او جيت يتوجه لهذه الدالة
        public async Task<IActionResult> SecureGetInfoRefs(string repositoryName,string service)
        {
            try
            {
                return await ActionsOnRepo(repositoryName, service);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }
        //pull or clone
        [HttpPost("/{repositoryName}.git/git-upload-pack")]
        public async Task<ActionResult> SecureUploadPack(String repositoryName)
        {
            //if (!RepositoryIsValid(repositoryName))
            //{
            //    return new HttpNotFoundResult();
            //}

            //if (RepositoryPermissionService.HasPermission(User.Id(), repositoryName, RepositoryAccessLevel.Pull))
            //{
                return await ExecuteUploadPack(repositoryName);
            //}
            //else
            //{
            //    return UnauthorizedResult();
            //}
        }
        //push Data
        [HttpPost("/{repositoryName}.git/git-receive-pack")]
        public async Task<ActionResult> SecureReceivePack(String repositoryName)
        {
            //if (!RepositoryIsValid(repositoryName))
            //{
            //    return new HttpNotFoundResult();
            //}

            //if (RepositoryPermissionService.HasPermission(User.Id(), repositoryName, RepositoryAccessLevel.Push))
            //{
            return await ExecuteReceivePack(repositoryName);
            //}
            //else
            //{
            //    return UnauthorizedResult();
            //}
        }
















        private async  Task<ActionResult> ActionsOnRepo(string repositoryName, string service)
        {
            try { 
                //معرفة نوع الاكشن بوش او بول
            bool isPush = string.Equals("git-receive-pack", service, StringComparison.OrdinalIgnoreCase);
                //فالديشن على الريبو والصلاحيات بالداتا
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
        private async Task<ActionResult> ExecuteUploadPack(string repositoryName)
        {
            var instream = await GetInputStream(disableBuffer: true);

            return new GitCmdResult(
                "application/x-git-upload-pack-result",
                (outStream) =>
                {
                    gitService.ExecuteGitUploadPack(
                        Guid.NewGuid().ToString("N"),
                        repositoryName,
                        instream,
                        outStream);
                });
        }
        private async Task<ActionResult> ExecuteReceivePack(string repositoryName)
        {
            var instream =await GetInputStream(disableBuffer: true);
            return new GitCmdResult(
                "application/x-git-receive-pack-result",
                (outStream) =>
                {
                    gitService.ExecuteGitReceivePack(
                        Guid.NewGuid().ToString("N"),
                        repositoryName,
                        instream,
                        outStream);
                });
        }
        //كلون او بول
        private async Task<ActionResult> GetInfoRefs(string repositoryName, string service)
        {
            try { 
                string contentType = string.Format("application/x-{0}-advertisement", service);
                Response.Headers.TryAdd("Expires", "Fri, 01 Jan 1980 00:00:00 GMT");
                Response.Headers.TryAdd("Pragma", "no-cache");
                Response.Headers.TryAdd("Cache-Control", "no-cache, max-age=0, must-revalidate");
                Response.HttpContext.Features.Get<IHttpResponseBodyFeature>().DisableBuffering();
                Response.Headers.AcceptCharset = "";
                Response.ContentType = contentType;
                string serviceName = service.Substring(4);
                string advertiseRefsContent = FormatMessage(string.Format("# service={0}\n", service)) + FlushMessage();

                Stream _instream;
                if(Request.Headers["Content-Encoding"] == "gzip")
                { _instream = new GZipStream(HttpContext.Request.Body, CompressionMode.Decompress);
                }else
                {
                    _instream = HttpContext.Request.Body;
                }
            

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
        private async Task<Stream> GetInputStream(bool disableBuffer = false)
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
