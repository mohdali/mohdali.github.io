---
title: "Post Title"
date: 2026-05-03
page: "/posts/post-slug"
description: "One sentence summary for the homepage, archive, RSS feed, and social previews."
tags:
  - blazor
  - architecture
draft: true
image: "/images/social-card.png"
imageAlt: "Short accessible description of the social preview image."
imageType: "image/png"
imageWidth: 1200
imageHeight: 600
---

# Post Title

Start with the durable idea, surprising result, or concrete problem. Keep the opening paragraph useful when it appears alone in readers and previews.

## Claim

State the main claim in one or two paragraphs. Prefer evidence over framing: measurements, code paths, tradeoffs, or a failed approach that changed the conclusion.

## Evidence

Use normal markdown, focused headings, and short paragraphs.

```csharp
public sealed record BlogPost(
    string Title,
    string Route,
    DateTime Date);
```

Use Mermaid for architecture, flow, and dependency explanations:

```mermaid
flowchart LR
    Markdown[Markdown post] --> Generator[Source generator]
    Generator --> Razor[Razor component]
    Razor --> Prerender[Playwright prerender]
    Prerender --> Pages[Static HTML]
```

Use images when they carry evidence or make a result inspectable:

![Alt text that explains the evidence in the image](/images/example-result.png)

Use links for source material, implementation references, and follow-up reading:

- [Repository source](https://github.com/mohdali/mohdali.github.io)
- [Related post](/posts/simple-blazor-blog)

## Reader Takeaway

End with the reusable lesson: what should the reader change, try, or avoid after reading?

## Publishing Notes

- Hypothesis:
- Primary audience:
- Primary metric:
- Distribution notes:
