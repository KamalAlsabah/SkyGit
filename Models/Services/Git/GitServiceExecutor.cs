
using Microsoft.Extensions.Options;
using SkyGit.Models.Git;
using System.Diagnostics;

namespace SkyGit.Models.Services.Git
{
    public class GitServiceExecutorParams
    {
        public string GitPath { get; set; }

        public string GitHomePath { get; set; }
        public string RepositoriesDirPath { get; set; }
    }

    public class GitServiceExecutor : IGitService
    {
        private static readonly string[] _permittedServiceNames = { "upload-pack", "receive-pack" };
        private readonly IOptions<GitServiceExecutorParams> gitParms;
        private readonly IWebHostEnvironment appEnvironment;
        private GitServiceExecutorParams _gitParms;
       
      
        public GitServiceExecutor(IOptions<GitServiceExecutorParams> gitParms, IWebHostEnvironment appEnvironment)
        {
            _gitParms=gitParms.Value;
            this.gitParms = gitParms;
            this.appEnvironment = appEnvironment;
        }
        public string GetRepositoryDirectoryPath(string repositoryName)
        {

            var directoryInfo = Path.Combine(appEnvironment.WebRootPath, _gitParms.RepositoriesDirPath, repositoryName);
            return directoryInfo;
        }
        public string GetGitExePath(string GitPath)
        {

            var directoryInfo = Path.Combine(appEnvironment.WebRootPath, GitPath);
            return directoryInfo;
        }
        public  void ExecuteServiceByName(
           string correlationId,
           string repositoryName,
           string serviceName,
           ExecutionOptions options,
           Stream inStream,
           Stream outStream)
        {
            var args = serviceName + " --stateless-rpc";
            args += options.ToCommandLineArgs();
            args += " \"" + GetRepositoryDirectoryPath(repositoryName) + "\"";

            var info = new ProcessStartInfo(GetGitExePath(_gitParms.GitPath), args)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(_gitParms.RepositoriesDirPath),
            };

            SetHomePath(info);
            ////database and Identity roles
            //var userid = HttpContext.Current.User.Id();
            //info.EnvironmentVariables.Add("AUTH_USER", userid);
            //info.EnvironmentVariables.Add("REMOTE_USER", userid);

            using (var process = Process.Start(info))
            {
                 inStream.CopyTo(process.StandardInput.BaseStream);
                if (options.endStreamWithClose)
                {
                    process.StandardInput.Close();
                }
                else
                {
                    process.StandardInput.Write('\0');
                }

                 process.StandardOutput.BaseStream.CopyTo(outStream);
                process.WaitForExit();
            }
        }

        private void SetHomePath(ProcessStartInfo info)
        {
            if (info.EnvironmentVariables.ContainsKey("HOME"))
            {
                info.EnvironmentVariables.Remove("HOME");
            }
            info.EnvironmentVariables.Add("HOME", _gitParms.GitHomePath);
        }
    }
}
