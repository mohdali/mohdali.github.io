using Microsoft.AspNetCore.Components;

namespace BlogEngine;

public partial class MermaidDiagram : ComponentBase
{
    [Inject] private MermaidJsInterop MermaidJsInterop { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await MermaidJsInterop.RenderMermaidDiagrams();
    }
}
