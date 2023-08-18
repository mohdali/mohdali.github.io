using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace BlogEngine;

public class BlogPostComponent : ComponentBase {
    [CascadingParameter]
    public PostLayoutBase Layout { get; set; }

    [Inject] BlogPostService blogPostService { get; set; }
    
    protected override void OnInitialized()
    {
        Layout?.SetBlogPost(blogPostService.GetBlogPost(this.GetType()));
    }
}