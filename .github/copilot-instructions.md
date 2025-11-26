# Blog Manager Instructions

You are a helpful assistant managing John Azariah's technical blog hosted on GitHub Pages.

## Blog Overview

- **Platform**: Jekyll static site on GitHub Pages
- **Theme**: Architect with custom enhancements (One Dark Pro code blocks, Inter/JetBrains Mono fonts)
- **Focus**: Functional programming, F#, Python, type theory, and programming languages
- **Author**: John Azariah (`johnazariah`)

## Key Directories

| Directory | Purpose |
|-----------|---------|
| `_posts/` | Published blog posts (YYYY-MM-DD-title.md) |
| `_layouts/` | HTML templates (default, home, post, tagpage) |
| `_sass/` | Stylesheets (code-blocks-onedark.scss, modern-enhancements.scss) |
| `assets/images/` | Post images organized by date |
| `code/` | External code samples organized by date |
| `tags/` | Auto-generated tag index pages |
| `future-series/` | Draft series waiting to be published |
| `.github/prompts/` | Workflow prompts for common tasks |

## Available Prompts

Reference these prompts in `.github/prompts/` for detailed workflows (all prefixed with `blog.`):
- `blog.create-new-post.prompt.md` - Create a new blog post
- `blog.publish-from-series.prompt.md` - Publish from future-series drafts
- `blog.add-tag.prompt.md` - Add or regenerate tag pages
- `blog.add-image.prompt.md` - Add images to posts
- `blog.add-code-sample.prompt.md` - Code blocks, MathJax, Mermaid
- `blog.create-series.prompt.md` - Multi-part blog series
- `blog.edit-post.prompt.md` - Modify existing posts
- `blog.publish.prompt.md` - Git workflow for publishing
- `blog.local-preview.prompt.md` - Run Jekyll locally
- `blog.customize-theme.prompt.md` - Theme customization guide
- `blog.conventions.prompt.md` - Style guide reference
- `blog.refresh-context.prompt.md` - Refresh context and analyze blog state
- `blog.publish-aura-series.prompt.md` - Publish from the Aura series (AI development accelerator)

## Front Matter Format

Posts use indented front matter (blog convention):
```yaml
---
    layout: post
    title: "Post Title Here"
    tags: [tag1, tag2, tag3]
    author: johnazariah
    summary: Brief description for the post header.
    update_date: YYYY-MM-DD  # Optional
---
```

## Common Tasks

### Creating a New Post
1. Use filename format: `_posts/YYYY-MM-DD-title-slug.md`
2. Include all required front matter fields
3. Check existing tags in `tags/` folder for consistency
4. Run `build-tags.sh` if new tags are introduced

### Publishing from Future Series
1. Check `future-series/<series>/posts/` for available drafts
2. Add Jekyll front matter to the draft content
3. Add series navigation (previous/next links)
4. Create in `_posts/` with proper date prefix
5. Update previous post's "coming soon" link if applicable

### Code Blocks
- Always specify language: ```fsharp, ```python, ```csharp
- Blog supports MathJax (`$...$` inline, `$$...$$` block)
- Blog supports Mermaid diagrams (```mermaid)
- Syntax highlighting uses One Dark Pro theme

### Tags
- Use existing tags when possible (check `tags/` folder)
- Language tags keep original casing: `F#`, `C#`, `Python`
- Topic tags use lowercase with hyphens: `functional-programming`
- New tags require a page in `tags/<tag-name>/index.html`

## Writing Style

- Technical but accessible
- F# and functional programming perspective
- Use practical examples
- Include code samples with syntax highlighting
- Math notation for formal concepts

## Series in Progress

- **The Parseltongue Chronicles** - Python idioms and patterns (3 posts published)
- **Aura Series** (in `future-series/aura-series/posts/`) - Building an AI development accelerator using story-driven development
  - 01-can-ai-accelerate.md - A skeptic's journey from "fancy autocomplete"
  - 02-the-missing-piece.md - Why LLMs need structure, stories provide it
  - 03-aura-architecture.md - Local-first architecture, agents, orchestration
  - 04-graphrag-case-study.md - Teaching AI codebase understanding with GraphRAG
  - 05-code-i-didnt-write.md - 17 projects, 294 files, zero implementation code
  - 06-the-challenges.md - What went wrong, honest lessons
  - 07-recursive-improvement.md - Aura improving itself
  - 08-getting-started.md - Practical guide to story-driven development
- **Tagless Final Series** (in `future-series/tagless-final-series/posts/`) - DSLs in F#
  - 01-froggy-tree-house.md
  - 02-maps-branches-choices.md
  - 03-goals-threats.md
  - 04-elevators.md
  - 05-verifying-elevators.md
  - 06-model-verification.md

## Publishing Workflow

1. Create/edit post in `_posts/`
2. Ensure tags exist
3. Preview locally (devcontainer or Ruby install)
4. `git add .`
5. `git commit -m "Add post: Title"`
6. `git push origin main`
7. GitHub Pages auto-builds and deploys
