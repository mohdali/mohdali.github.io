using Microsoft.JSInterop;

namespace BlogEngine;

public class CodeSnippetJsInterop
{
    private readonly IJSRuntime jSRuntime;
    
    public CodeSnippetJsInterop(IJSRuntime jsRuntime)
    {
        this.jSRuntime = jsRuntime;
    }

    public async ValueTask HighlightCode()
    {
        await jSRuntime.InvokeVoidAsync("window.BlogEngine.highlightCode");
    }

}
