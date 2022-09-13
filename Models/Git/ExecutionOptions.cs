namespace SkyGit.Models.Git
{
    
    public class ExecutionOptions
    {
        public bool AdvertiseRefs { get; set; }
        public bool endStreamWithClose = false;

        public string ToCommandLineArgs()
        {
            var args = "";
            if (this.AdvertiseRefs)
            {
                args += " --advertise-refs";
            }
            return args;
        }
    }
}
