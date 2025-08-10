# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Personal blog website built with Blazor WebAssembly, hosted on GitHub Pages with static pre-rendering for SEO.

## Tech Stack

- **Blazor WebAssembly** (.NET 7) - Main framework
- **MudBlazor** - UI component library
- **TypeScript/Webpack** - Client-side assets (syntax highlighting)
- **react-snap** - Static pre-rendering
- **GitHub Actions** - CI/CD deployment

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
- Blog posts are Blazor components in `src/mohdali.github.io/Pages/Posts/YYYY/`
- Naming pattern: `YYYY-MM-DD-Title.razor`
- Posts are discovered automatically via reflection from filename patterns
- Each post must have `@page "/posts/Title"` directive
- Posts inherit from `BlogPostComponent` and use `PostLayout`

### Project Structure
```
├── src/
│   ├── mohdali.github.io/        # Main Blazor WASM app
│   │   ├── Pages/Posts/          # Blog posts organized by year
│   │   └── wwwroot/              # Static assets
│   └── BlogEngine/                # Reusable blog engine library
│       └── ClientLib/             # TypeScript/webpack assets
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