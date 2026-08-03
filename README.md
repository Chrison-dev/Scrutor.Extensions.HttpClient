# Scrutor.Extensions.HttpClient

[![NuGet](https://img.shields.io/nuget/v/Scrutor.Extensions.HttpClient.svg)](https://www.nuget.org/packages/Scrutor.Extensions.HttpClient/)
[![Downloads](https://img.shields.io/nuget/dt/Scrutor.Extensions.HttpClient.svg)](https://www.nuget.org/packages/Scrutor.Extensions.HttpClient/)
[![Build & Test](https://github.com/Chrison-dev/Scrutor.Extensions.HttpClient/actions/workflows/build.yml/badge.svg)](https://github.com/Chrison-dev/Scrutor.Extensions.HttpClient/actions/workflows/build.yml)
[![built with Fallout](https://img.shields.io/badge/built%20with-Fallout-F5C800?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI%2BPGNpcmNsZSBjeD0iMTIiIGN5PSIxMiIgcj0iMTIiIGZpbGw9IiNGNUM4MDAiLz48ZyBmaWxsPSIjMTExIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIzLjEiLz48cGF0aCBkPSJNOS45OSA5LjAyQTMuNiAzLjYgMCAwIDEgMTQuMDEgOS4wMkwxOC40OSAyLjM4QTExLjYgMTEuNiAwIDAgMCA1LjUxIDIuMzhaTTE1LjU5IDExLjc1QTMuNiAzLjYgMCAwIDEgMTMuNTggMTUuMjRMMTcuMDkgMjIuNDNBMTEuNiAxMS42IDAgMCAwIDIzLjU3IDExLjE5Wk0xMC40MiAxNS4yNEEzLjYgMy42IDAgMCAxIDguNDEgMTEuNzVMMC40MyAxMS4xOUExMS42IDExLjYgMCAwIDAgNi45MSAyMi40M1oiLz48L2c%2BPC9zdmc%2B)](https://github.com/Fallout-build/Fallout)
[![License](https://img.shields.io/github/license/Chrison-dev/Scrutor.Extensions.HttpClient.svg)](LICENSE)

A tiny companion for [Scrutor](https://github.com/khellang/Scrutor) that lets its
assembly-scanning register each matched class as a **typed
[`HttpClient`](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory#typed-clients)**
(via `IHttpClientFactory`) instead of a plain service — the one thing Scrutor
[deliberately doesn't do out of the box](https://github.com/khellang/Scrutor/issues/180).

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<IWeatherClient>()
    .AddClasses(c => c.AssignableTo<IApiClient>())
    .AsMatchingInterface()
    .AsHttpClient("weather"));   // ← every scanned client becomes a typed HttpClient
```

## Install

```sh
dotnet add package Scrutor.Extensions.HttpClient
```

## Usage

1. Register a **named** `HttpClient` with your shared configuration.
2. Make your API clients implement a common marker interface (and, for
   `.AsMatchingInterface()`, an `I{ClassName}` interface each).
3. Scan for them and finish the chain with `.AsHttpClient("<name>")`.

```csharp
// 1. A named client carrying the shared config (base address, handlers, Polly, …).
services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});

// 2 + 3. Register every IApiClient implementation as a typed client of "weather".
services.Scan(scan => scan
    .FromAssemblyOf<IWeatherClient>()
    .AddClasses(classes => classes.AssignableTo<IApiClient>())
    .AsMatchingInterface()
    .AsHttpClient("weather"));
```

Now each scanned client is resolved from `IHttpClientFactory` with the `"weather"`
client's configuration injected:

```csharp
public sealed class WeatherClient(HttpClient http) : IWeatherClient
{
    public Task<Forecast?> GetAsync(string city) =>
        http.GetFromJsonAsync<Forecast>($"forecast/{city}");
}
```

### Overloads

| Call | Behaviour |
|---|---|
| `.AsHttpClient("name")` | Registers each scanned class as a typed client bound to the **named** client `"name"` — they share its configuration. |
| `.AsHttpClient()` | Registers each scanned class as a typed client with **its own** default client (named after the service type). |

Both are `IServiceTypeSelector` extensions, so they slot onto the end of any Scrutor
`.Scan(...)` chain (`.AsMatchingInterface()`, `.AsSelf()`, `.As<T>()`, …).

## Compatibility

- **Scrutor 7.x** — the package major tracks Scrutor's major (`7.x` ↔ Scrutor 7.x).
- Targets **`net8.0`** and **`netstandard2.0`** (so it's usable from .NET Framework 4.6.2+,
  .NET Core 2.0+, and modern .NET alike).

### Trimming / AOT

This bridges Scrutor's **runtime** assembly scan and closes the generic
`AddHttpClient<TClient,TImplementation>` over the scanned types via reflection, so it
is **not** trim- or Native-AOT-safe. The public methods are annotated with
`[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`, so a trimmed/AOT build gets an
honest warning at the call site rather than silent breakage.

## Building

CI is defined in C# with [Fallout](https://github.com/Fallout-build/Fallout) (a NUKE
fork); the GitHub Actions workflow is generated from the build, never hand-edited.

```sh
./build.cmd Test    # run the spec suite
./build.cmd Pack    # test + pack the NuGet package
```

## License

[MIT](LICENSE)
