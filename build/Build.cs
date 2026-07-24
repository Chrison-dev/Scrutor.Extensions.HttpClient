using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

/// <summary>
/// Fallout build for Scrutor.Extensions.HttpClient.
///
/// CI (build.yml, auto-generated from the [GitHubActions] attribute) runs Test + Pack
/// on pushes/PRs to main. Publishing is a separate, hand-written workflow (publish.yml,
/// a documented exception) driven by version tags — see CLAUDE.md.
/// </summary>
// Workflows are GENERATED from this attribute (see CLAUDE.md: never hand-edit
// .github/workflows/*.yml). Regenerate with `./build.cmd` or:
//   dotnet fallout --generate-configuration GitHubActions_build --host GitHubActions
[GitHubActions(
    "build",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    OnPushBranches = new[] { "main" },
    OnPullRequestBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(Test), nameof(Pack) })]
partial class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Pack);

    const string PackageProject = "Scrutor.Extensions.HttpClient";

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";
    AbsolutePath SpecsProject => RootDirectory / "tests" / "Scrutor.Extensions.HttpClient.Specs" / "Scrutor.Extensions.HttpClient.Specs.csproj";
    AbsolutePath PackableProject => RootDirectory / PackageProject / $"{PackageProject}.csproj";

    Target Test => _ => _
        .Description("Run the *.Specs test suite")
        .Executes(() => DotNetTest(_ => _
            .SetProjectFile(SpecsProject)
            .SetConfiguration("Release")));

    Target Pack => _ => _
        .Description("Pack the NuGet package into artifacts/packages")
        .DependsOn(Test)
        .Executes(() =>
        {
            PackagesDirectory.CreateOrCleanDirectory();
            DotNetPack(_ => _
                .SetProject(PackableProject)
                .SetConfiguration("Release")
                .SetOutputDirectory(PackagesDirectory));
        });
}
