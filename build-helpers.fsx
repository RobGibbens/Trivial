module BuildHelpers

open Fake
open Fake.XamarinHelper
open System
open System.IO
open System.Linq

let Exec command args =
    let result = Shell.Exec(command, args)

    if result <> 0 then failwithf "%s exited with error %d" command result

let RestorePackages solutionFile =
    Exec "tools/NuGet/NuGet.exe" ("restore " + solutionFile)
    solutionFile |> RestoreComponents (fun defaults -> {defaults with ToolPath = "tools/xpkg/xamarin-component.exe" })

let RunNUnitTests dllPath xmlPath =
    let testResultsFolder = "testResults"
    if Directory.Exists(testResultsFolder) then Directory.Delete(testResultsFolder, true)
    Directory.CreateDirectory(testResultsFolder) |> ignore
    Exec "/Library/Frameworks/Mono.framework/Versions/Current/bin/nunit-console4" (dllPath + " -xml=" + xmlPath)
    if File.Exists(xmlPath) then TeamCityHelper.sendTeamCityNUnitImport xmlPath

let RunUITests appPath =
    let testAppFolder = Path.Combine("testRuns", "apps")
    
    if Directory.Exists(testAppFolder) then Directory.Delete(testAppFolder, true)
    Directory.CreateDirectory(testAppFolder) |> ignore

    let testAppPath = Path.Combine(testAppFolder, DirectoryInfo(appPath).Name)

    Directory.Move(appPath, testAppPath)

    System.Console.WriteLine("Restoring packages from Trivial.UITests.sln")

    RestorePackages "Trivial.UITests.sln"

    MSBuild "Trivial/Trivial.Tests.UI/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Trivial.UITests.sln" ] |> ignore

    RunNUnitTests "Trivial/Trivial.Tests.UI/bin/Debug/Trivial.Tests.UI.dll" "Trivial/testRuns/testresults.xml"

let RunTestCloudTests appFile deviceList =
    System.Console.WriteLine("Restoring packages from Trivial.Tests.UI.sln")
    
    RestorePackages "Trivial/Trivial.Tests.UI/Trivial.Tests.UI.sln"

    MSBuild "Trivial/Trivial.Tests.UI/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Trivial/Trivial.Tests.UI/Trivial.Tests.UI.sln" ] |> ignore

    //TODO Don't hardcode token : let testCloudToken = Environment.GetEnvironmentVariable("env.TestCloudApiToken")
    let testCloudToken = "10591f4e6d90c77a4dc7e3a37d7fafc2"

    //TODO : Don't hardcode devices
    let args = String.Format(@"submit ""{0}"" {1} --devices 1726dcc9 --series ""master"" --locale ""en_US"" --user rob.gibbens@xamarin.com --fixture-chunk --assembly-dir ""Trivial/Trivial.Tests.UI/bin/Debug"" --nunit-xml testresults.xml", appFile, testCloudToken)

    System.Console.WriteLine("packages/Xamarin.UITest.1.0.0/tools/test-cloud.exe {0}", args)

    Exec "packages/Xamarin.UITest.1.0.0/tools/test-cloud.exe" args

    TeamCityHelper.sendTeamCityNUnitImport "testresults.xml"