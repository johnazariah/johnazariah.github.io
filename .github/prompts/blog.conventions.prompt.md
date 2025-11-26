# Blog Conventions & Style Guide

Reference guide for consistent blogging on johnazariah.github.io.

## Site Information

- **Author**: John Azariah
- **Theme**: Architect (via `pages-themes/architect@v0.2.0`)
- **Hosted**: GitHub Pages
- **URL**: https://johnazariah.github.io

## File Structure

```
_posts/              # Blog posts (YYYY-MM-DD-title.md)
_layouts/            # HTML templates
_includes/           # Reusable HTML snippets
_sass/               # SCSS stylesheets
assets/
  css/               # Compiled CSS
  images/            # Post images (organized by date)
code/                # External code samples (organized by date)
tags/                # Tag index pages
future-series/       # Planning for future content
```

## Front Matter Template

```yaml
---
    layout: post
    title: "Your Post Title"
    tags: [tag1, tag2, tag3]
    author: johnazariah
    summary: A concise description of your post (1-2 sentences).
    update_date: YYYY-MM-DD  # Optional: when post was last updated
---
```

**Note**: The indentation in front matter is a convention used in this blog.

## Writing Style

### Headings
- Use `###` (h3) for main sections (h1/h2 reserved for title/layout)
- Use `####` (h4) for subsections
- Keep headings concise and descriptive

### Code
- Always specify language for syntax highlighting
- Prefer F# examples where applicable
- Store long code samples in `code/YYYY-MM-DD/`

### Math
- Use MathJax: `$inline$` or `$$block$$`
- Left-aligned by default (configured in post.html)

### Diagrams
- Mermaid.js is available for flowcharts, sequence diagrams, etc.

## Tagging Guidelines

### Existing Tag Categories

**Languages**:
- `F#`, `C#`, `Python`, `javascript`, `Q#`

**Paradigms**:
- `functional-programming`, `programming-languages`

**Concepts**:
- `monads`, `functors`, `applicatives`
- `recursion`, `trampolines`
- `lambda-calculus`, `y-combinator`
- `parsing`, `fparsec`

**Topics**:
- `scientific-computing`, `quantum-computing`
- `functional-data-structures`, `immutable-data-structures`
- `trees`, `composition`

**Algorithms**:
- `brkga`, `evolutionary-algorithms`, `ising`, `tsp`

### Tag Naming Rules
- Use lowercase with hyphens: `functional-programming`
- Exception: Language names keep original casing: `F#`, `C#`, `Q#`
- Be specific but not too narrow
- Reuse existing tags when possible

## URL Structure

Posts are accessible at:
```
/YYYY/MM/DD/post-title-slug.html
```

Tags are accessible at:
```
/tags/tag-name/
```

## Content Themes

Based on existing posts, common themes include:
- Functional programming concepts and patterns
- F# tutorials and explorations
- Mathematical/scientific computing
- Programming language comparisons
- Algorithm implementations
- Lambda calculus and type theory

## Series Conventions

For multi-part posts:
1. Use consistent filename prefix
2. Include part number in title
3. Use a series-specific tag
4. Add navigation links between parts

Examples:
- "Scientific Computing with F#" (5 parts)
- "The Parseltongue Chronicles" (ongoing)
- "Lego Railway Tracks Origami" (5 parts)
