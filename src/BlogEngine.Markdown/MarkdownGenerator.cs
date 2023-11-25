using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using YamlDotNet.Serialization;

namespace BlogEngine.Markdown
{
    [Generator]
    public class MarkdownGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDirectory);

            var fs = RazorProjectFileSystem.Create(projectDirectory);

            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace))
            {
                rootNamespace = "MD";
            }

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fs, (b) =>
            {
                b.SetRootNamespace(rootNamespace);
            });

            foreach (var file in context.AdditionalFiles)
            {
                if (Path.GetExtension(file.Path).Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    var item = projectEngine.FileSystem.GetItem(file.Path, FileKinds.Component);

                    var newItem = new MarkdownProjectItem(item);

                    var codeDocument = projectEngine.Process(newItem);

                    var csharpDocument = codeDocument.GetCSharpDocument();

                    var generatedCode = csharpDocument.GeneratedCode;

                    context.AddSource($"{Path.GetFileNameWithoutExtension(file.Path)}.g.cs", generatedCode);
                }
            }
        }

        internal class MarkdownProjectItem : RazorProjectItem
        {
            private RazorProjectItem _item;
            public MarkdownProjectItem(RazorProjectItem item)
            {
                this._item = item;
            }
            public override string BasePath => _item.BasePath;

            public override string FilePath => _item.FilePath;

            public override string PhysicalPath => _item.PhysicalPath;

            public override bool Exists => _item.Exists;

            public override string FileKind => _item.FileKind;

            public Dictionary<string, string> Frontmatter { get; private set; }

            public string Htmlstring { get; private set; }

            public override Stream Read()
            {
                using (var reader = new StreamReader(_item.Read()))
                {
                    var pipeline = new MarkdownPipelineBuilder()
                        .UseYamlFrontMatter()
                        .Build();

                    var writer = new StringWriter();

                    var renderer = new HtmlRenderer(writer);

                    pipeline.Setup(renderer);

                    var stringContent = reader.ReadToEnd();

                    var document = Markdig.Markdown.Parse(stringContent, pipeline);

                    var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

                    if(yamlBlock != null)
                    {
                        string yaml = stringContent.Substring(yamlBlock.Span.Start , yamlBlock.Span.Length).Replace("---\r\n","").Replace("\r\n---", "");
                        
                        var deserializer = new DeserializerBuilder()
                            .Build();

                        Frontmatter = deserializer.Deserialize<Dictionary<string, string>>(yaml);
                    }

                    renderer.Render(document);

                    writer.Flush();

                    Htmlstring = writer.ToString();

                    foreach(var kv in Frontmatter)
                    {
                        Htmlstring = $"@{kv.Key} \"{kv.Value}\"\r\n{Htmlstring}";
                    }
                    
                    var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(Htmlstring));

                    return outputStream;
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {

        }
    }
}
