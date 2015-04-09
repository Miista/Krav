#r "tools/FAKE.Core/tools/FakeLib.dll"
open Fake
open System
open Fake.AssemblyInfoFile
open Fake.XUnit2Helper

type project = { name: string; description: string; version: string; }

// Settings
let projects = [ { name = "Krav"
                   description = "Readable preconditions"
                   version = "0.1.1" }
                 { name = "Krav.Simple"
                   description = "Readable and snappy preconditions"
                   version = "0.1.1" } ]

let buildMode = getBuildParamOrDefault "buildMode" "Release"

// Directories
let buildDirFor project = sprintf "./build/%s/" project.name
let testsDir = "./tests/"
let testResultsDir = "./testresults/"
let distDirFor project = sprintf "./dist/%s/" project.name
let assemblyInfoPathFor project = sprintf "./src/%s/Properties/AssemblyInfo.cs" project.name
let targetFramework = "portable-net4+sl5+wp8+windows8/"

// Targets
Target "Clean" (fun _ ->
  let clean project =
    CleanDirs [ buildDirFor project
                distDirFor project
                distDirFor project @@ "/lib/" @@ targetFramework ]

  CleanDirs [ testResultsDir ]
  projects |> Seq.iter clean
)

Target "Build" (fun _ ->
  let build project =
    let buildFolder = buildDirFor project
    CreateCSharpAssemblyInfo (assemblyInfoPathFor project)
      [ Attribute.Title project.name
        Attribute.Description project.description
        Attribute.Product project.name
        Attribute.Version project.version
        Attribute.FileVersion project.version ]

    !! (sprintf "./src/%s/**/*.csproj" project.name)
    |> MSBuildRelease buildFolder "Build"
    |> Log "AppBuild-Output: "

  projects
  |> Seq.iter build
)


Target "BuildTests" (fun _ ->
  !! "src/Tests/**/*.csproj"
  |> MSBuildRelease testsDir "Build"
  |> Log "TestBuild-Output: "
)


Target "UnitTests" (fun _ ->
  !! (sprintf "%s/*.Tests.dll" testsDir)
  |> xUnit2 (fun p ->
    {p with
      OutputDir = testResultsDir })
)

Target "Package" (fun _ ->
  let package project =
    let buildDir = buildDirFor project
    let distDir = distDirFor project
    !! (buildDir @@ "*.dll") ++ (buildDir @@ "*.xml")
      |> Copy (distDir @@ "/lib/" @@ targetFramework)

    NuGet (fun p ->
      {p with
        WorkingDir = distDir
        OutputPath = distDir
        Project = project.name
        Description = project.description
        Publish = false
        Version = project.version }) "Template.nuspec"

  projects 
  |> Seq.iter package
)

Target "Default" DoNothing

// Dependencies
"Clean"
  ==> "Build"
  ==> "BuildTests"
  ==> "UnitTests"
  ==> "Default"
  ==> "Package"

// Start
RunTargetOrDefault "Default"
