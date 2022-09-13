using SkyGit.Models.Git;

namespace SkyGit.Models.Services.Git
{
    public interface IGitService
    {
        void ExecuteServiceByName(string correlationId, string repositoryName, string serviceName, ExecutionOptions options, Stream inStream, Stream outStream);
        string GetRepositoryDirectoryPath(string repositoryName);

    }
}
