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
            .Where(post => post is not null)
            .ToList();

        return blogPosts;
    }

    public BlogPost GetBlogPost(Type component)
    {
        var attributes = component.GetCustomAttributes(inherit: true);

        var routeAttribute = attributes.OfType<RouteAttribute>().FirstOrDefault();

        if (routeAttribute != null)
        {
            var route = routeAttribute.Template;
            if (!string.IsNullOrEmpty(route) && route.StartsWith("/posts/"))
            {
                var name = Regex.Replace(component.Name, pattern, "");
                
                // Try to get title and date from the instance if it's a generated markdown post
                var date = DateTime.MinValue;
                string title = name.Humanize();
                
                try
                {
                    // Check if this is a markdown-generated post
                    if (component.Namespace?.Contains("Generated") == true)
                    {
                        // Create an instance to get the properties
                        var instance = Activator.CreateInstance(component);
                        var titleProp = component.GetProperty("Title");
                        var timestampProp = component.GetProperty("Timestamp");
                        
                        if (titleProp != null && instance != null)
                        {
                            var titleValue = titleProp.GetValue(instance) as string;
                            if (!string.IsNullOrEmpty(titleValue))
                                title = titleValue;
                        }
                        
                        if (timestampProp != null && instance != null)
                        {
                            var timestampValue = timestampProp.GetValue(instance);
                            if (timestampValue is DateTime dt)
                                date = dt;
                        }
                    }
                    else
                    {
                        // Original logic for non-generated posts
                        var match = Regex.Match(component.Name, pattern);
                        if(match.Success) {
                            DateTime.TryParseExact(match.Value,"yyyy_MM_dd_", null, System.Globalization.DateTimeStyles.None, out date);
                        }
                    }
                }
                catch
                {
                    // Fallback to original logic if instantiation fails
                    var match = Regex.Match(component.Name, pattern);
                    if(match.Success) {
                        DateTime.TryParseExact(match.Value,"yyyy_MM_dd_", null, System.Globalization.DateTimeStyles.None, out date);
                    }
                }
                
                return new BlogPost(title, route, date, component);
            }
        }

        return null;
    }
}
