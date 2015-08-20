#I @"dependencies\FAKE.Core\tools\"
#r "FakeLib.dll"
 
open Fake
open Fake.AssemblyInfoFile
open System.IO
open System
open System.Text.RegularExpressions

let pathConcat a = String.concat (string Path.DirectorySeparatorChar) a

let config = getBuildParamOrDefault "config" "Release"
let coreProjPath = "src" </> "Org.Interactivity.Recognizer"
let buildSlns = [ "src" </> "GestureRecognizer.sln" ]
let outputPath = "output" </> config
let mainOutputPath = outputPath </> "Org.Interactivity.Recognizer"
let projectId = "WPFGestureRecognizer"
let packagePath = outputPath </> "nuget-package" 
let projectName = "WPF Gesture Recognizer"
let projectDescription = "WPF interactivity trigger, running actions when swipe and/or tap gestures are detected."
let projectOwners = [ "Rod Landaeta" ]

let releaseInfo =
    ReadFile "release_notes.md"
    |> ReleaseNotesHelper.parseReleaseNotes

Target "Clean" <| fun _ ->
    CleanDirs [ outputPath ]
    
Target "Version" <| fun _ ->
    let ver = releaseInfo.AssemblyVersion
    let gitHash = Git.Information.getCurrentHash()
    printfn "##teamcity[buildNumber '%s']" ver
    let productString = sprintf "%s %s (%s)" projectName ver gitHash
        
    CreateCSharpAssemblyInfo
        (pathConcat [coreProjPath; "Properties"; "AssemblyInfo.cs" ])
        [ Attribute.Title productString
          Attribute.Description productString
          Attribute.Guid "c93fad81-4f16-40b0-b65a-383e1d1ef26e"
          Attribute.Product projectName
          Attribute.Company (projectOwners |> String.concat ", ")
          Attribute.Version ver
          Attribute.FileVersion ver ]

Target "Build" <| fun _ -> 
    buildSlns
    |> MSBuild null "Build" [ "Configuration", config ]
    |> ignore

Target "Package" <| fun _ -> 
    let lib = packagePath </> "lib" </> "net46"
    let dlls = !! "*.dll" ++ "*.xml" |> SetBaseDir mainOutputPath
    CreateDir lib
    CopyFiles lib dlls

Target "Nuget" <| fun _ ->
    let nugetOutput = outputPath </> "nuget"
    CreateDir nugetOutput
    (coreProjPath </> "Org.Interactivity.Recognizer.nuspec")
    |> NuGet (fun p ->
        { p with
            Project = projectId
            Title = projectName
            Authors = projectOwners
            Description = projectDescription
            OutputPath = nugetOutput
            WorkingDir = packagePath
            ReleaseNotes = releaseInfo.Notes |> String.concat "\n"
            Version = releaseInfo.AssemblyVersion
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
        })

Target "UpdateNuget" <| fun _ ->
    let nugetPath = pathConcat ["dependencies";"nuget";"nuget.exe"]
    Shell.Exec (nugetPath, "update -self") |> ignore

Target "-T" <| fun _ ->
    log "Properties:"
    log "  config=Debug|Release"
    log ""
    PrintTargets()

Target "Full" DoNothing

// Dependencies
"Clean" ==> "Version" ==> "Build" ==> "Package" ==> "Nuget" ==> "Full"
RunTargetOrDefault "Full"
