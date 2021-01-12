using System;
using System.IO;
using System.Linq;
using System.Threading;
using DotNetEnv;
using JenkinsNET;
using JenkinsNET.Models;
using JenkinsNET.Utilities;
using Serilog;

namespace JenkinsRemoteBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupEnvironment();
            Log.Information("Jenkins Remote Builder");
            ExecuteJenkinsJob();
        }

        private static void ExecuteJenkinsJob()
        {
            var jobName = Env.GetString("JOB");
            var client = new JenkinsClient(Env.GetString("JENKINS_HOST"))
            {
                UserName = Env.GetString("JENKINS_USER"),
                ApiToken = Env.GetString("JENKINS_API_KEY")
            };
            client.UpdateSecurityCrumb();
            if(Env.GetBool("WAIT_FOR_COMPLETION"))
            {
                var jenkinsJobRunner = new JenkinsJobRunner(client) {BuildTimeout = Env.GetInt("BUILD_TIMEOUT", 600)};

                Log.Information("Starting build");
                var buildBase = jenkinsJobRunner.Run(jobName);
                if (Env.GetBool("TERMINATE_ON_FAILURE") && buildBase.Result != "SUCCESS")
                {
                    Log.Error("Build status was " + buildBase.Result);
                    Environment.Exit(1);
                }

                if (Env.GetBool("DOWNLOAD_ARTIFACT"))
                {
                    if (buildBase.Id != null)
                        DownloadArtifact(client, jobName, buildBase.Id.Value, Env.GetString("ARTIFACT_NAME"));
                }
            }
            else
            {
                client.Jobs.Build(jobName);
            }
            Log.Information("Completed");
        }
        
        private static void DownloadArtifact(IJenkinsClient client, string jobName, int buildNumber, string fileName)
        {
            using var stream = client.Artifacts.Get(jobName, buildNumber.ToString(), fileName);
            using var fileStream = new FileStream($"./" + fileName, FileMode.Create,
                FileAccess.Write);
            stream.CopyTo(fileStream);
            Log.Information("Downloaded artifact to: " + fileStream.Name);
        }

        private static void SetupEnvironment()
        {
            Env.TraversePath().Load();
            CreateLogger();
        }

        private static void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug()
                .CreateLogger();
        }
    }
}