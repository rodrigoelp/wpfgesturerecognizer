#I @"dependencies\FAKE.Core\tools\"
#r "FakeLib.dll"
 
open Fake
open Fake.AssemblyInfoFile
open System.IO
open System
open System.Text.RegularExpressions

let pathConcat a = String.concat (string Path.DirectorySeparatorChar) a

let config = getBuildParamOrDefault "config" "Release"
let outputPath = pathConcat ["output"; config]
let buildSlns = [ pathConcat ["src"; "GestureRecognizer.sln"] ]

let releaseInfo =
    ReadFile "release_notes.md"
    |> ReleaseNotesHelper.parseReleaseNotes

Target "Clean" <| fun _ ->
    CleanDirs [ outputPath ]
    
Target "Version" <| fun _ ->
    let ver = releaseInfo.SemVer |> string
    let gitHash = Git.Information.getCurrentHash()
    printfn "##teamcity[buildNumber '%s']" ver
    let appId = "c93fad81-4f16-40b0-b65a-383e1d1ef26e"
    let getAttributes name id =
        let productString = sprintf "%s %s (%s)" name ver gitHash
        [ Attribute.Title productString
          Attribute.Description productString
          Attribute.Guid id
          Attribute.Product name
          Attribute.Version ver
          Attribute.FileVersion ver ]
    CreateCSharpAssemblyInfo "src/Org.Interactivity.Recognizer/Properties/AssemblyInfo.cs" (getAttributes "Gesture Recognizer" appId)

Target "Build" <| fun _ -> 
    buildSlns
    |> MSBuild null "Build" [ "Configuration", config ]
    |> ignore

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
"Clean" ==> "Version" ==> "Build" ==> "Full"
RunTargetOrDefault "Full"
