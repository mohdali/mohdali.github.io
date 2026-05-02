---
title: The Blazor Blog, Three Years Later
date: 2026-05-02
page: /posts/blazor-blog-three-years-later
description: A follow-up to the 2023 Blazor blog post covering markdown authoring, source generation, prerendering, and UI polish.
tags:
  - blazor
  - markdown
  - github-pages
  - static-site
---

# The Blazor Blog, Three Years Later

In 2023 I wrote [Simple Blazor Blog](/posts/Simple-Blazor-Blog), a short note about building this site as a Blazor WebAssembly app hosted on GitHub Pages. The main idea was simple: each post was a Blazor component, the app discovered those components at runtime, and a prerendering step produced static HTML so the site would behave more like a normal blog.

That post still describes the original shape of the project, but a lot of the implementation details are obsolete now. The site is still a static Blazor blog, but the way posts are authored, rendered, indexed, and deployed has changed quite a bit.

## The Old Version

The 2023 version had two important constraints:

1. Posts were written as `.razor` components.
2. The static output was produced with `react-snap`.

The Razor approach worked, but it made writing feel too close to application development. Every paragraph needed component syntax, and simple prose carried more ceremony than it should. It was flexible, especially for interactive posts, but it was not a great default for regular writing.

The prerendering setup also worked, but `react-snap` was always a workaround. It was designed for React apps, and the blog needed extra configuration like `skipThirdPartyRequests` to keep the static generation reliable.

The "next steps" section of that post said I wanted markdown authoring. That is now done.

## Markdown Is Now The Default

New posts can be plain markdown files with YAML frontmatter:

```yaml
---
title: The Blazor Blog, Three Years Later
date: 2026-05-02
page: /posts/blazor-blog-three-years-later
description: A follow-up to the original Simple Blazor Blog post.
tags:
  - blazor
  - markdown
---
```

The file name still matters, but frontmatter now carries the human-facing metadata. The title, date, description, route, and tags are all explicit. That makes the archive and home page less dependent on guessing intent from a class name or file name.

The interesting part is that markdown is not parsed at runtime. The project has a C# incremental source generator that reads `.md` files during compilation, parses them with Markdig, and generates Blazor components.

That keeps the runtime model close to the original design. The blog still ends up with normal routed Blazor components. They just happen to be generated from markdown instead of handwritten as Razor.

## Code Blocks Are Still Components

Markdown code fences are converted into the same `CodeSnippet` component used by the older Razor posts. That keeps syntax highlighting and copy behavior consistent across old and new posts.

```csharp
[Generator]
public class MarkdownGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover markdown files during compilation
        // Parse frontmatter and markdown
        // Generate routed Blazor components
    }
}
```

This also means the site can keep improving the code block experience in one place. Syntax highlighting, light and dark palettes, copy buttons, and the small "Copied!" tooltip are all styling and client-library concerns, not authoring concerns.

## Prerendering Moved To Playwright

The site no longer uses `react-snap`. The prerender step is now a small Node script built around Playwright.

The script does a few jobs:

1. Discovers Razor and markdown posts from `Pages/Posts`.
2. Writes `rss.xml`, `sitemap.xml`, and `robots.txt`.
3. Starts a local static server from the publish output.
4. Opens each route in Chromium.
5. Saves the rendered HTML back into the static output.
6. Copies the home page to `404.html` for GitHub Pages fallback routing.

This is more direct than the old setup. It understands this project, it does not need to pretend the app is React, and it lets the build fail if a route renders the not-found page.

## Deployment Is More Explicit

The GitHub Actions workflow now does the full production build:

```yaml
- name: Publish .NET Core Project
  run: dotnet publish src/mohdali.github.io/mohdali.github.io.csproj -c Release -o Prerender/output --nologo

- name: Pre-render Blazor pages
  working-directory: Prerender
  run: |
    npm install
    npx playwright install chromium
    npm run prerender
```

The generated `Prerender/output/wwwroot` folder is then deployed to the `gh-pages` branch. GitHub Pages still hosts a static site, but the static output is now produced by a build process that knows about the blog content.

## Still Blazor, Still MudBlazor

The frontend is still Blazor WebAssembly and still uses MudBlazor. The recent work was not a framework rewrite. It was mostly a cleanup of the experience around the existing stack.

The site now has:

1. A calmer home page with latest posts.
2. A real posts archive grouped by year.
3. RSS and sitemap output.
4. Light and dark themes with a custom MudBlazor palette.
5. Cleaner typography and spacing.
6. Better code block colors.
7. A less awkward copy button.
8. Better handling for embedded interactive content.

Most of the visual changes are CSS and theme configuration on top of MudBlazor. That is a good place for this site to be. MudBlazor handles the component primitives, while the site stylesheet makes the blog feel like its own thing.

## What Is Obsolete In The 2023 Post

If I were rebuilding this now, I would not start from the exact recipe in the old post.

I would still keep Blazor WebAssembly for static hosting. I would still keep the idea that posts become routed components. I would still prerender for fast first paint and crawlable pages.

But I would change these parts:

1. Use markdown for ordinary posts.
2. Use Razor posts only when a post needs custom interactive UI.
3. Use a source generator instead of hand-authoring every post component.
4. Use Playwright for prerendering instead of `react-snap`.
5. Generate RSS and sitemap files as part of prerendering.
6. Put post metadata in frontmatter instead of deriving everything from component names.

The old post was a good first version. The current version keeps the same spirit, but removes a lot of friction from writing and publishing.

The best test is this post itself: it is just a markdown file in the repo, and the build turns it into a routed, styled, prerendered page.
