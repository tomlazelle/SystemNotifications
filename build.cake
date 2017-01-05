#tool "nuget:?package=GitVersion.CommandLine"

var buildTarget = Argument("target","Default");
var configuration = Argument("configuration", "Release");

var workingFolder = Directory("./").ToString();
var buildDir = Directory("./SystemNotifications/bin/") + Directory(configuration);
var sln = "./SystemNotifications.sln";

var appName = "SystemNotifications";
var version = "1.0.0.0";

Task("Default").IsDependentOn("Push-Nuget");

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);

    if(DirectoryExists("./nuget"))
        {
            CleanDirectory("./nuget");
        }
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore(sln);
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    
      // Use MSBuild
      MSBuild(sln, settings =>
        settings.SetConfiguration(configuration));
    
});


Task("Create-Directory")
.IsDependentOn("Build")
.Does(()=>{

  EnsureDirectoryExists("nuget");
});


var relativeVersion = "0.1.99-local1";

Task("GetVersionInfo")
.IsDependentOn("Create-Directory")
    .Does(() =>
{
    var buildnumber = EnvironmentVariable("BUILD_NUMBER");

    if(buildnumber != null && buildnumber != ""){
        relativeVersion = string.Format("0.1.{0}", buildnumber);
    }
});

Task("Pack")
  .IsDependentOn("GetVersionInfo")
  .Does(() => {

var info = ParseAssemblyInfo("./SystemNotifications/Properties/AssemblyInfo.cs");

    
    Information(info.Title);
    Information(info.Description);
    Information(info.Product);

       var nuGetPackSettings   = new NuGetPackSettings {
                                    Id                      = info.Product,
                                    Version                 = relativeVersion,
                                    Title                   = info.Title,
                                    Authors                 = new[] {"Tom La Zelle"},
                                    Description             = info.Description,
                                    Summary                 = info.Description,
                                    ProjectUrl              = new Uri("https://github.com/tomlazelle/SystemNotifications.git"),
                                    Files                   = new [] {
                                                                        new NuSpecContent {Source = @"SystemNotifications.*" ,Target = @"lib\net45\"},
                                                                      },
                                    Dependencies = new []{                                        
                                        new NuSpecDependency{
                                            Id="StructureMap",
                                            TargetFramework = "net452"
                                        }
                                    },
                                    BasePath                = "./SystemNotifications/bin",
                                    OutputDirectory         = "./nuget"
                                };

    NuGetPack(nuGetPackSettings);
});

 //43b006e1-e30a-4a0e-af42-b04ac797ce0a

 Task("Push-Nuget")
 .IsDependentOn("Pack")
 .Does(()=>{
// Get the path to the package.
var package = "./nuget/SystemNotifications." + relativeVersion + ".nupkg";
            
// Push the package.
NuGetPush(package, new NuGetPushSettings {
    Source = "https://www.myget.org/F/tomlazelle/api/v2/package",
    ApiKey = "43b006e1-e30a-4a0e-af42-b04ac797ce0a"
});

 });

RunTarget(buildTarget);