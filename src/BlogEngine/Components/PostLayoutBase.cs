using Microsoft.AspNetCore.Components;

namespace BlogEngine;

public abstract class PostLayoutBase : LayoutComponentBase
{
    public BlogPost BlogPost { get; private set; }

    protected bool summary = false;

    [Inject]
    public NavigationHelper NavigationHelper { get; set; }

    public void SetBlogPost(BlogPost blogPost)
    {
        BlogPost = blogPost;

        StateHasChanged();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await NavigationHelper.NavigateToFragmentAsync();
        }
    }
}