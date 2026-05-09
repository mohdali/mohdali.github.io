using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Humanizer;

namespace BlogEngine;

public class BlogPostService
{
    private const string DatePattern = @"[0-9]{4}_[0-9]{2}_[0-9]{2}_";

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

    public List<BlogPost> GetPublishedBlogPosts(Assembly assembly)
    {
        return GetBlogPosts(assembly)
            .Where(IsPublished)
            .OrderByDescending(post => post.Timestamp)
            .ThenBy(post => post.Title)
            .ToList();
    }

    public List<BlogPost> GetPublishedBlogPostsByTag(Assembly assembly, string tagSlug)
    {
        var normalizedSlug = NormalizeTagSlug(tagSlug);

        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return new List<BlogPost>();
        }

        return GetPublishedBlogPosts(assembly)
            .Where(post => post.Tags.Any(tag => string.Equals(GetTagSlug(tag), normalizedSlug, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public List<BlogTag> GetTags(Assembly assembly)
    {
        var tags = GetPublishedBlogPosts(assembly)
            .SelectMany(post => post.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => new { Post = post, Name = tag.Trim(), Slug = GetTagSlug(tag) }))
            .Where(tag => !string.IsNullOrWhiteSpace(tag.Slug))
            .GroupBy(tag => tag.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var tagName = group
                    .Select(tag => tag.Name)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .First();

                var postCount = group
                    .Select(tag => tag.Post.Url)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                return new BlogTag(tagName, group.Key, postCount);
            })
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return tags;
    }

    public BlogTag? GetTag(Assembly assembly, string tagSlug)
    {
        var normalizedSlug = NormalizeTagSlug(tagSlug);

        return GetTags(assembly)
            .FirstOrDefault(tag => string.Equals(tag.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));
    }

    public string GetTagUrl(string tag)
    {
        return $"/tags/{GetTagSlug(tag)}";
    }

    public string GetTagSlug(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Empty;
        }

        var slug = Regex.Replace(tag.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? string.Empty : slug;
    }

    public (BlogPost? Previous, BlogPost? Next) GetAdjacentPosts(Assembly assembly, BlogPost currentPost)
    {
        var posts = GetPublishedBlogPosts(assembly);
        var currentIndex = posts.FindIndex(post => IsSamePost(post, currentPost));

        if (currentIndex < 0)
        {
            return (null, null);
        }

        var previous = currentIndex < posts.Count - 1 ? posts[currentIndex + 1] : null;
        var next = currentIndex > 0 ? posts[currentIndex - 1] : null;

        return (previous, next);
    }

    public List<BlogPost> GetRelatedPosts(Assembly assembly, BlogPost currentPost, int count = 3)
    {
        if (count <= 0)
        {
            return new List<BlogPost>();
        }

        var currentTags = currentPost.Tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = GetPublishedBlogPosts(assembly)
            .Where(post => !IsSamePost(post, currentPost))
            .ToList();

        var relatedPosts = candidates
            .Select(post => new
            {
                Post = post,
                SharedTagCount = post.Tags.Count(currentTags.Contains)
            })
            .Where(result => result.SharedTagCount > 0)
            .OrderByDescending(result => result.SharedTagCount)
            .ThenByDescending(result => result.Post.Timestamp)
            .ThenBy(result => result.Post.Title)
            .Select(result => result.Post)
            .Take(count)
            .ToList();

        if (relatedPosts.Count >= count)
        {
            return relatedPosts;
        }

        var missingPostCount = count - relatedPosts.Count;
        var fallbackPosts = candidates
            .Where(post => relatedPosts.All(relatedPost => !IsSamePost(relatedPost, post)))
            .OrderByDescending(post => post.Timestamp)
            .ThenBy(post => post.Title)
            .Take(missingPostCount);

        relatedPosts.AddRange(fallbackPosts);

        return relatedPosts;
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
                var name = Regex.Replace(component.Name, DatePattern, "").Trim('_', ' ');
                var title = name.Humanize();
                var date = ReadDateFromComponentName(component.Name);
                var description = string.Empty;
                var tags = Array.Empty<string>();
                string? image = null;
                string? imageAlt = null;
                string? imageType = null;
                int? imageWidth = null;
                int? imageHeight = null;
                string? cardImage = null;
                string? cardImageAlt = null;

                var instance = TryCreateComponent(component);
                title = ReadStringProperty(component, instance, "Title") ?? title;
                date = ReadDateProperty(component, instance, "Timestamp") ?? date;
                description = ReadStringProperty(component, instance, "Description") ?? description;
                tags = ReadStringArrayProperty(component, instance, "Tags") ?? tags;
                image = ReadStringProperty(component, instance, "Image");
                imageAlt = ReadStringProperty(component, instance, "ImageAlt");
                imageType = ReadStringProperty(component, instance, "ImageType");
                imageWidth = ReadIntProperty(component, instance, "ImageWidth");
                imageHeight = ReadIntProperty(component, instance, "ImageHeight");
                cardImage = ReadStringProperty(component, instance, "CardImage");
                cardImageAlt = ReadStringProperty(component, instance, "CardImageAlt");
                ResolvePostImage(route, title, ref image, ref imageAlt, ref imageType, ref imageWidth, ref imageHeight);

                return new BlogPost(title, route, date, component, description, tags, image, imageAlt, imageType, imageWidth, imageHeight, cardImage, cardImageAlt);
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

    private static int? ReadIntProperty(Type component, object? instance, string propertyName)
    {
        if (instance is null)
        {
            return null;
        }

        if (component.GetProperty(propertyName)?.GetValue(instance) is int value)
        {
            return value;
        }

        return null;
    }

    private static bool IsSamePost(BlogPost post, BlogPost otherPost)
    {
        return string.Equals(post.Url, otherPost.Url, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublished(BlogPost post)
    {
        return post.Timestamp != DateTime.MinValue &&
               post.Timestamp.Date <= DateTime.UtcNow.Date;
    }

    private string NormalizeTagSlug(string tagSlug)
    {
        return GetTagSlug(Uri.UnescapeDataString(tagSlug ?? string.Empty));
    }

    private static void ResolvePostImage(
        string route,
        string title,
        ref string? image,
        ref string? imageAlt,
        ref string? imageType,
        ref int? imageWidth,
        ref int? imageHeight)
    {
        if (!string.IsNullOrWhiteSpace(image))
        {
            return;
        }

        image = BuildGeneratedSocialImageUrl(route);
        imageAlt = $"Social preview card for {title}";
        imageType = "image/png";
        imageWidth = 1200;
        imageHeight = 600;
    }

    private static string BuildGeneratedSocialImageUrl(string route)
    {
        var slug = route
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault() ?? "post";

        slug = Regex.Replace(slug.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "post";
        }

        return $"/images/social/posts/{slug}.png";
    }

    private static DateTime ReadDateFromComponentName(string componentName)
    {
        var match = Regex.Match(componentName, DatePattern);
        return match.Success &&
               DateTime.TryParseExact(match.Value, "yyyy_MM_dd_", null, System.Globalization.DateTimeStyles.None, out var date)
            ? date
            : DateTime.MinValue;
    }
}
