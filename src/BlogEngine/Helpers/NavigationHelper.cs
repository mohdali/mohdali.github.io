using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace BlogEngine;

public class NavigationHelper : IDisposable
{
    private readonly NavigationManager navManager;

    private readonly NavigationJsInterop navigationJsInterop;

    public NavigationHelper(NavigationManager navManager, NavigationJsInterop navigationJsInterop)
    {
        this.navManager = navManager;
        this.navigationJsInterop = navigationJsInterop;

        navManager.LocationChanged += TryFragmentNavigation;
    }

    public async ValueTask NavigateToFragmentAsync()
    {
        var uri = navManager.ToAbsoluteUri(navManager.Uri);

        if (uri.Fragment.Length > 0)
        {
            await navigationJsInterop.ScrollToFragment(uri.Fragment.Substring(1));
        }
    }

    private async void TryFragmentNavigation(object? sender, LocationChangedEventArgs args)
    {
        await NavigateToFragmentAsync();
    }

    public void Dispose()
    {
        navManager.LocationChanged -= TryFragmentNavigation;
    }

}
