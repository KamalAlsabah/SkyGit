using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.PortableExecutable;
using System.Collections.Generic;

namespace SkyGit.Models.ApiResult
{
    public class GitCmdResult:ActionResult
    {
        private readonly string contentType;
        private readonly string advertiseRefsContent;
        private readonly Action<Stream> executeGitCommand;

        public GitCmdResult(string contentType, Action<Stream> executeGitCommand)
            : this(contentType, executeGitCommand, null)
        {
        }

        public GitCmdResult(string contentType, Action<Stream> executeGitCommand, string advertiseRefsContent)
        {
            this.contentType = contentType;
            this.advertiseRefsContent = advertiseRefsContent;
            this.executeGitCommand = executeGitCommand;
        }
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            try
            {


                if (context == null)
                    throw new ArgumentNullException("context");

                HttpResponse? response = context.HttpContext.Response;

                if (advertiseRefsContent != null)
                {
                    await response.WriteAsync(advertiseRefsContent);
                }

                // SetNoCache

                response.Headers.TryAdd("Expires", "Fri, 01 Jan 1980 00:00:00 GMT");
                response.Headers.TryAdd("Pragma", "no-cache");
                response.Headers.TryAdd("Cache-Control", "no-cache, max-age=0, must-revalidate");

                response.HttpContext.Features.Get<IHttpResponseBodyFeature>().DisableBuffering();
                response.Headers.AcceptCharset = "";
                response.ContentType = contentType;
                executeGitCommand(response.Body);
            }catch(Exception ex)
            {
                return;
            }
        }
       
    }
}
