using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Markdig;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

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

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fs, (b) => {
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

                    context.AddSource($"CSharp_{Path.GetFileNameWithoutExtension(file.Path)}.g.cs", generatedCode);
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

            public override Stream Read()
            {
                using(var reader = new StreamReader(_item.Read()))
                {
                    var piepline = new MarkdownPipelineBuilder()
                        .Build();

                    var stringContent = reader.ReadToEnd();

                    var htmlString = Markdig.Markdown.ToHtml(stringContent, piepline);

                    var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(htmlString));

                    return outputStream;
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {

        }
    }
}
