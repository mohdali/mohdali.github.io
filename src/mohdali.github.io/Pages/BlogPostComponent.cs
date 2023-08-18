using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace mohdali.github.io.Pages;

public class BlogPostComponent : ComponentBase {
    [CascadingParameter]
    public PostLayout Layout { get; set; }

    [Inject] NavigationManager NavManager { get; set; }
    [Inject] IJSRuntime JsRuntime { get; set; }


    protected override void OnInitialized()
    {
        Layout?.SetBlogPost(BlogPostsHelper.GetBlogPost(this.GetType()));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await NavManager.NavigateToFragmentAsync(JsRuntime);
        }
    }

    private async void TryFragmentNavigation(object sender, LocationChangedEventArgs args)
    {
        await NavManager.NavigateToFragmentAsync(JsRuntime);
    }

    public void Dispose()
    {
        NavManager.LocationChanged -= TryFragmentNavigation;
    }
}