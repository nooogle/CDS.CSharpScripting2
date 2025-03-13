///////////////////////////////////////////////////////////////////////////////
// VARIABLES
///////////////////////////////////////////////////////////////////////////////

var solutionName = "CDS.CSharpScripting2.sln";
var artifactsDir = "./artifacts";

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = "Release"; // Always using Release as requested

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    Information($"Starting build process for {solutionName}...");
    // Ensure artifacts directory exists
    EnsureDirectoryExists(artifactsDir);
});

Teardown(ctx =>
{
    Information($"Finished build process for {solutionName}.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Description("Cleans all build artifacts and directories")
    .Does(() =>
    {
        CleanDirectories("**/bin");
        CleanDirectories("**/obj");
        CleanDirectories(artifactsDir);
        
        Information("Cleaned directories.");
    });

Task("Restore")
    .Description("Restores NuGet packages")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        Information($"Restoring NuGet packages for {solutionName}...");
        DotNetRestore(solutionName);
    });

Task("Build")
    .Description("Builds the solution")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        Information($"Building {solutionName} ({configuration})...");
        DotNetBuild(solutionName, new DotNetBuildSettings
        {
            Configuration = configuration,
            NoRestore = true,
            ArgumentCustomization = args => args.Append("--no-incremental")
        });
    });

Task("Test")
    .Description("Runs all tests")
    .IsDependentOn("Build")
    .Does(() =>
    {
        Information("Running tests...");
        var testProjects = GetFiles("./**/*Tests.csproj");
        
        if (testProjects.Count == 0)
        {
            Warning("No test projects found.");
            return;
        }
        
        foreach (var project in testProjects)
        {
            Information($"Testing {project.GetFilenameWithoutExtension()}...");
            DotNetTest(project.FullPath, new DotNetTestSettings
            {
                Configuration = configuration,
                NoBuild = true,
                NoRestore = true,
                Verbosity = DotNetVerbosity.Normal,
                ResultsDirectory = $"{artifactsDir}/testresults"
            });
        }
    });

Task("Default")
    .Description("Performs a complete build including tests")
    .IsDependentOn("Test");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
