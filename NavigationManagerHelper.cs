
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace mohdali.github.io;

public static class NavigationManagerHelper
{
    public static ValueTask NavigateToFragmentAsync(this NavigationManager navigationManager, IJSRuntime jSRuntime)
    {
        var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);

        if (uri.Fragment.Length == 0)
        {
            return default;
        }
        return jSRuntime.InvokeVoidAsync("blazorHelpers.scrollToFragment", uri.Fragment.Substring(1));
    }
}