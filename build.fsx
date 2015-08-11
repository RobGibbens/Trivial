#r @"packages/FAKE.4.1.0/tools/FakeLib.dll"
#load "build-helpers.fsx"
open Fake
open System
open System.IO
open System.Linq
open BuildHelpers
open Fake.XamarinHelper

//DONE
Target "core-build" (fun () ->
    RestorePackages "Trivial.Core.sln"

    MSBuild "Trivial/Trivial.Core/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Trivial.Core.sln" ] |> ignore
)

//DONE
Target "core-tests" (fun () -> 
    RestorePackages "Trivial.Core.sln"
    
    MSBuild "Trivial/Trivial.Core/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Trivial.Core.sln" ] |> ignore
    
    RunNUnitTests "Trivial/Trivial.Tests.Unit/bin/Debug/Trivial.Tests.Unit.dll" "Trivial/testRuns/testresults.xml"
)

//DONE
Target "ios-build" (fun () ->
    RestorePackages "Trivial.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "Trivial.iOS.sln"
            Configuration = "Debug|iPhoneSimulator"
            Target = "Build"
        })
)

//TODO :  Needs publishing profile
Target "ios-adhoc" (fun () ->
    RestorePackages "Trivial.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "Trivial.iOS.sln"
            Configuration = "Ad-Hoc|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("Trivial", "Trivial.iOS", "bin", "iPhone", "Ad-Hoc"), "*.ipa").First()

    TeamCityHelper.PublishArtifact appPath
)

//TODO : Needs publishing profile
Target "ios-appstore" (fun () ->
    RestorePackages "Trivial.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "Trivial.iOS.sln"
            Configuration = "AppStore|iPhone"
            Target = "Build"
        })

    let outputFolder = Path.Combine("Trivial", "Trivial.iOS", "bin", "iPhone", "AppStore")
    let appPath = Directory.EnumerateDirectories(outputFolder, "*.app").First()
    let zipFilePath = Path.Combine(outputFolder, "Trivial.iOS.zip")
    let zipArgs = String.Format("-r -y '{0}' '{1}'", zipFilePath, appPath)

    Exec "zip" zipArgs

    TeamCityHelper.PublishArtifact zipFilePath
)

//TODO : Test
Target "ios-uitests" (fun () ->
    let appPath = Directory.EnumerateDirectories(Path.Combine("Trivial", "Trivial.iOS", "bin", "iPhoneSimulator", "Debug"), "*.app").First()

    RunUITests appPath
)

//TODO : Test
Target "ios-testcloud" (fun () ->
    let appPath = Directory.EnumerateFiles(Path.Combine("Trivial", "Trivial.iOS", "bin", "iPhone", "Debug"), "*.ipa").First()

    getBuildParam "devices" |> RunTestCloudTests appPath
)

//DONE
Target "android-build" (fun () ->
    RestorePackages "Trivial.Droid.sln"

    MSBuild "Trivial/Trivial.Droid/bin/Release" "Build" [ ("Configuration", "Release") ] [ "Trivial.Droid.sln" ] |> ignore
)

//TODO : Needs keystore
Target "android-package" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "Trivial/Trivial.Droid/Trivial.Droid.csproj"
            Configuration = "Release"
            OutputPath = "Trivial/Trivial.Droid/bin/Release"
        }) 
    |> AndroidSignAndAlign (fun defaults ->
        {defaults with
            KeystorePath = "Trivial.keystore"
            KeystorePassword = "Trivial" // TODO: don't store this in the build script for a real app!
            KeystoreAlias = "Trivial"
        })
    |> fun file -> TeamCityHelper.PublishArtifact file.FullName
)

//TODO : Test
Target "android-uitests" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "Trivial/Trivial.Droid/Trivial.Droid.csproj"
            Configuration = "Release"
            OutputPath = "Trivial/Trivial.Droid/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("Trivial", "Trivial.Droid", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

    RunUITests appPath
)

//TODO : Test
Target "android-testcloud" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "Trivial/Trivial.Droid/Trivial.Droid.csproj"
            Configuration = "Release"
            OutputPath = "Trivial/Trivial.Droid/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("Trivial", "Trivial.Droid", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

    getBuildParam "devices" |> RunTestCloudTests appPath
)

"core-build"
  ==> "core-tests"

"ios-build"
  ==> "ios-uitests"

"ios-testcloud"
  ==> "ios-uitests"
  
"android-build"
  ==> "android-uitests"

"android-build"
  ==> "android-testcloud"

"android-build"
  ==> "android-package"

RunTarget() 