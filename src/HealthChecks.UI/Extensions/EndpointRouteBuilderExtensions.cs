using HealthChecks.UI;
using HealthChecks.UI.Core;
using HealthChecks.UI.Middleware;
using Microsoft.AspNetCore.Routing;
using Options = HealthChecks.UI.Configuration.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapHealthChecksUI(this IEndpointRouteBuilder builder,
            Action<Options>? setupOptions = null)
        {
            var options = new Options();
            setupOptions?.Invoke(options);

            EnsureValidApiOptions(options);

            var apiDelegate =
                builder.CreateApplicationBuilder()
                    .UseMiddleware<UIApiRequestLimitingMidleware>()
                    .UseMiddleware<UIApiEndpointMiddleware>()
                    .Build();

            var settingsDelegate = builder.CreateApplicationBuilder()
                .UseMiddleware<UISettingsMiddleware>()
                .Build();

            var webhooksDelegate =
                builder.CreateApplicationBuilder()
                    .UseMiddleware<UIWebHooksApiMiddleware>()
                    .Build();


            var embeddedResourcesAssembly = typeof(UIResource).Assembly;

            var resourcesEndpoints =
                new UIEndpointsResourceMapper(new UIEmbeddedResourcesReader(embeddedResourcesAssembly))
                    .Map(builder, options);

            var apiEndpoint = builder.Map(options.ApiPath, apiDelegate)
                .WithDisplayName("HealthChecks UI Api");

            var settingsEndpoint =
                builder.Map($"{options.ApiPath}/{Keys.HEALTHCHECKSUI_SETTINGS_PATH}", settingsDelegate);

            var webhooksEndpoint = builder.Map(options.WebhookPath, webhooksDelegate)
                .WithDisplayName("HealthChecks UI Webhooks");

            var endpointConventionBuilders =
                new List<IEndpointConventionBuilder>(
                    new[] { apiEndpoint, webhooksEndpoint, settingsEndpoint }.Union(resourcesEndpoints));

            return new HealthCheckUIConventionBuilder(endpointConventionBuilders);
        }

        private static void EnsureValidApiOptions(Options options)
        {
            Action<string, string> ensureValidPath = (string path, string argument) =>
            {
                if (string.IsNullOrEmpty(path) || !path.StartsWith("/"))
                {
                    throw new ArgumentException(
                        "The value for customized path can't be null and need to start with / character.", argument);
                }
            };

            ensureValidPath(options.ApiPath, nameof(Options.ApiPath));
            ensureValidPath(options.UIPath, nameof(Options.UIPath));
            ensureValidPath(options.WebhookPath, nameof(Options.WebhookPath));
        }
    }
}
