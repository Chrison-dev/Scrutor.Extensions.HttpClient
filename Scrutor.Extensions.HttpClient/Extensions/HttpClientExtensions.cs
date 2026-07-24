using System;
using System.Linq;
using System.Reflection;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using Scrutor;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Scrutor selector extensions that register each scanned class as a <em>typed</em>
/// <see cref="System.Net.Http.HttpClient"/> (through <see cref="IHttpClientFactory"/>)
/// instead of a plain transient/scoped/singleton service.
/// </summary>
/// <remarks>Bridges Scrutor assembly scanning with <c>Microsoft.Extensions.Http</c>.
/// See https://github.com/khellang/Scrutor/issues/180.</remarks>
public static class HttpClientExtensions
{
    // Honest trim/AOT signal: registration runs Scrutor's runtime assembly scan and closes the
    // generic AddHttpClient<TClient,TImplementation> over the scanned types via reflection. Consumers
    // building trimmed/AOT get a warning at these entry points instead of silent breakage.
    private const string TrimMessage =
        "Registers types discovered by Scrutor's runtime assembly scan as typed HttpClients; the scanned client types may be removed by the trimmer.";
    private const string AotMessage =
        "Closes the generic AddHttpClient<TClient,TImplementation> over the scanned types via MakeGenericMethod, which requires runtime code generation.";

    /// <summary>
    /// Registers each scanned service as a typed <see cref="System.Net.Http.HttpClient"/> bound to
    /// the named client <paramref name="name"/> (so they share that client's configuration).
    /// </summary>
    /// <param name="selector">The Scrutor service-type selector.</param>
    /// <param name="name">The name of a client registered via <c>AddHttpClient(name, ...)</c>.</param>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode(TrimMessage)]
    [RequiresDynamicCode(AotMessage)]
#endif
    public static IServiceTypeSelector AsHttpClient(this IServiceTypeSelector selector, string name = "")
        => selector.UsingRegistrationStrategy(new HttpClientRegistrationStrategy(name));

    /// <summary>
    /// Registers each scanned service as a typed <see cref="System.Net.Http.HttpClient"/>, each with
    /// its own default client named after the service type.
    /// </summary>
    /// <param name="selector">The Scrutor service-type selector.</param>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode(TrimMessage)]
    [RequiresDynamicCode(AotMessage)]
#endif
    public static IServiceTypeSelector AsHttpClient(this IServiceTypeSelector selector)
        => selector.UsingRegistrationStrategy(new HttpClientRegistrationStrategy(name: null));

    /// <summary>
    /// A Scrutor <see cref="RegistrationStrategy"/> that registers each scanned
    /// (service, implementation) pair as a typed HttpClient. <see cref="System.Net.Http.HttpClient"/>'s
    /// <c>AddHttpClient&lt;TClient,TImplementation&gt;</c> has no non-generic overload, so the closed
    /// generic method is resolved once and invoked per scanned type.
    /// </summary>
    private sealed class HttpClientRegistrationStrategy(string? name) : RegistrationStrategy
    {
        // Resolved lazily and cached: the AddHttpClient<,> generic definition matching whether a
        // client name was supplied. Named → (IServiceCollection, string); unnamed → (IServiceCollection).
        private static readonly MethodInfo NamedAddHttpClient = ResolveAddHttpClient(named: true);
        private static readonly MethodInfo UnnamedAddHttpClient = ResolveAddHttpClient(named: false);

        // Apply overrides Scrutor's (un-annotated) RegistrationStrategy.Apply, so the RequiresDynamicCode
        // warning can't propagate here. It's already surfaced to consumers on the public AsHttpClient
        // entry points (the only way to reach this strategy), so suppress the internal MakeGenericMethod site.
#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("AOT", "IL3050",
            Justification = "AsHttpClient() entry points are marked [RequiresDynamicCode]; callers are warned. AddHttpClient<,> has no non-generic overload.")]
#endif
        public override void Apply(IServiceCollection services, ServiceDescriptor descriptor)
        {
            var serviceType = descriptor.ServiceType;
            var implementationType = descriptor.ImplementationType
                ?? throw new InvalidOperationException(
                    $"AsHttpClient() requires a concrete implementation type, but the descriptor for " +
                    $"'{serviceType}' has none. Pair it with a class-based Scrutor selector such as " +
                    $"AddClasses(...).AsMatchingInterface() / .AsSelf().");

            var closed = (name is null ? UnnamedAddHttpClient : NamedAddHttpClient)
                .MakeGenericMethod(serviceType, implementationType);

            var arguments = name is null ? new object[] { services } : new object[] { services, name };
            closed.Invoke(obj: null, arguments);
        }

        private static MethodInfo ResolveAddHttpClient(bool named) =>
            typeof(HttpClientFactoryServiceCollectionExtensions).GetMethods()
                .SingleOrDefault(method =>
                    method is { Name: nameof(HttpClientFactoryServiceCollectionExtensions.AddHttpClient), IsGenericMethodDefinition: true }
                    && method.GetGenericArguments().Length == 2
                    && HasShape(method.GetParameters(), named))
            ?? throw new InvalidOperationException(
                $"Could not locate the AddHttpClient<TClient,TImplementation>({(named ? "IServiceCollection, string" : "IServiceCollection")}) " +
                $"overload on {nameof(HttpClientFactoryServiceCollectionExtensions)}. The Microsoft.Extensions.Http API may have changed.");

        private static bool HasShape(ParameterInfo[] parameters, bool named) => named
            ? parameters.Length == 2 && parameters[0].ParameterType == typeof(IServiceCollection) && parameters[1].ParameterType == typeof(string)
            : parameters.Length == 1 && parameters[0].ParameterType == typeof(IServiceCollection);
    }
}
