# Publishing Checklist

Use this before moving a post from `draft: true` to public. Markdown posts live under `src/mohdali.github.io/Pages/Posts/{year}/`.

## Content

- Frontmatter includes `title`, `date`, `page`, `description`, and `tags`.
- `date` is the UTC publication date; future-dated posts are excluded from generated routes, RSS, sitemap, archive, and home.
- Use `draft: true` until the post is ready; omit it or set it to `false` for publication.
- Use `image` and `imageAlt` only when the post needs a specific social preview instead of the generated title card.
- Custom social previews include accurate `imageType`; `imageWidth` and `imageHeight` are optional, but must match the actual file when provided.
- Use `cardImage` and `cardImageAlt` only when a specific visual thumbnail improves the homepage/archive card.
- `page` is stable, lowercase, and starts with `/posts/`.
- `description` works as standalone RSS/social copy.
- Tags are specific enough to support archive browsing.
- Images have meaningful alt text and live under `src/mohdali.github.io/wwwroot/images/`.
- Generated social cards are for link previews, not default on-page thumbnails.
- Code blocks declare a language. Mermaid blocks render locally before publishing.
- External links are useful, canonical, and not tracking-heavy.

## CI Guardrails

These map to `.github/workflows/main.yml` and `Prerender/prerender.mjs`.

- Build: run `dotnet publish src/mohdali.github.io/mohdali.github.io.csproj -c Release -o Prerender/output --nologo`.
- Prerender: from `Prerender/`, run `npm install`, `npx playwright install chromium` if needed, then `npm run prerender`.
- Validate: from `Prerender/`, run `npm run validate` after prerendering.
- Route smoke: prerender must not log or throw a NotFound route for the new `page`.
- Mermaid smoke: prerender waits for `.mermaid` diagrams to render; inspect the generated page if a timeout appears.
- Social metadata: confirm the page has title, description, canonical URL, `og:*`, and `twitter:*` tags via `SocialMeta`.
- RSS: confirm `Prerender/output/wwwroot/rss.xml` contains the post title, route, date, and description.
- Sitemap: confirm `Prerender/output/wwwroot/sitemap.xml` contains the post route.
- Draft leakage: keep unfinished posts as `draft: true`; confirm drafts are absent from RSS, sitemap, archive, and prerendered route list.
- Smoke leakage: search generated output for draft-only titles, notes, placeholders, and private links before shipping.

## Manual Review

- Open the generated HTML for the post and scan desktop and mobile widths.
- Check the first viewport, code block wrapping, image sizing, Mermaid readability, and dark/light theme contrast.
- Share with one intended reader or rubber-duck the claim: if the takeaway is unclear, rewrite before publishing.
