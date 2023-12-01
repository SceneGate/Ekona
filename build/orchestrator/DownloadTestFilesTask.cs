using System.Collections.ObjectModel;
using System.Text.Json;
using Cake.Common.IO;
using Cake.Common.Net;
using Cake.Core.Diagnostics;
using Cake.Frosting;

namespace BuildSystem;

[TaskName("Download-TestFiles")]
[TaskDescription("Download the test resource files")]
[IsDependeeOf(typeof(Cake.Frosting.PleOps.Recipe.Dotnet.TestTask))]
public class DownloadTestFilesTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        string resourcesPath = Path.GetFullPath("./resources");
        Environment.SetEnvironmentVariable("SCENEGATE_TEST_DIR", resourcesPath);

        if (Directory.Exists("resources")) {
            context.Log.Information("Test files already exists, skipping download.");
            return;
        }

        if (string.IsNullOrEmpty(context.TestResourceUri)) {
            context.Log.Information("Test resource uri is not present, skipping download.");
            return;
        }

        var jsonInfoPath = context.DownloadFile(context.TestResourceUri);
        string jsonInfoText = File.ReadAllText(jsonInfoPath.FullPath);
        IEnumerable<TestResource>? resources = JsonSerializer.Deserialize<IEnumerable<TestResource>>(jsonInfoText);
        if (resources is null) {
            throw new Exception("Failed to read json info file");
        }

        foreach (TestResource resource in resources) {
            var compressedResources = context.DownloadFile(resource.uri);
            context.Unzip(compressedResources, resource.path);
        }
    }

    private sealed record TestResource(string uri, string path);
}
