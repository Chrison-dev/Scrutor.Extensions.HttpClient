using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Scrutor.Extensions.HttpClient.Specs;

/// <summary>
/// End-to-end specs for the <c>.AsHttpClient()</c> / <c>.AsHttpClient(name)</c> Scrutor
/// registration selectors: they must register every scanned class as a <em>typed</em>
/// HttpClient (via <see cref="IHttpClientFactory"/>), not a plain service — bridging
/// Scrutor's assembly scanning with <c>Microsoft.Extensions.Http</c>.
/// </summary>
public class AsHttpClientSpecs
{
    private const string ClientName = "test-client";

    private static IServiceCollection ScanClientsAsNamed(IServiceCollection services) =>
        services.Scan(scan => scan
            .FromAssemblyOf<WeatherClient>()
            .AddClasses(classes => classes.AssignableTo<ITestApiClient>())
            .AsMatchingInterface()
            .AsHttpClient(ClientName));

    [Fact]
    public void AsHttpClient_named_registers_every_scanned_client_against_its_matching_interface()
    {
        var services = new ServiceCollection();
        services.AddHttpClient(ClientName, c => c.BaseAddress = new Uri("https://api.example.com/"));

        ScanClientsAsNamed(services);
        var provider = services.BuildServiceProvider();

        provider.GetService<IWeatherClient>().Should().BeOfType<WeatherClient>();
        provider.GetService<IStockClient>().Should().BeOfType<StockClient>();
    }

    [Fact]
    public void AsHttpClient_named_wires_up_the_http_client_factory()
    {
        var services = new ServiceCollection();
        services.AddHttpClient(ClientName, c => c.BaseAddress = new Uri("https://api.example.com/"));

        ScanClientsAsNamed(services);
        var provider = services.BuildServiceProvider();

        provider.GetService<IHttpClientFactory>().Should().NotBeNull(
            "registering a typed client must pull in IHttpClientFactory");
    }

    [Fact]
    public void Named_client_configuration_flows_into_the_injected_http_client()
    {
        var baseAddress = new Uri("https://api.example.com/");
        var services = new ServiceCollection();
        services.AddHttpClient(ClientName, c => c.BaseAddress = baseAddress);

        ScanClientsAsNamed(services);
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IWeatherClient>().Http.BaseAddress.Should().Be(baseAddress);
        provider.GetRequiredService<IStockClient>().Http.BaseAddress.Should().Be(baseAddress);
    }

    [Fact]
    public void Typed_clients_are_registered_with_transient_lifetime()
    {
        var services = new ServiceCollection();
        services.AddHttpClient(ClientName, c => c.BaseAddress = new Uri("https://api.example.com/"));

        ScanClientsAsNamed(services);
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IWeatherClient>()
            .Should().NotBeSameAs(provider.GetRequiredService<IWeatherClient>(),
                "typed HttpClients are registered transient");
    }

    [Fact]
    public async Task Injected_client_flows_through_the_named_clients_handler_pipeline()
    {
        var services = new ServiceCollection();
        services
            .AddHttpClient(ClientName, c => c.BaseAddress = new Uri("http://stub/"))
            .AddHttpMessageHandler(() => new StubHandler("pong"));

        ScanClientsAsNamed(services);
        var provider = services.BuildServiceProvider();

        var weather = provider.GetRequiredService<IWeatherClient>();

        (await weather.PingAsync()).Should().Be("pong",
            "the scanned client must be a genuine factory client whose configured handlers run");
    }

    [Fact]
    public void AsHttpClient_without_a_name_registers_scanned_clients_as_typed_clients()
    {
        var services = new ServiceCollection();
        services.Scan(scan => scan
            .FromAssemblyOf<WeatherClient>()
            .AddClasses(classes => classes.AssignableTo<ITestApiClient>())
            .AsMatchingInterface()
            .AsHttpClient());

        var provider = services.BuildServiceProvider();

        provider.GetService<IWeatherClient>().Should().BeOfType<WeatherClient>();
        provider.GetService<IStockClient>().Should().BeOfType<StockClient>();
        provider.GetService<IHttpClientFactory>().Should().NotBeNull();
        provider.GetRequiredService<IWeatherClient>().Http.Should().NotBeNull();
    }
}
