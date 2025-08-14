using System;
using System.Collections.Generic;
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
    public class MarkdownGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Get root namespace from project properties
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace))
            {
                rootNamespace = "mohdali.github.io";
            }

            // Process each markdown file
            foreach (var file in context.AdditionalFiles)
            {
                if (Path.GetExtension(file.Path).Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var content = file.GetText(context.CancellationToken)?.ToString();
                        if (string.IsNullOrEmpty(content))
                            continue;

                        // Parse the markdown file
                        var (frontmatter, htmlContent, codeBlocks) = ParseMarkdown(content);
                        
                        // Extract metadata from frontmatter and filename
                        var fileName = Path.GetFileNameWithoutExtension(file.Path);
                        var metadata = ExtractMetadata(fileName, frontmatter);
                        
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
            }
        }

        private (Dictionary<string, string> frontmatter, string html, List<CodeBlockInfo> codeBlocks) ParseMarkdown(string content)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions() // Tables, footnotes, etc.
                .Build();

            var document = Markdig.Markdown.Parse(content, pipeline);
            var frontmatter = new Dictionary<string, string>();

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
                        frontmatter = deserializer.Deserialize<Dictionary<string, string>>(yamlContent) ?? new Dictionary<string, string>();
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
            
            // Skip the YAML block when rendering
            foreach (var child in document.Where(x => !(x is YamlFrontMatterBlock)))
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

            // Replace <pre><code> blocks with placeholders
            var pattern = @"<pre><code class=""language-(\w+)"">(.*?)</code></pre>";
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

        private BlogPostMetadata ExtractMetadata(string fileName, Dictionary<string, string> frontmatter)
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
                metadata.UrlSlug = match.Groups[4].Value;
                metadata.ClassName = $"Post_{year}_{month:D2}_{day:D2}_{match.Groups[4].Value.Replace("-", "")}";
            }
            else
            {
                // Fallback if filename doesn't match expected format
                metadata.Title = fileName.Replace("-", " ");
                metadata.UrlSlug = fileName;
                metadata.ClassName = $"Post_{fileName.Replace("-", "")}";
                metadata.Date = DateTime.Now;
            }

            // Override with frontmatter values if present
            if (frontmatter.TryGetValue("title", out var title))
                metadata.Title = title;
            
            if (frontmatter.TryGetValue("date", out var dateStr) && DateTime.TryParse(dateStr, out var date))
                metadata.Date = date;
            
            if (frontmatter.TryGetValue("slug", out var slug))
                metadata.UrlSlug = slug;
            
            if (frontmatter.TryGetValue("tags", out var tags))
                metadata.Tags = tags;

            // Get the page route from frontmatter or generate it
            if (frontmatter.TryGetValue("page", out var page))
            {
                metadata.Route = page;
            }
            else
            {
                metadata.Route = $"/posts/{metadata.UrlSlug}";
            }

            return metadata;
        }

        private string GenerateBlazorComponent(BlogPostMetadata metadata, string htmlContent, List<CodeBlockInfo> codeBlocks, string rootNamespace)
        {
            // For verbatim string literals in C#, only quotes need to be doubled
            var escapedTitle = metadata.Title.Replace("\"", "\"\"");

            // Split HTML content by code block placeholders
            var htmlParts = new List<string>();
            var lastIndex = 0;
            
            foreach (var codeBlock in codeBlocks.OrderBy(cb => cb.Index))
            {
                var placeholder = $"%%CODE_BLOCK_{codeBlock.Index}%%";
                var index = htmlContent.IndexOf(placeholder, lastIndex);
                if (index >= 0)
                {
                    // Add HTML before the code block
                    if (index > lastIndex)
                    {
                        htmlParts.Add(htmlContent.Substring(lastIndex, index - lastIndex));
                    }
                    // Add marker for code block
                    htmlParts.Add($"__CODEBLOCK_{codeBlock.Index}__");
                    lastIndex = index + placeholder.Length;
                }
            }
            
            // Add remaining HTML
            if (lastIndex < htmlContent.Length)
            {
                htmlParts.Add(htmlContent.Substring(lastIndex));
            }

            // Generate render tree builder code
            var renderCode = new System.Text.StringBuilder();
            var sequenceNumber = 0;
            
            foreach (var part in htmlParts)
            {
                if (part.StartsWith("__CODEBLOCK_"))
                {
                    var blockIndex = int.Parse(part.Replace("__CODEBLOCK_", "").Replace("__", ""));
                    var codeBlock = codeBlocks.First(cb => cb.Index == blockIndex);
                    var escapedCode = codeBlock.Code.Replace("\"", "\"\"");
                    
                    renderCode.AppendLine($@"            builder.OpenComponent<CodeSnippet>({sequenceNumber++});");
                    if (!string.IsNullOrEmpty(codeBlock.Language))
                    {
                        renderCode.AppendLine($@"            builder.AddAttribute({sequenceNumber++}, ""Language"", ""{codeBlock.Language}"");");
                    }
                    renderCode.AppendLine($@"            builder.AddAttribute({sequenceNumber++}, ""ChildContent"", (RenderFragment)((builder2) =>
            {{
                builder2.AddContent({sequenceNumber++}, @""{escapedCode}"");
            }}));");
                    renderCode.AppendLine($@"            builder.CloseComponent();");
                }
                else if (!string.IsNullOrWhiteSpace(part))
                {
                    var escapedHtml = part.Replace("\"", "\"\"");
                    renderCode.AppendLine($@"            builder.AddMarkupContent({sequenceNumber++}, @""{escapedHtml}"");");
                }
            }

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

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {{
{renderCode}        }}
    }}
}}";

            return code;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }
    }

    internal class BlogPostMetadata
    {
        public string Title { get; set; } = "";
        public string UrlSlug { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Route { get; set; } = "";
        public DateTime Date { get; set; }
        public string Tags { get; set; } = "";
    }

    internal class CodeBlockInfo
    {
        public int Index { get; set; }
        public string Language { get; set; } = "";
        public string Code { get; set; } = "";
    }
}