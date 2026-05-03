using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Humanizer;

namespace BlogEngine;

public class BlogPostService
{
    const string pattern = @"[0-9]{4}_[0-9]{2}_[0-9]{2}_";

    public List<BlogPost> GetBlogPosts(Assembly assembly)
    {
        var components = assembly
            .ExportedTypes
            .Where(t => t.IsSubclassOf(typeof(BlogPostComponent)) || 
                       (t.IsSubclassOf(typeof(ComponentBase)) && 
                        t.Namespace != null && 
                        t.Namespace.Contains("Pages.Posts")));

        var blogPosts = components
            .Select(component => GetBlogPost(component))
            .OfType<BlogPost>()
            .ToList();

        return blogPosts;
    }

    public BlogPost? GetBlogPost(Type component)
    {
        var attributes = component.GetCustomAttributes(inherit: true);

        var routeAttribute = attributes.OfType<RouteAttribute>().FirstOrDefault();

        if (routeAttribute != null)
        {
            var route = routeAttribute.Template;
            if (!string.IsNullOrEmpty(route) && route.StartsWith("/posts/"))
            {
                var name = Regex.Replace(component.Name, pattern, "").Trim('_', ' ');
                var title = name.Humanize();
                var date = ReadDateFromComponentName(component.Name);
                var description = string.Empty;
                var tags = Array.Empty<string>();
                string? image = null;
                string? imageAlt = null;

                var instance = TryCreateComponent(component);
                title = ReadStringProperty(component, instance, "Title") ?? title;
                date = ReadDateProperty(component, instance, "Timestamp") ?? date;
                description = ReadStringProperty(component, instance, "Description") ?? description;
                tags = ReadStringArrayProperty(component, instance, "Tags") ?? tags;
                image = ReadStringProperty(component, instance, "Image");
                imageAlt = ReadStringProperty(component, instance, "ImageAlt");

                return new BlogPost(title, route, date, component, description, tags, image, imageAlt);
            }
        }

        return null;
    }

    private static object? TryCreateComponent(Type component)
    {
        try
        {
            return Activator.CreateInstance(component);
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadStringProperty(Type component, object? instance, string propertyName)
    {
        return instance is null
            ? null
            : component.GetProperty(propertyName)?.GetValue(instance) as string;
    }

    private static DateTime? ReadDateProperty(Type component, object? instance, string propertyName)
    {
        return instance is null
            ? null
            : component.GetProperty(propertyName)?.GetValue(instance) as DateTime?;
    }

    private static string[]? ReadStringArrayProperty(Type component, object? instance, string propertyName)
    {
        return instance is null
            ? null
            : component.GetProperty(propertyName)?.GetValue(instance) as string[];
    }

    private static DateTime ReadDateFromComponentName(string componentName)
    {
        var match = Regex.Match(componentName, pattern);
        return match.Success &&
               DateTime.TryParseExact(match.Value, "yyyy_MM_dd_", null, System.Globalization.DateTimeStyles.None, out var date)
            ? date
            : DateTime.MinValue;
    }
}
