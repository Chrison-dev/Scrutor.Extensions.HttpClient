# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

`Scrutor.Extensions.HttpClient` is a tiny (single public class) companion library for
[Scrutor](https://github.com/khellang/Scrutor). It adds `.AsHttpClient()` /
`.AsHttpClient(name)` registration selectors to Scrutor's assembly-scan pipeline so that
each scanned class is registered as a **typed `HttpClient`** (via `IHttpClientFactory` /
`AddHttpClient<TInterface,TImpl>()`) instead of a plain service.

It exists to solve [Scrutor issue #180](https://github.com/khellang/Scrutor/issues/180),
which the Scrutor maintainer declined to ship natively (it would force a
`Microsoft.Extensions.Http` dependency and needs `MakeGenericMethod` reflection). As of
Scrutor 7.0.0 / .NET 10 nothing upstream fills this gap, so the library still earns its keep.

```csharp
services.AddHttpClient("MyClient", c => c.BaseAddress = new Uri("https://api.example.com"));
services.Scan(scan => scan
    .FromAssemblyOf<IMyApiClient>()
    .AddClasses(c => c.AssignableTo<IMyApiClient>())
    .AsMatchingInterface()
    .AsHttpClient("MyClient"));   // ← all scanned clients become typed clients of "MyClient"
```

## Build & CI — Fallout owns the workflows (never hand-edit YAML)

CI is defined in C# via [Fallout](https://www.nuget.org/packages/Fallout.GlobalTool/) (a NUKE
fork), not hand-written pipelines. The build lives in `build/Build.cs`.

- `.github/workflows/build.yml` is **GENERATED** from the `[GitHubActions]` attribute on the
  `Build` class. **Never hand-edit it.** Regenerate by running any build (`./build.cmd`) or:
  `dotnet fallout --generate-configuration GitHubActions_build --host GitHubActions`.
- Run the build locally with the polyglot bootstrapper (bat/sh in one file — works on the
  Linux CI runner and locally):
  ```bash
  ./build.cmd Test     # run the *.Specs suite
  ./build.cmd Pack     # Test + pack the NuGet package into artifacts/packages
  ./build.cmd          # default target (Pack)
  ```
- Fallout tooling notes (learned the hard way on the TVDB sibling repo): the tool package
  `Fallout.GlobalTool` is singular and **lags the libraries at 10.3.49** — keep `Fallout.Common`
  / `Fallout.Components` pinned to `10.3.49` to match. `.fallout/parameters.json` marks the repo
  root; `.fallout/build.schema.json` is generated.

### Publishing — hand-written `publish.yml` (documented exception)

`.github/workflows/publish.yml` is the **one hand-maintained workflow**. The publish flow
(OIDC trusted publishing + GitHub Packages + GitHub Release) is not expressible via the pinned
Fallout 10.3.49 attribute API, so it is written by hand — this is deliberate, not an oversight.

Publishing is **tag-driven**. Pushing a `v*` tag:
1. Packs via the Fallout `Pack` target.
2. Pushes to **nuget.org via Trusted Publishing (OIDC)** — no stored API key.
3. Pushes to **GitHub Packages** (`nuget.pkg.github.com/Chrison-dev`) with `GITHUB_TOKEN`.
4. Creates a **GitHub Release** with the `.nupkg` attached and label-categorized notes
   (`.github/release.yml`).

One-time nuget.org setup: a Trusted Publisher policy for `Scrutor.Extensions.HttpClient`
(owner `Chrison-dev`, repo `Scrutor.Extensions.HttpClient`, workflow `publish.yml`, environment
`nuget.org`) + repo variable `NUGET_USER`.

## Versioning — GitVersion, tag-driven

`GitVersion.yml` drives the version from `v*` tags. **Convention: MAJOR tracks the compatible
Scrutor major** — currently **7.x ↔ Scrutor 7.x** (the old 5.x line was Scrutor 5.x; 6.x was
skipped/never adopted). MINOR.PATCH is our own internal release counter. Cut a release by
tagging, e.g. `git tag v7.0.0 && git push origin v7.0.0`. Untagged builds fall back to the
static version in `Directory.Build.props`.

> **Note:** GitVersion only honours a version tag on `main` (feature branches get branch-name
> prerelease labels). Cut releases/prereleases by tagging `main` after the PR merges.

## Tests — spec-style, no Central Package Management

Tests live in `tests/*.Specs` and follow the Fallout/TVDB convention:

- The test stack (xUnit + FluentAssertions + Verify + PublicApiGenerator) is injected via
  `tests/Directory.Build.props` with **per-package versions — do NOT introduce Central Package
  Management** (deliberate preference).
- `PublicApiSpecs` snapshots the public surface with PublicApiGenerator + Verify. The surface is
  in the `Microsoft.Extensions.DependencyInjection` namespace, so it must be allow-listed past
  PublicApiGenerator's default `System`/`Microsoft` deny-list (`AllowNamespacePrefixes`). Accept
  intentional API changes by updating the `*.verified.txt` baseline.
- Functional specs exercise the real DI container + a stub `HttpMessageHandler` (no Mockly /
  NetArchTest here — nothing to mock or architect in a single-class library).

## Library conventions

- Targets **net8.0** (Scrutor 7.x supports net8.0+). Multi-targeting is a separate modernization
  concern, tracked apart from routine changes.
- Scrutor pinned `[7.0.0, 8.0.0)` (aligned with the package major) and `Microsoft.Extensions.Http`
  `[9.0.0, 11.0.0)`. The 5→7 bump was clean — the `.AsHttpClient()` bridge sits on Scrutor's
  stable `RegistrationStrategy` seam, so no code/test changes were needed. Root
  `Directory.Build.props` centralizes TFM + GitVersion; the library csproj only carries metadata.
- Still-open modernization (deliberately deferred, discuss before doing): multi-target TFMs, and a
  source-generator rewrite to replace the `MakeGenericMethod` reflection (the one axis that isn't
  trim/Native-AOT friendly).
