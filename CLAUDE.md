# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Personal blog website built with Blazor WebAssembly, hosted on GitHub Pages with static pre-rendering for SEO.

## Tech Stack

- **Blazor WebAssembly** (.NET 9) - Main framework
- **MudBlazor v7** - Modern Material Design component library
- **TypeScript/Webpack** - Client-side assets (syntax highlighting)
- **react-snap** - Static pre-rendering
- **GitHub Actions** - CI/CD deployment
- **C# Source Generators** - Compile-time markdown to Blazor conversion
- **Markdig** - Markdown parsing with advanced features

## Common Commands

### Development
```bash
# Run development server
dotnet watch run --project src/mohdali.github.io/mohdali.github.io.csproj

# Build solution
dotnet build

# Build client assets (in BlogEngine/ClientLib)
npm run build
```

### Production Build & Deploy
```bash
# Publish for production
dotnet publish src/mohdali.github.io/mohdali.github.io.csproj -c Release -o Prerender/output

# Pre-render static pages (run from Prerender folder)
npx react-snap

# Full pre-render pipeline
./pre-render.ps1
```

## Architecture

### Blog Post System

#### Creating Posts
Blog posts can be created in two ways:

1. **Razor Components** (Traditional method):
   - Create `.razor` files in `src/mohdali.github.io/Pages/Posts/YYYY/`
   - Must inherit from `BlogPostComponent`
   - Must include `@page "/posts/url-slug"` directive
   - Example: `2025-01-14-My-Post.razor`

2. **Markdown Files** (New feature!):
   - Create `.md` files in `src/mohdali.github.io/Pages/Posts/YYYY/`
   - Automatically converted to Blazor components at compile time
   - Support YAML frontmatter for metadata
   - Example: `2025-01-14-My-Post.md`

#### Markdown Post Format
```markdown
---
title: Post Title Here
date: 2025-01-14
page: /posts/url-slug
tags: tag1, tag2, tag3
---

# Your Markdown Content

Write your post using **standard markdown** syntax.

## Code blocks are automatically syntax highlighted

\`\`\`csharp
public class Example
{
    public string Name { get; set; }
}
\`\`\`
```

#### Features
- Posts are discovered automatically via reflection
- Markdown posts use C# source generators for compile-time conversion
- Code blocks in markdown are converted to `CodeSnippet` components
- Supports all Markdig features (tables, footnotes, task lists, etc.)
- Both post types work identically once compiled

### Project Structure
```
├── src/
│   ├── mohdali.github.io/        # Main Blazor WASM app
│   │   ├── Pages/Posts/          # Blog posts organized by year (.razor and .md)
│   │   └── wwwroot/              # Static assets
│   ├── BlogEngine/                # Reusable blog engine library
│   │   └── ClientLib/             # TypeScript/webpack assets
│   └── BlogEngine.Markdown/       # Source generator for markdown posts
└── Prerender/                     # Pre-rendering configuration
```

### Client Assets Build Process
The BlogEngine/ClientLib contains TypeScript code for syntax highlighting that gets built with webpack:
1. TypeScript files in `BlogEngine/ClientLib/src/`
2. Webpack bundles to `BlogEngine/wwwroot/js/`
3. Automatically builds during .NET build via MSBuild target

### Deployment Pipeline
GitHub Actions workflow on push to master:
1. Builds and publishes Blazor app
2. Runs react-snap for static pre-rendering
3. Removes WASM bootstrap scripts from HTML for SEO
4. Deploys to gh-pages branch

## Key Development Notes

- When modifying client-side JavaScript/TypeScript, rebuild in `BlogEngine/ClientLib`
- Blog posts must follow the exact naming convention to be discovered
- Pre-rendering is essential for SEO - always test with `pre-render.ps1` before deploying
- The site works both as static HTML (initial load) and as Blazor WASM (after JS loads)