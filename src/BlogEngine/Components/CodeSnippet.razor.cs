using Microsoft.AspNetCore.Components;

namespace BlogEngine;

public partial class CodeSnippet : ComponentBase
{
    [Inject] private CodeSnippetJsInterop codeSnippetJsInterop { get; set; }

    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public string Language { get; set; } = "csharp";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await codeSnippetJsInterop.HighlightCode();
    }
}
