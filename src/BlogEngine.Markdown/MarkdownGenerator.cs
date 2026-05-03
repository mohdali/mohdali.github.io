using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YamlDotNet.Serialization;

namespace BlogEngine.Markdown
{
    [Generator]
    public class MarkdownGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var markdownFiles = context.AdditionalTextsProvider
                .Where(file => Path.GetExtension(file.Path).Equals(".md", StringComparison.OrdinalIgnoreCase));

            var rootNamespace = context.AnalyzerConfigOptionsProvider
                .Select((options, _) => options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value)
                    ? value
                    : "mohdali.github.io");

            var markdownInputs = markdownFiles.Combine(rootNamespace);

            context.RegisterSourceOutput(markdownInputs, (sourceContext, input) =>
            {
                ProcessMarkdownFile(sourceContext, input.Left, input.Right);
            });
        }

        private void ProcessMarkdownFile(SourceProductionContext context, AdditionalText file, string rootNamespace)
        {
            try
            {
                var content = file.GetText(context.CancellationToken)?.ToString();
                if (string.IsNullOrEmpty(content))
                    return;

                // Parse the markdown file
                var (frontmatter, htmlContent, codeBlocks) = ParseMarkdown(content);

                // Extract metadata from frontmatter and filename
                var fileName = Path.GetFileNameWithoutExtension(file.Path);
                var metadata = ExtractMetadata(fileName, frontmatter);

                if (metadata.IsDraft || IsFuturePost(metadata.Date))
                    return;

                // Generate the Blazor component
                var componentCode = GenerateBlazorComponent(metadata, htmlContent, codeBlocks, rootNamespace);

                // Add the generated source
                var sourceFileName = $"{metadata.ClassName}.g.cs";
                context.AddSource(sourceFileName, SourceText.From(componentCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                // Report diagnostic if something goes wrong
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "MD001",
                        "Markdown Processing Error",
                        $"Error processing markdown file {{0}}: {{1}}",
                        "MarkdownGenerator",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None,
                    Path.GetFileName(file.Path),
                    ex.Message);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private (Dictionary<string, object> frontmatter, string html, List<CodeBlockInfo> codeBlocks) ParseMarkdown(string content)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions() // Tables, footnotes, etc.
                .Build();

            var document = Markdig.Markdown.Parse(content, pipeline);
            var frontmatter = new Dictionary<string, object>();

            // Extract YAML frontmatter
            var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
            if (yamlBlock != null)
            {
                var yamlContent = content.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
                yamlContent = yamlContent.Replace("---\r\n", "").Replace("---\n", "").Replace("\r\n---", "").Replace("\n---", "");

                if (!string.IsNullOrWhiteSpace(yamlContent))
                {
                    var deserializer = new DeserializerBuilder().Build();
                    try
                    {
                        frontmatter = deserializer.Deserialize<Dictionary<string, object>>(yamlContent) ?? new Dictionary<string, object>();
                    }
                    catch
                    {
                        // If YAML parsing fails, continue with empty frontmatter
                    }
                }
            }

            // Convert to HTML
            var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer);
            pipeline.Setup(renderer);

            var contentBlocks = document
                .Where(x => !(x is YamlFrontMatterBlock))
                .ToList();

            // PostLayout already renders the post title, so a leading markdown H1 would duplicate it.
            if (contentBlocks.FirstOrDefault() is HeadingBlock { Level: 1 })
            {
                contentBlocks.RemoveAt(0);
            }

            foreach (var child in contentBlocks)
            {
                renderer.Render(child);
            }

            writer.Flush();
            var html = writer.ToString();

            // Process code blocks to extract them and replace with placeholders
            var codeBlocks = ProcessCodeBlocks(ref html);

            return (frontmatter, html, codeBlocks);
        }

        private List<CodeBlockInfo> ProcessCodeBlocks(ref string html)
        {
            var codeBlocks = new List<CodeBlockInfo>();
            var blockIndex = 0;

            // Markdig's advanced extensions render Mermaid fences as <pre class="mermaid">.
            var pattern = @"<pre class=""mermaid"">(.*?)</pre>";
            html = Regex.Replace(html, pattern, m =>
            {
                var code = System.Web.HttpUtility.HtmlDecode(m.Groups[1].Value);
                var placeholder = $"%%CODE_BLOCK_{blockIndex}%%";
                codeBlocks.Add(new CodeBlockInfo { Index = blockIndex, Language = "mermaid", Code = code });
                blockIndex++;
                return placeholder;
            }, RegexOptions.Singleline);

            // Replace <pre><code> blocks with placeholders
            pattern = @"<pre><code class=""language-([^""]+)"">(.*?)</code></pre>";
            html = Regex.Replace(html, pattern, m =>
            {
                var language = m.Groups[1].Value;
                var code = System.Web.HttpUtility.HtmlDecode(m.Groups[2].Value);
                var placeholder = $"%%CODE_BLOCK_{blockIndex}%%";
                codeBlocks.Add(new CodeBlockInfo { Index = blockIndex, Language = language, Code = code });
                blockIndex++;
                return placeholder;
            }, RegexOptions.Singleline);

            // Handle code blocks without language specification
            pattern = @"<pre><code>(.*?)</code></pre>";
            html = Regex.Replace(html, pattern, m =>
            {
                var code = System.Web.HttpUtility.HtmlDecode(m.Groups[1].Value);
                var placeholder = $"%%CODE_BLOCK_{blockIndex}%%";
                codeBlocks.Add(new CodeBlockInfo { Index = blockIndex, Language = "", Code = code });
                blockIndex++;
                return placeholder;
            }, RegexOptions.Singleline);

            return codeBlocks;
        }

        private BlogPostMetadata ExtractMetadata(string fileName, Dictionary<string, object> frontmatter)
        {
            var metadata = new BlogPostMetadata();

            // Parse filename for date and title (format: YYYY-MM-DD-Title.md)
            var match = Regex.Match(fileName, @"^(\d{4})-(\d{2})-(\d{2})-(.+)$");
            if (match.Success)
            {
                var year = int.Parse(match.Groups[1].Value);
                var month = int.Parse(match.Groups[2].Value);
                var day = int.Parse(match.Groups[3].Value);
                metadata.Date = new DateTime(year, month, day);
                metadata.Title = match.Groups[4].Value.Replace("-", " ");
                metadata.UrlSlug = Slugify(match.Groups[4].Value);
                metadata.ClassName = BuildClassName(metadata.Date, metadata.UrlSlug);
            }
            else
            {
                // Fallback if filename doesn't match expected format
                metadata.Title = fileName.Replace("-", " ");
                metadata.UrlSlug = Slugify(fileName);
                metadata.ClassName = $"Post_{ToIdentifier(metadata.UrlSlug)}";
                metadata.Date = DateTime.MinValue;
            }

            // Override with frontmatter values if present
            if (TryGetString(frontmatter, "title", out var title))
                metadata.Title = title;

            if (TryGetString(frontmatter, "date", out var dateStr) && DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                metadata.Date = date;

            if (TryGetString(frontmatter, "slug", out var slug))
                metadata.UrlSlug = Slugify(slug);

            metadata.Tags = GetTags(frontmatter);
            metadata.Description = GetFirstString(frontmatter, "description", "excerpt", "summary") ?? "";
            metadata.Image = GetFirstString(frontmatter, "image", "imageUrl", "socialImage", "ogImage");
            metadata.ImageAlt = GetFirstString(frontmatter, "imageAlt", "image_alt", "socialImageAlt", "ogImageAlt");
            metadata.ImageType = GetFirstString(frontmatter, "imageType", "image_type", "socialImageType", "ogImageType");
            metadata.ImageWidth = TryGetInt(frontmatter, "imageWidth", out var imageWidth) ? imageWidth : null;
            metadata.ImageHeight = TryGetInt(frontmatter, "imageHeight", out var imageHeight) ? imageHeight : null;
            metadata.IsDraft = TryGetBool(frontmatter, "draft", out var draft) && draft;

            // Get the page route from frontmatter or generate it
            if (TryGetString(frontmatter, "page", out var page))
            {
                metadata.Route = page;
            }
            else
            {
                metadata.Route = $"/posts/{metadata.UrlSlug}";
            }

            metadata.ClassName = BuildClassName(metadata.Date, metadata.UrlSlug);

            return metadata;
        }

        private static bool TryGetString(Dictionary<string, object> frontmatter, string key, out string value)
        {
            value = "";

            if (!frontmatter.TryGetValue(key, out var rawValue) || rawValue == null)
                return false;

            if (rawValue is DateTime date)
            {
                value = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return true;
            }

            value = rawValue.ToString()?.Trim() ?? "";
            return !string.IsNullOrWhiteSpace(value);
        }

        private static string GetFirstString(Dictionary<string, object> frontmatter, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (TryGetString(frontmatter, key, out var value))
                    return value;
            }

            return "";
        }

        private static bool TryGetBool(Dictionary<string, object> frontmatter, string key, out bool value)
        {
            value = false;

            if (!frontmatter.TryGetValue(key, out var rawValue) || rawValue == null)
                return false;

            if (rawValue is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            return bool.TryParse(rawValue.ToString(), out value);
        }

        private static bool TryGetInt(Dictionary<string, object> frontmatter, string key, out int value)
        {
            value = 0;

            if (!frontmatter.TryGetValue(key, out var rawValue) || rawValue == null)
                return false;

            if (rawValue is int intValue)
            {
                value = intValue;
                return value > 0;
            }

            return int.TryParse(rawValue.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) &&
                   value > 0;
        }

        private static bool IsFuturePost(DateTime date)
        {
            return date != DateTime.MinValue && date.Date > DateTime.UtcNow.Date;
        }

        private static string[] GetTags(Dictionary<string, object> frontmatter)
        {
            if (!frontmatter.TryGetValue("tags", out var rawTags) || rawTags == null)
                return new string[0];

            if (rawTags is IEnumerable<object> tagList)
            {
                return tagList
                    .Select(tag => tag?.ToString()?.Trim() ?? "")
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            return rawTags
                .ToString()
                ?.Split(',')
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? new string[0];
        }

        private static string Slugify(string value)
        {
            var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-");
            slug = Regex.Replace(slug, @"^-+|-+$", "");
            return string.IsNullOrWhiteSpace(slug) ? "post" : slug;
        }

        private static string BuildClassName(DateTime date, string slug)
        {
            var datePrefix = date == DateTime.MinValue
                ? ""
                : $"{date.Year}_{date.Month:D2}_{date.Day:D2}_";

            return $"Post_{datePrefix}{ToIdentifier(slug)}";
        }

        private static string ToIdentifier(string value)
        {
            var words = Regex.Split(value, @"[^A-Za-z0-9]+")
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1));

            var identifier = string.Concat(words);
            return string.IsNullOrWhiteSpace(identifier) ? "Markdown" : identifier;
        }

        private string GenerateBlazorComponent(BlogPostMetadata metadata, string htmlContent, List<CodeBlockInfo> codeBlocks, string rootNamespace)
        {
            // For verbatim string literals in C#, only quotes need to be doubled
            var escapedTitle = EscapeVerbatimString(metadata.Title);
            var escapedDescription = EscapeVerbatimString(metadata.Description);
            var escapedImage = EscapeVerbatimString(metadata.Image);
            var escapedImageAlt = EscapeVerbatimString(metadata.ImageAlt);
            var escapedImageType = EscapeVerbatimString(metadata.ImageType);
            var imageWidth = FormatNullableInt(metadata.ImageWidth);
            var imageHeight = FormatNullableInt(metadata.ImageHeight);
            var tags = metadata.Tags.Length == 0
                ? "Array.Empty<string>()"
                : $"new[] {{ {string.Join(", ", metadata.Tags.Select(tag => $"@\"{EscapeVerbatimString(tag)}\""))} }}";

            // Split HTML content by code block placeholders in document order.
            // Code block indices are assigned by block type, so numeric order can
            // differ from where placeholders appear in the rendered HTML.
            var htmlParts = new List<string>();
            var lastIndex = 0;
            var codeBlockByIndex = codeBlocks.ToDictionary(codeBlock => codeBlock.Index);

            foreach (Match match in Regex.Matches(htmlContent, @"%%CODE_BLOCK_(\d+)%%"))
            {
                if (!int.TryParse(match.Groups[1].Value, out var blockIndex) ||
                    !codeBlockByIndex.ContainsKey(blockIndex))
                {
                    continue;
                }

                // Add HTML before the code block
                if (match.Index > lastIndex)
                {
                    htmlParts.Add(htmlContent.Substring(lastIndex, match.Index - lastIndex));
                }

                // Add marker for code block
                htmlParts.Add($"__CODEBLOCK_{blockIndex}__");
                lastIndex = match.Index + match.Length;
            }

            // Add remaining HTML
            if (lastIndex < htmlContent.Length)
            {
                htmlParts.Add(htmlContent.Substring(lastIndex));
            }

            // Generate render tree builder code
            var renderCode = new System.Text.StringBuilder();
            var sequenceNumber = 0;

            // Open wrapper div with post-content class for proper styling
            renderCode.AppendLine($@"            builder.OpenElement({sequenceNumber++}, ""div"");");
            renderCode.AppendLine($@"            builder.AddAttribute({sequenceNumber++}, ""class"", ""post-content"");");

            foreach (var part in htmlParts)
            {
                if (part.StartsWith("__CODEBLOCK_"))
                {
                    var blockIndex = int.Parse(part.Replace("__CODEBLOCK_", "").Replace("__", ""));
                    var codeBlock = codeBlockByIndex[blockIndex];
                    var escapedCode = EscapeVerbatimString(codeBlock.Code);

                    if (codeBlock.IsMermaid)
                    {
                        renderCode.AppendLine($@"            builder.OpenComponent<MermaidDiagram>({sequenceNumber++});");
                    }
                    else
                    {
                        renderCode.AppendLine($@"            builder.OpenComponent<CodeSnippet>({sequenceNumber++});");
                        if (!string.IsNullOrEmpty(codeBlock.Language))
                        {
                            renderCode.AppendLine($@"            builder.AddAttribute({sequenceNumber++}, ""Language"", @""{EscapeVerbatimString(codeBlock.Language)}"");");
                        }
                    }

                    renderCode.AppendLine($@"            builder.AddAttribute({sequenceNumber++}, ""ChildContent"", (RenderFragment)((builder2) =>
            {{
                builder2.AddContent({sequenceNumber++}, @""{escapedCode}"");
            }}));");
                    renderCode.AppendLine($@"            builder.CloseComponent();");
                }
                else if (!string.IsNullOrWhiteSpace(part))
                {
                    var escapedHtml = EscapeVerbatimString(part);
                    renderCode.AppendLine($@"            builder.AddMarkupContent({sequenceNumber++}, @""{escapedHtml}"");");
                }
            }

            // Close wrapper div
            renderCode.AppendLine($@"            builder.CloseElement();");

            // Generate C# code that will be compiled along with the main project
            var code = $@"// <auto-generated/>
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlogEngine;
using {rootNamespace}.Pages;

