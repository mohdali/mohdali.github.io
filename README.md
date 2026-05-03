# mohdali.dev

Personal blog for <https://mohdali.dev>. The site is a Blazor WebAssembly app that is published as static GitHub Pages output after a build-time prerender pass.

## Publish A Post

1. Create a markdown file under `src/mohdali.github.io/Pages/Posts/YYYY/`.
2. Name it `YYYY-MM-DD-your-post-slug.md`.
3. Add frontmatter:

```markdown
---
title: Your Post Title
date: 2026-05-02
description: One sentence that can appear on cards, feeds, and link previews.
tags:
  - dotnet
  - blogging
---
```

4. Write the post body in normal markdown.
5. Merge to `master`. The GitHub Actions workflow builds, prerenders, and deploys to the `gh-pages` branch.

Optional frontmatter:

- `page: /posts/custom-url` overrides the generated route.
- `slug: custom-url` overrides the filename slug when `page` is not set.
- `draft: true` skips the post during build-time generation.
- Future `date` values are treated as scheduled posts and are excluded from generated routes, RSS, sitemap, archive, and home until a later deploy on or after that UTC date.
- Custom social preview images can use `image`, `imageAlt`, `imageType`, `imageWidth`, and `imageHeight`; dimensions are optional, but they are validated when provided. If `image` is omitted, prerendering generates a per-post social card.
- On-page post cards use `cardImage` and `cardImageAlt`. Leave them unset for a text-only card; generated social cards are not shown as thumbnails unless explicitly referenced.

Use [docs/post-template.md](docs/post-template.md) as a starting point.

## Local Commands

```bash
dotnet build
dotnet watch run --project src/mohdali.github.io/mohdali.github.io.csproj
dotnet publish src/mohdali.github.io/mohdali.github.io.csproj -c Release -o Prerender/output
npm install --prefix Prerender
npm run prerender --prefix Prerender
npm run validate --prefix Prerender
```

## Stack Decision

The original Blazor prerendering idea is still viable for this site because the content is mostly static, GitHub Pages remains the host, and the existing code already has a build-time markdown source generator. The modernization keeps that architecture but removes the main friction: markdown posts are now first-class content, prerender discovery includes markdown routes, and the deployment emits `rss.xml`, `sitemap.xml`, and `robots.txt`.
