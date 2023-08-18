using Microsoft.AspNetCore.Components;

namespace BlogEngine;

public abstract class PostLayoutBase : LayoutComponentBase
{
    public BlogPost BlogPost { get; private set; }

    protected bool summary = false;

    public void SetBlogPost(BlogPost blogPost)
    {
        BlogPost = blogPost;

        StateHasChanged();
    }
}