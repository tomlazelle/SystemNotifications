#tool "nuget:?package=GitVersion.CommandLine"

var buildTarget = Argument("target","Default");
var configuration = Argument("configuration", "Release");

var workingFolder = Directory("./").ToString();
var buildDir = Directory("./SystemNotifications/bin/") + Directory(configuration);
var sln = "./SystemNotifications.sln";

var appName = "SystemNotifications";


Task("Default").IsDependentOn("Push-Nuget");

Task("Clean")
    .Does(() =>
{
    EnsureDirectoryExists("nuget");

    CleanDirectory(buildDir);

    if(DirectoryExists("./nuget"))
    {
            CleanDirectory("./nuget");
    }
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore("./SystemNotifications.sln");
});

var relativeVersion = "1.0.0.0";

Task("GetVersionInfo")
.IsDependentOn("Restore")
.Does(() => {

    var versionInfo = XmlPeek("./SystemNotifications/SystemNotifications.csproj", "/Project/PropertyGroup/AssemblyVersion");

    var ver = new System.Version(versionInfo);

    var major = ver.Major;
    var minor = ver.Minor;
    var build = ver.Build;
    var rev = ver.Revision;

    var buildnumber = EnvironmentVariable("BUILD_NUMBER");

    if(buildnumber != null && buildnumber != "")
    {
        relativeVersion = System.String.Format("{0}.{1}.{2}.{3}",major,minor,buildnumber,rev);
    }
    else
    {
        rev += 1;

        relativeVersion = System.String.Format("{0}.{1}.{2}.{3}",major,minor,build,rev);
        
        XmlPoke("./SystemNotifications/SystemNotifications.csproj", "/Project/PropertyGroup/AssemblyVersion",relativeVersion);
        XmlPoke("./SystemNotifications/SystemNotifications.csproj", "/Project/PropertyGroup/AssemblyFileVersion",relativeVersion);
        XmlPoke("./SystemNotifications/SystemNotifications.csproj", "/Project/PropertyGroup/Version",relativeVersion);
        
        Information(relativeVersion);
    }

    
});

Task("Build")
.IsDependentOn("GetVersionInfo")
.Does(() =>
{
    var parameters = new []{
    @"build",
    @"/p:AssemblyVersion=" + relativeVersion,
    @"/p:Version=" + relativeVersion,
    @"--configuration Release"
  };

  Information(string.Join(" ",parameters));

  StartProcess(@"dotnet ", string.Join(" ",parameters) + " ");
});


Task("Pack")
.IsDependentOn("Build")
.Does(() => {
    var parameters = new []{
    @"pack",
    @"--output ..\nuget",
    @"-c Release",
    @"/p:Version=" + relativeVersion
  };
    Information(string.Join(" ",parameters));
  StartProcess(@"dotnet ", string.Join(" ",parameters) + " ");
});


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