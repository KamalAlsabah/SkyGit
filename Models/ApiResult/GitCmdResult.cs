using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.PortableExecutable;
using System.Collections.Generic;
using System.Diagnostics;

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
               
               
                

                
                executeGitCommand(response.Body);
            }catch(Exception ex)
            {
                return;
            }
        }
       
    }
}
