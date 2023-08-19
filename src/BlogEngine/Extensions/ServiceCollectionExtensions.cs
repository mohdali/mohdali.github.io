using Microsoft.Extensions.DependencyInjection;

namespace BlogEngine;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddBlogEngineServices(this IServiceCollection services)
    {
        services.AddScoped<BlogPostService>();

        services.AddScoped<NavigationJsInterop>();
        services.AddScoped<CodeSnippetJsInterop>();

        services.AddScoped<NavigationHelper>();

        return services;
    }
}
