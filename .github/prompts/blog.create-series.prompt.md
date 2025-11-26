# Create Blog Post Series

Create a multi-part blog post series with consistent naming and linking.

## Series Naming Convention

Use a consistent prefix for all posts in a series:
```
YYYY-MM-DD-series-name-part-N.md
```

Examples from existing series:
- `2019-12-09-lego-railway-tracks-origami-post-1.md` through `post-5.md`
- `2021-12-10-scientific-computing-with-fsharp-1.md` through `5.md`
- `2024-12-15-the-parseltongue-chronicles-intro.md`, then `decorators.md`, `pipelines-basic.md`

## Series Structure Options

### Option 1: Numbered Parts
```
2025-11-26-my-series-part-1.md
2025-11-26-my-series-part-2.md
2025-11-26-my-series-part-3.md
```

### Option 2: Named Parts
```
2025-11-26-my-series-intro.md
2025-11-27-my-series-basics.md
2025-11-28-my-series-advanced.md
```

## Front Matter for Series Posts

Include series information in the summary:

```yaml
---
    layout: post
    title: "My Series: Part 1 - Introduction"
    tags: [series-tag, topic1, topic2]
    author: johnazariah
    summary: Part 1 of the My Series. This post introduces the core concepts we'll explore throughout the series.
---
```

## Navigation Between Posts

### At the Start of Each Post
```markdown
> This is **Part 2** of the "My Series" series.
> - [Part 1: Introduction](/2025/11/26/my-series-part-1.html)
> - **Part 2: Deep Dive** (you are here)
> - [Part 3: Advanced Topics](/2025/11/28/my-series-part-3.html)
```

### At the End of Each Post
```markdown
---

### Next in the Series

Continue to [Part 2: Deep Dive](/2025/11/27/my-series-part-2.html) where we explore...

### Series Index

1. [Introduction](/2025/11/26/my-series-part-1.html)
2. **Deep Dive** (current)
3. [Advanced Topics](/2025/11/28/my-series-part-3.html)
```

## Consistent Tagging

Use a unique tag for the series to group posts:
```yaml
tags: [my-series, functional-programming, F#]
```

This creates a tag page that lists all posts in the series.

## Planning a Series

Before starting, consider creating a planning document in `future-series/`:

```
future-series/
  my-series/
    OUTLINE.md
    NOTES.md
    drafts/
```

## Example: Creating a 3-Part Series

### Part 1: Introduction
```markdown
---
    layout: post
    title: "Understanding Monads: Part 1 - What and Why"
    tags: [monads-series, F#, functional-programming, monads]
    author: johnazariah
    summary: Part 1 of the Understanding Monads series. We explore what monads are and why they matter.
---

### Understanding Monads Series

Welcome to my series on monads! In this series, we'll cover:

1. **What and Why** (this post) - Understanding the problem monads solve
2. [How They Work](/2025/11/27/understanding-monads-part-2.html) - The mechanics
3. [Practical Examples](/2025/11/28/understanding-monads-part-3.html) - Real-world usage

---

### Introduction

[Your content here...]

---

**Next**: [Part 2 - How They Work](/2025/11/27/understanding-monads-part-2.html)
```
