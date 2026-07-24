using System.Net;
using System.Text;

namespace Scrutor.Extensions.HttpClient.Specs;

/// <summary>
/// Fixtures for the registration specs: a common marker interface plus two API
/// clients, each with its own matching interface (so <c>AsMatchingInterface()</c>
/// maps <c>WeatherClient</c> → <c>IWeatherClient</c>, etc.) and an injected
/// <see cref="System.Net.Http.HttpClient"/>.
/// </summary>
public interface ITestApiClient;

public interface IWeatherClient : ITestApiClient
{
    System.Net.Http.HttpClient Http { get; }

    Task<string> PingAsync(CancellationToken ct = default);
}

public sealed class WeatherClient(System.Net.Http.HttpClient http) : IWeatherClient
{
    public System.Net.Http.HttpClient Http => http;

    public async Task<string> PingAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("ping", ct);
        return await response.Content.ReadAsStringAsync(ct);
    }
}

public interface IStockClient : ITestApiClient
{
    System.Net.Http.HttpClient Http { get; }
}

public sealed class StockClient(System.Net.Http.HttpClient http) : IStockClient
{
    public System.Net.Http.HttpClient Http => http;
}

/// <summary>
/// Terminating <see cref="DelegatingHandler"/> that returns a canned body without an
/// inner handler — used to prove the injected client flows through the named client's
/// configured message-handler pipeline (i.e. it really is an IHttpClientFactory client).
/// </summary>
public sealed class StubHandler(string body) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        });
}
