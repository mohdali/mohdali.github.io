using Microsoft.JSInterop;

namespace BlogEngine;

public class MermaidJsInterop
{
    private readonly IJSRuntime jsRuntime;

    public MermaidJsInterop(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    public async ValueTask RenderMermaidDiagrams()
    {
        await jsRuntime.InvokeVoidAsync("window.BlogEngine.renderMermaidDiagrams");
    }
}
