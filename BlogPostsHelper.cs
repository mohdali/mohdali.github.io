using Microsoft.AspNetCore.Components;
using System.Reflection;
using System.Text.RegularExpressions;

using Humanizer;

namespace mohdali.github.io;

public record BlogPost(string Title, string Url, DateTime Timestamp);

public static class BlogPostsHelper
{
    const string pattern = @"[0-9]{4}_[0-9]{2}_[0-9]{2}_";

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

    public static BlogPost GetBlogPost(Type component)
    {
        var attributes = component.GetCustomAttributes(inherit: true);

        var routeAttribute = attributes.OfType<RouteAttribute>().FirstOrDefault();

        if (routeAttribute != null)
        {
            var route = routeAttribute.Template;
            if (!string.IsNullOrEmpty(route) && route.StartsWith("/posts/"))
            {
                var name = Regex.Replace(component.Name, pattern, "");

                var match = Regex.Match(component.Name, pattern);
                var date = DateTime.MinValue;
                if(match.Success) {
                    DateTime.TryParseExact(match.Value,"yyyy_MM_dd_", null, System.Globalization.DateTimeStyles.None, out date);
                }
                return new BlogPost(name.Humanize(), route, date);
            }
        }

        return null;
    }
}