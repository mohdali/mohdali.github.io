# Measurement And Experiment Playbook

Every post should answer one evidence question. The goal is not vanity traffic; it is learning which formats and topics create durable technical value.

## Per-Post Hypothesis

Before publishing, write this in the post's publishing notes or issue:

- Hypothesis: "If I publish `<format>` about `<technical problem>`, then `<audience>` will do `<observable behavior>` because `<reason>`."
- Primary audience: one clear reader group.
- Primary metric: one behavior that proves value.
- Secondary signals: comments, saves, backlinks, search impressions, or source-code clicks.
- Distribution: where the post will be shared and why that audience fits.

## Measurement Windows

Record lightweight notes at 24 hours, 7 days, and 30 days.

| Window | What to Check | Question |
| --- | --- | --- |
| 24h | GA realtime/traffic, referrers, social replies, obvious rendering issues | Did the hook and distribution reach the intended audience? |
| 7d | GA engagement, Search Console impressions/clicks, referrers, backlinks, code clicks | Did readers stay, share, or search their way in? |
| 30d | Search trend, long-tail referrers, repeat references, conversions to related posts | Is this evergreen, episodic, or a dead end? |

## Sources

- GA: page views, engaged sessions, average engagement time, outbound clicks if configured.
- Search Console: queries, impressions, clicks, click-through rate, indexed URL status.
- Referrers: GitHub, Hacker News, Reddit, LinkedIn, Mastodon, Bluesky, newsletters, docs sites.
- Social notes: exact phrasing that drove replies, saves, reposts, or misunderstandings.
- Qualitative evidence: questions readers ask, code snippets they copy, diagrams they reference.

## Decision Rules

- Double down: 30-day search or referral traffic keeps growing, or readers ask for the next layer of detail.
- Rewrite title/description: strong impressions with weak clicks, or social replies indicate the value is unclear.
- Add follow-up: readers ask implementation questions that are too large for comments.
- Add diagram/interactive asset: readers misunderstand flow, lifecycle, or data shape.
- Stop the track: low engagement across all windows and no useful qualitative signal.

## Experiment Backlog

| Track | Evidence Question | Candidate Posts | Success Signal |
| --- | --- | --- | --- |
| Blazor/source-generator architecture | Do implementation-deep architecture posts attract search and GitHub-oriented readers? | Markdown-to-Razor source generator internals; prerender pipeline design; draft filtering and content discovery | Search Console queries, GitHub referrers, source-code clicks, technical questions |
| Mermaid diagram-heavy explanation | Do dense diagrams make complex posts more understandable and shareable? | Blazor static publishing flow; component/data lifecycle; RSS/sitemap generation path | Longer engagement time, diagram mentions, fewer clarification questions |
| Observable/D3/interactive visual explanation | Do interactive explanations create enough value to justify the build cost? | Rolling shutter visualizer refresh; source-generator dependency graph explorer; prerender route timing explorer | Repeat visits, direct links to the tool, higher time on page, implementation requests |

Review the backlog monthly. Promote one experiment only when the previous post has at least a 7-day readout or a clear qualitative reason to continue.
