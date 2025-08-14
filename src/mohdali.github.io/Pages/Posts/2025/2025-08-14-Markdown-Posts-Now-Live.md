---
title: Markdown Posts Are Now Live!
date: 2025-08-14
page: /posts/markdown-posts-now-live
tags: blazor, markdown, source-generators, claude-code
---

# Markdown Posts Are Now Live! 🎉

After months of being stuck in development limbo, the markdown feature for this blog is finally complete! With the help of [Claude Code](https://claude.ai/code), what was a long-pending task got wrapped up in just a few hours today.

## The Power of Source Generators

This blog now supports writing posts in markdown, and here's the cool part: **this very post you're reading was written in markdown** and compiled into a Blazor component at build time!

The implementation uses C# source generators to:
- Parse markdown files during compilation
- Extract YAML frontmatter for metadata
- Convert markdown to HTML using Markdig
- Transform code blocks into syntax-highlighted components
- Generate fully-functional Blazor components

Here's a glimpse of how it works:

```csharp
[Generator]
public class MarkdownGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // Parse markdown files
        // Generate Blazor components
        // No runtime overhead!
    }
}
```

## Why This Matters

Writing blog posts in markdown brings several benefits:

1. **Faster content creation** - No more wrestling with Razor syntax
2. **Better performance** - All conversion happens at compile time
3. **Full Blazor integration** - Generated components work exactly like hand-written ones
4. **SEO-friendly** - Works perfectly with static pre-rendering

## A Nod to Claude Code

This feature had been sitting incomplete on the `feature/markdown` branch for quite some time. Today, with Claude Code's assistance, we:

- Fixed the source generator implementation
- Resolved namespace and inheritance issues
- Integrated proper layout rendering
- Added syntax highlighting for code blocks
- Ensured compatibility with the existing blog infrastructure

What would have taken days of debugging and trial-and-error was accomplished in a single focused session. The AI didn't just write code—it understood the architecture, debugged issues systematically, and even helped with the proper integration into the existing Blazor WebAssembly setup.

## Technical Implementation

The magic happens through a source generator that:

```yaml
# 1. Reads markdown files with frontmatter
---
title: Your Post Title
date: 2025-08-14
---

# 2. Converts to Blazor components
# 3. Integrates with existing blog system
```

All markdown files in `Pages/Posts/YYYY/` are automatically discovered and converted during build time. The generated components inherit from `BlogPostComponent` and use the same `PostLayout` as regular Razor posts.

## Looking Forward

With markdown support now live, creating new content for this blog becomes significantly easier. The combination of Blazor's component model, C# source generators, and markdown's simplicity creates a powerful blogging platform that's both developer-friendly and performant.

And yes, it's pretty meta that this post announcing markdown support was itself written in markdown and processed by the very system it's describing!

---

*This feature was completed on August 14, 2025, with the assistance of Claude Code—turning a long-pending task into a success story in just a few hours.*