using System.Reflection;
using Microsoft.AspNetCore.Components;
using Humanizer;

namespace mohdali.github.io;

public record BlogPost(string Title, string Url, DateTime Timestamp);

public static class BlogPostsHelper
{
    public static List<BlogPost> GetBlogPosts(Assembly assembly)
    {
        var components = assembly
            .ExportedTypes
            .Where(t => t.IsSubclassOf(typeof(ComponentBase)));

        var blogPosts = components
            .Select(component => GetBlogPost(component))
            .Where(post => post is not null)
            .ToList();

        return blogPosts;
    }

    private static BlogPost GetBlogPost(Type component)
    {
        var attributes = component.GetCustomAttributes(inherit: true);

        var routeAttribute = attributes.OfType<RouteAttribute>().FirstOrDefault();

        if (routeAttribute != null)
        {
            var route = routeAttribute.Template;
            if (!string.IsNullOrEmpty(route) && route.StartsWith("/posts/"))
            {
                return new BlogPost(component.Name.Humanize(), route, DateTime.Now);
            }
        }

        return null;
    }
}