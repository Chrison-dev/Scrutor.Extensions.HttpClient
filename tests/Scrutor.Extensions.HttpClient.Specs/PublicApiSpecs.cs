using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PublicApiGenerator;
using VerifyXunit;
using Xunit;

namespace Scrutor.Extensions.HttpClient.Specs;

/// <summary>
/// Roslyn-based public-API baseline (PublicApiGenerator + Verify), mirroring the
/// approach used in Fallout/TVDB. Any unintended change to the shipped public
/// surface fails this spec; intended changes are accepted by updating the
/// <c>*.verified.txt</c> snapshot.
/// </summary>
public class PublicApiSpecs
{
    [Fact]
    public Task Public_api_surface_has_not_changed()
    {
        // The public surface lives in the Microsoft.Extensions.DependencyInjection
        // namespace (by design — it extends Scrutor's selector). PublicApiGenerator
        // denies "Microsoft"/"System" prefixes by default, so allow-list ours back in.
        var publicApi = typeof(HttpClientExtensions).Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            AllowNamespacePrefixes = new[] { "Microsoft.Extensions.DependencyInjection" },
        });
        return Verifier.Verify(publicApi);
    }
}
