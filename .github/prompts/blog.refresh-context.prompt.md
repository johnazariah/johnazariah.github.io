---
name: blogRefreshContext
description: Refresh context and analyze the current state of John Azariah's technical blog. Use this to get oriented before any blog management task.
argument-hint: Optional focus area (e.g., "series", "tags", "theme", "posts")
---
# Blog Context Refresh

You are managing **John Azariah's technical blog** at https://johnazariah.github.io.

## Quick Orientation

When this prompt is invoked, perform these steps to understand the current blog state:

### 1. Scan Published Posts
```bash
ls -la _posts/
```
Review recent posts, identify active series, and note any patterns.

### 2. Check Available Draft Series
```bash
ls -la future-series/
ls -la future-series/*/posts/ 2>/dev/null
```
Identify unpublished content ready for publication.

### 3. Verify Tag Consistency
```bash
# Extract tags from posts
grep -roh 'tags:\s*\[.*\]' _posts/*.md | sort -u

# List existing tag pages
ls tags/*/index.html | sed 's|tags/||' | sed 's|/index.html||'
```
Ensure all used tags have corresponding pages.

### 4. Check for Build Issues
```bash
bundle exec jekyll build 2>&1 | tail -20
```

---

## Blog Architecture

### Platform & Theme
- **Engine**: Jekyll 3.9.x on GitHub Pages
- **Theme**: Architect (`pages-themes/architect@v0.2.0`) with custom enhancements
- **Code Highlighting**: One Dark Pro theme (Rouge/Pygments)
- **Typography**: Inter (body), JetBrains Mono (code), Architects Daughter (headings)
- **Features**: MathJax equations, Mermaid diagrams, Disqus comments

### Directory Structure
```
johnazariah.github.io/
├── _posts/                    # Published posts (YYYY-MM-DD-title.md)
├── _layouts/                  # Templates (default, home, post, tagpage)
├── _sass/                     # Stylesheets
│   ├── code-blocks-onedark.scss
│   └── modern-enhancements.scss
├── _includes/                 # Partials (analytics, collecttags)
├── assets/
│   ├── css/                   # Compiled styles
│   └── images/YYYY-MM-DD/     # Post images by date
├── code/YYYY-MM-DD/           # External code samples
├── tags/<tag-name>/           # Tag index pages
├── future-series/             # Unpublished draft series
│   ├── tagless-final-series/  # 6-part F# DSL series
│   └── aura-series/           # Planning stage
├── .github/
│   ├── prompts/               # All blog.*.prompt.md files
│   └── copilot-instructions.md
└── .devcontainer/             # Development container config
```

### Front Matter Convention
Posts use **indented** front matter (blog-specific style):
```yaml
---
    layout: post
    title: "Post Title"
    tags: [tag1, tag2, tag3]
    author: johnazariah
    summary: 1-2 sentence description shown in post header.
    update_date: YYYY-MM-DD  # Optional: for updated posts
---
```

---

## Content Inventory

### Published Series
| Series | Posts | Status |
|--------|-------|--------|
| Lego Railway Tracks Origami | 5 parts | Complete |
| Scientific Computing with F# | 5 parts | Complete |
| The Parseltongue Chronicles | 3 parts | In Progress |

### Ready to Publish (future-series/tagless-final-series/posts/)
1. `01-froggy-tree-house.md` - Intro to tagless-final DSLs
2. `02-maps-branches-choices.md` - Nondeterminism and choice
3. `03-goals-threats.md` - Win/lose states, safety analysis
4. `04-elevators.md` - Surprise domain shift to elevators
5. `05-verifying-elevators.md` - Safety verification
6. `06-model-verification.md` - The reveal: model checking