namespace {rootNamespace}.Pages.Posts.Generated
{{
    [Route(""{metadata.Route}"")]
    [Layout(typeof(PostLayout))]
    public partial class {metadata.ClassName} : BlogPostComponent
    {{
        public string Title {{ get; set; }} = @""{escapedTitle}"";
        public DateTime Timestamp {{ get; set; }} = new DateTime({metadata.Date.Year}, {metadata.Date.Month}, {metadata.Date.Day});
        public string Description {{ get; set; }} = @""{escapedDescription}"";
        public string[] Tags {{ get; set; }} = {tags};
        public string Image {{ get; set; }} = @""{escapedImage}"";
        public string ImageAlt {{ get; set; }} = @""{escapedImageAlt}"";
        public string ImageType {{ get; set; }} = @""{escapedImageType}"";
        public int? ImageWidth {{ get; set; }} = {imageWidth};
        public int? ImageHeight {{ get; set; }} = {imageHeight};

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {{
{renderCode}        }}
    }}
}}";

            return code;
        }

        private static string EscapeVerbatimString(string value)
        {
            return value.Replace("\"", "\"\"");
        }

        private static string FormatNullableInt(int? value)
        {
            return value.HasValue
                ? value.Value.ToString(CultureInfo.InvariantCulture)
                : "null";
        }
    }

    internal class BlogPostMetadata
    {
        public string Title { get; set; } = "";
        public string UrlSlug { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Route { get; set; } = "";
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public string Image { get; set; } = "";
        public string ImageAlt { get; set; } = "";
        public string ImageType { get; set; } = "";
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public string[] Tags { get; set; } = new string[0];
        public bool IsDraft { get; set; }
    }

    internal class CodeBlockInfo
    {
        public int Index { get; set; }
        public string Language { get; set; } = "";
        public string Code { get; set; } = "";
        public bool IsMermaid => string.Equals(Language, "mermaid", StringComparison.OrdinalIgnoreCase);
    }
}
