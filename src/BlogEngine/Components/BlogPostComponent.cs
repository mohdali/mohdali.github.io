using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace BlogEngine;

public class BlogPostComponent : ComponentBase {
    [CascadingParameter]
    public PostLayoutBase Layout { get; set; }

    [Inject] NavigationManager NavManager { get; set; }
    [Inject] IJSRuntime JsRuntime { get; set; }

    [Inject] BlogPostService blogPostService { get; set; }


    protected override void OnInitialized()
    {
        Layout?.SetBlogPost(blogPostService.GetBlogPost(this.GetType()));
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