### Ready to Publish (future-series/aura-series/posts/)
1. `01-can-ai-accelerate.md` - A skeptic's journey from "fancy autocomplete"
2. `02-the-missing-piece.md` - Why LLMs need structure, stories provide it
3. `03-aura-architecture.md` - Local-first architecture, agents, orchestration
4. `04-graphrag-case-study.md` - Teaching AI codebase understanding with GraphRAG
5. `05-code-i-didnt-write.md` - 17 projects, 294 files, zero implementation code
6. `06-the-challenges.md` - What went wrong, honest lessons
7. `07-recursive-improvement.md` - Aura improving itself
8. `08-getting-started.md` - Practical guide to story-driven development

### Tag Categories
- **Languages**: `F#`, `C#`, `Python`, `javascript`, `Q#`
- **Paradigms**: `functional-programming`, `programming-languages`
- **Concepts**: `monads`, `functors`, `applicatives`, `recursion`, `trampolines`
- **Theory**: `lambda-calculus`, `y-combinator`, `free-monad`
- **Topics**: `scientific-computing`, `quantum-computing`, `parsing`

---

## Available Prompts

All prompts are in `.github/prompts/` with `blog.` prefix:

| Prompt | Purpose |
|--------|---------|
| `blog.create-new-post` | Create a new blog post with proper front matter |
| `blog.publish-from-series` | Publish a draft from future-series |
| `blog.publish-aura-series` | Publish from the Aura AI development series |
| `blog.add-tag` | Create or regenerate tag pages |
| `blog.add-image` | Add images with proper organization |
| `blog.add-code-sample` | Code blocks, MathJax, Mermaid |
| `blog.create-series` | Plan a multi-part blog series |
| `blog.edit-post` | Modify an existing post |
| `blog.publish` | Git workflow for publishing changes |
| `blog.local-preview` | Run Jekyll locally for testing |
| `blog.customize-theme` | Theme and styling customization |
| `blog.conventions` | Writing style and format guide |

---

## Writing Style

John's posts are:
- **Technical but accessible** - Deep concepts explained clearly
- **Functional programming perspective** - F# is the primary lens
- **Example-driven** - Code samples with syntax highlighting
- **Mathematically inclined** - Uses notation when it clarifies
- **Slightly whimsical** - Occasional humor, creative analogies

---

## Common Tasks

### To Create a New Post
1. Determine filename: `_posts/YYYY-MM-DD-title-slug.md`
2. Add indented front matter with all required fields
3. Check `tags/` for existing tags (create new ones if needed)
4. Write content with proper heading levels (start with `###`)
5. Run `./build-tags.sh` if new tags were added

### To Publish from Tagless-Final Series
1. Read the draft from `future-series/tagless-final-series/posts/`
2. Add Jekyll front matter (drafts may not have it)
3. Add series navigation (previous/next links)
4. Create file in `_posts/` with today's date
5. Update previous post's "coming soon" link

### To Preview Locally
```bash
bundle exec jekyll serve --livereload --host 0.0.0.0
# Open http://localhost:4000
```

### To Publish Changes
```bash
git add .
git status
git commit -m "Add post: Title"
git push origin main
```

---

## Quick Health Check

Run this to verify blog health:
```bash
# Build test
bundle exec jekyll build

# Check for missing tags
for tag in $(grep -roh 'tags:\s*\[.*\]' _posts/*.md | \
  sed 's/tags:\s*\[//' | sed 's/\]//' | tr ',' '\n' | \
  sed 's/^[[:space:]]*//;s/[[:space:]]*$//' | sort -u); do
    [ ! -f "tags/$tag/index.html" ] && echo "Missing tag: $tag"
done
```

---

## Notes for AI Assistant

When managing this blog:
1. **Preserve indented front matter style** - This is intentional
2. **Match existing tag casing** - `F#` not `f#`, `Python` not `python`  
3. **Use bash commands** - We're in a Linux devcontainer
4. **Check before creating** - Verify tags/files don't already exist
5. **Series navigation** - Include prev/next links in series posts
6. **Code language tags** - Always specify: ```fsharp, ```python, etc.
