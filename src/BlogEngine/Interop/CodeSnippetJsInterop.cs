using Microsoft.JSInterop;

namespace BlogEngine;

public class CodeSnippetJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    
    public CodeSnippetJsInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new (() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlogEngine/highlight-code.js").AsTask());
    }

    public async ValueTask HighlightCode()
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("highlightCode");
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
