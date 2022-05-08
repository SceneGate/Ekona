#load "nuget:?package=PleOps.Cake&version=0.7.0"

string testResourceUri = Argument("resource-uri", string.Empty);

Task("Define-Project")
    .Description("Fill specific project information")
    .Does<BuildInfo>(info =>
{
    info.AddLibraryProjects("Ekona");
    info.AddTestProjects("Ekona.Tests");

    info.PreviewNuGetFeed = "https://pkgs.dev.azure.com/SceneGate/SceneGate/_packaging/SceneGate-Preview/nuget/v3/index.json";

    info.CoverageTarget = 85;
});

Task("Download-TestFiles")
    .Description("Download the test resource files")
    .IsDependeeOf("Test")
    .Does(() =>
{
    string resourcesPath = MakeAbsolute(Directory("./resources")).FullPath;

    if (DirectoryExists("resources")) {
        Information("Test files already exists, skipping download.");
        System.Environment.SetEnvironmentVariable("SCENEGATE_TEST_DIR", resourcesPath);
        return;
    }

    if (string.IsNullOrEmpty(testResourceUri)) {
        Information("Test resource uri is not present, skipping download.");
        return;
    }

    var jsonInfoPath = DownloadFile(testResourceUri);
    string jsonInfoText = System.IO.File.ReadAllText(jsonInfoPath.FullPath);
    IEnumerable<TestResource> resources = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<TestResource>>(jsonInfoText);

    foreach (TestResource resource in resources) {
        var compressedResources = DownloadFile(resource.uri);
        Unzip(compressedResources, resource.path);
    }

    System.Environment.SetEnvironmentVariable("SCENEGATE_TEST_DIR", resourcesPath);
});

Task("Default")
    .IsDependentOn("Stage-Artifacts");

string target = Argument("target", "Default");
RunTarget(target);

private sealed record TestResource(string uri, string path);
