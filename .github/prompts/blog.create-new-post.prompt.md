# Create New Blog Post

Create a new blog post for my Jekyll blog.

## Instructions

1. Ask me for the following details (if not provided):
   - **Title**: The post title
   - **Tags**: Relevant tags (check existing tags in `tags/` folder for consistency)
   - **Summary**: A 1-2 sentence description for the post header
   - **Topic/Content**: What the post should be about

2. Create the post file:
   - Location: `_posts/`
   - Filename format: `YYYY-MM-DD-title-slug.md` (use today's date, lowercase, hyphens for spaces)
   - Use the `post` layout

3. Required front matter format:
```yaml
---
    layout: post
    title: "Your Title Here"
    tags: [tag1, tag2, tag3]
    author: johnazariah
    summary: Your summary here describing the post content.
---
```

4. After creating the post:
   - Check if any new tags were introduced
   - If so, remind me to run the tag generation (see `add-tag.prompt.md`)

## Existing Tags Reference

Check the `tags/` folder for existing tags to maintain consistency. Common tags include:
- Programming languages: `F#`, `C#`, `Python`, `javascript`, `Q#`
- Topics: `functional-programming`, `monads`, `recursion`, `parsing`
- Concepts: `lambda-calculus`, `y-combinator`, `scientific-computing`

## Example

```markdown
---
    layout: post
    title: "Understanding Monads in F#"
    tags: [F#, functional-programming, monads]
    author: johnazariah
    summary: A practical guide to understanding and using monads in F# with real-world examples.
---

### Introduction

Your content here...
```
