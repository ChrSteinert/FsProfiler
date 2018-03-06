// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r "./packages/build/FAKE/tools/FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile

// --------------------------------------------------------------------------------------
// Build variables
// --------------------------------------------------------------------------------------

let buildDir  = "./build/"
let appReferences = !! "/**/*.fsproj"

System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

// --------------------------------------------------------------------------------------
// Targets
// --------------------------------------------------------------------------------------

Target "Clean" (fun _ ->
    CleanDirs [buildDir]
)

Target "AssemblyInfo" (fun _ ->
    let release = ReleaseNotesHelper.LoadReleaseNotes "CHANGELOG"

    let getAssemblyInfoAttributes projectName =
        [ Attribute.Title (projectName)
          Attribute.Product "Fracer"
          Attribute.Description "" ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName))

    !! "**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> 
            let asmInfoFile = folderName </> "AssemblyInfo.fs"
            let version = 
                let notesVersion = release.AssemblyVersion |> SemVerHelper.parse
                match GetAttribute "AssemblyVersion" asmInfoFile with
                | Some old -> 
                    let old = old.Value |> SemVerHelper.parse
                    if old.Major = notesVersion.Major && old.Minor = notesVersion.Minor then
                        { old with Patch = old.Patch + 1 }
                    else
                        notesVersion
                | None -> notesVersion
                |> (fun c -> Attribute.Version (c.ToString ()))

            let fileVersion = 
                let notesVersion = release.AssemblyVersion |> SemVerHelper.parse
                match GetAttribute "AssemblyFileVersion" asmInfoFile with
                | Some old -> 
                    let old = old.Value |> SemVerHelper.parse
                    if old.Major = notesVersion.Major && old.Minor = notesVersion.Minor then
                        { old with Patch = old.Patch + 1 }
                    else
                        notesVersion
                | None -> notesVersion
                |> (fun c -> Attribute.FileVersion (c.ToString ()))

            CreateFSharpAssemblyInfo asmInfoFile (version :: fileVersion :: attributes)
        | _ -> ()
        )
)

Target "Build" (fun _ ->
    appReferences
    |> Seq.iter (fun p ->
        DotNetCli.Build (fun c -> { c with Project = p })
    )
)

Target "Pack" (fun _ -> Paket.Pack id)

Target "Push" (fun _ -> Paket.Push (fun c -> { c with PublishUrl = "http://proget.de.kworld.kpmg.com/nuget/KPMG.DE.KAIL.NUGET/"; ApiKey = "[APIKEY]" }))

// --------------------------------------------------------------------------------------
// Build order
// --------------------------------------------------------------------------------------

"Clean"
    ==> "AssemblyInfo"
    ==> "Build"
    ==> "Pack"
    ==> "Push"

RunTargetOrDefault "Build"
