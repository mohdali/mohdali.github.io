using Microsoft.JSInterop;

namespace BlogEngine;

public class NavigationJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public NavigationJsInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new (() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlogEngine/navigation.js").AsTask());
    }

    public async ValueTask ScrollToFragment(string elementID)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("scrollToFragment", elementID);
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
