using Microsoft.AspNetCore.Components;

namespace BlogEngine;

public partial class Detail : ComponentBase
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [CascadingParameter]
    public bool? Summary { get; set; } = false;
}