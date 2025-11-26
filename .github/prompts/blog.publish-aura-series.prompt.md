---
name: blogPublishAuraSeries
description: Publish posts from the Aura series - John's journey building an AI development accelerator using story-driven development.
argument-hint: Post number to publish (1-8) or "next" for the next unpublished post
---
# Publish from Aura Series

The **Aura Series** documents John's journey building an AI development accelerator using story-driven development methodology. It's a candid exploration of what worked, what didn't, and the surprising results of three weeks of AI collaboration.

## Series Overview

| # | File | Title | Summary |
|---|------|-------|---------|
| 1 | `01-can-ai-accelerate.md` | Can AI Actually Accelerate Development? | A skeptic's journey from "fancy autocomplete" - introducing the experiment |
| 2 | `02-the-missing-piece.md` | The Missing Piece | Why LLMs need structure, and how stories provide it |
| 3 | `03-aura-architecture.md` | Aura: Architecture of an AI Accelerator | How the pieces fit together - local-first, agents, orchestration |
| 4 | `04-graphrag-case-study.md` | Teaching AI to Understand Your Codebase | A GraphRAG case study - giving agents contextual knowledge |
| 5 | `05-code-i-didnt-write.md` | Code I Didn't Write | The surprising results - 17 projects, 294 files, zero implementation code |
| 6 | `06-the-challenges.md` | The Challenges | What went wrong, what remains unsolved, honest lessons |
| 7 | `07-recursive-improvement.md` | Aura is Eating Itself | When your AI tool improves AI development - the recursive property |
| 8 | `08-getting-started.md` | Getting Started | A practical guide to story-driven development |

## Suggested Tags

```yaml
tags: [AI, development-tools, story-driven-development, C#, productivity]
```

Consider also: `local-first`, `GraphRAG`, `agents`, `VS-Code`

## Publishing Instructions

### 1. Read the Draft
```bash
cat future-series/aura-series/posts/0N-filename.md
```

### 2. Create the Post File
Filename format: `_posts/YYYY-MM-DD-aura-series-title-slug.md`

Example: `_posts/2025-11-26-aura-series-can-ai-accelerate.md`

### 3. Add Front Matter
The drafts don't have Jekyll front matter. Add:

```yaml
---
    layout: post
    title: "Aura Series: Part N - [Title]"
    tags: [AI, development-tools, story-driven-development, C#, productivity]
    author: johnazariah
    summary: Part N of the Aura Series. [Brief description from the subtitle]
---
```

### 4. Add Series Navigation

**At the top of each post:**
```markdown
> **Series: Building Aura - An AI Development Accelerator**
> 1. [Can AI Actually Accelerate Development?](/YYYY/MM/DD/aura-series-can-ai-accelerate.html)
> 2. **The Missing Piece** (you are here)
> 3. Aura: Architecture (coming soon)
> ...
```

**At the bottom of each post:**
```markdown
---

**Previous**: [Part N-1: Title](/YYYY/MM/DD/aura-series-previous.html)

**Next**: [Part N+1: Title](/YYYY/MM/DD/aura-series-next.html) *(coming soon)*
```

### 5. Create New Tags (if needed)

Check if these tags exist:
```bash
ls tags/AI tags/development-tools tags/story-driven-development tags/productivity 2>/dev/null
```

Create missing tag pages in `tags/<tag-name>/index.html`:
```html
---
layout : tagpage
tag : tag-name
---
```

### 6. Update Previous Post (if applicable)

When publishing Part N, update Part N-1's "coming soon" link to the actual URL.

## Recommended Publishing Schedule

This is a substantial series. Consider:
- **Option A**: Publish weekly (8 weeks of content)
- **Option B**: Publish twice weekly (1 month of content)
- **Option C**: Publish 2-3 at launch, then weekly (builds anticipation)

## Series Themes for Cross-Linking

Posts in this series connect well with:
- **Parseltongue Chronicles** - Both explore AI-assisted development
- **Tagless Final Series** - Architecture and abstraction themes
- Any posts about development methodology or productivity

## Example: Publishing Post 1

```bash
# Read the draft
cat future-series/aura-series/posts/01-can-ai-accelerate.md

# Create the post (use today's date)
# Add front matter + series navigation + content
# File: _posts/2025-11-26-aura-series-can-ai-accelerate.md
```

Front matter for Post 1:
```yaml
---
    layout: post
    title: "Aura Series: Part 1 - Can AI Actually Accelerate Development?"
    tags: [AI, development-tools, story-driven-development, C#, productivity]
    author: johnazariah
    summary: Part 1 of the Aura Series. A skeptic's journey from "fancy autocomplete" to building a real AI development accelerator.
---
```
