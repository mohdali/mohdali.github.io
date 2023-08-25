using Microsoft.JSInterop;

namespace BlogEngine;

public class NavigationJsInterop
{
    private readonly IJSRuntime jSRuntime;
    public NavigationJsInterop(IJSRuntime jsRuntime)
    {
        this.jSRuntime = jsRuntime;
    }

    public async ValueTask ScrollToFragment(string elementID)
    {
        await jSRuntime.InvokeVoidAsync("window.BlogEngine.scrollToFragment", elementID);
    }    
}
