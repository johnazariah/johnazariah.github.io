# Publish Post from Future Series

Take a draft post from `future-series/<series>/posts/` and prepare it for publication.

## Available Series

Check `future-series/` for available series:
- `tagless-final-series/posts/` - Posts about tagless final pattern in F#
  - `01-froggy-tree-house.md`
  - `02-maps-branches-choices.md`
  - `03-goals-threats.md`
  - `04-elevators.md`
  - `05-verifying-elevators.md`
  - `06-model-verification.md`
- `aura-series/` - (Planning stage)

## Instructions

1. **Identify the series and post to publish**:
   ```bash
   # List available series
   ls future-series/
   
   # List posts in a series
   ls future-series/<series-name>/posts/
   ```

2. **Read the draft post** from `future-series/<series>/posts/NN-post-name.md`

3. **Prepare for publication**:
   - Add proper front matter (the drafts may not have Jekyll front matter)
   - Set today's date or the desired publication date
   - Create the filename: `YYYY-MM-DD-series-prefix-post-name.md`
   - Ensure tags are consistent with other posts in the series
   - Add series navigation links (previous/next posts)

4. **Create the post** in `_posts/`

5. **Generate any new tags** if needed (see `add-tag.prompt.md`)

## Front Matter Template for Series Posts

```yaml
---
    layout: post
    title: "Series Name: Part N - Post Title"
    tags: [series-tag, topic1, topic2, F#]
    author: johnazariah
    summary: Part N of the Series Name series. Brief description of this post's content.
---
```

## Series Navigation Block

Add at the beginning of each post:

```markdown
> **Series: [Series Name]**
> 1. [Part 1: Title](/YYYY/MM/DD/series-part-1.html) 
> 2. **Part 2: Current Title** (you are here)
> 3. Part 3: Upcoming Title (coming soon)
```

Add at the end:

```markdown
---

**Previous**: [Part N-1: Title](/YYYY/MM/DD/series-part-N-1.html)

**Next**: [Part N+1: Title](/YYYY/MM/DD/series-part-N+1.html) *(coming soon)*
```

## Example: Publishing from tagless-final-series

Given draft: `future-series/tagless-final-series/posts/01-froggy-tree-house.md`

**Step 1**: Read the draft content

**Step 2**: Create `_posts/2025-11-26-tagless-final-froggy-tree-house.md`:

```markdown
---
    layout: post
    title: "Tagless Final in F#: Part 1 - Froggy Tree House"
    tags: [tagless-final, F#, functional-programming, DSL]
    author: johnazariah
    summary: Part 1 of the Tagless Final series. We introduce the concept through a tiny DSL for controlling a frog in a tree house game.
---

> **Series: Tagless Final in F#**
> 1. **Froggy Tree House** (you are here)
> 2. Maps, Branches, and Choices (coming soon)
> 3. Goals and Threats (coming soon)

[Original draft content here, converted as needed...]

---

**Next**: Part 2 - Maps, Branches, and Choices *(coming soon)*
```

## Post-Publish Checklist

- [ ] Draft content properly converted to Jekyll post format
- [ ] Front matter includes all required fields
- [ ] Series navigation added (top and bottom)
- [ ] Code blocks have proper syntax highlighting
- [ ] Any images moved to `assets/images/YYYY-MM-DD/`
- [ ] Tags created if new ones introduced
- [ ] Internal links use correct Jekyll URL format
- [ ] Previous posts in series updated with "Next" link (if applicable)

## Updating Previous Posts in Series

When publishing Part N, update Part N-1 to include the actual link:

Change:
```markdown
**Next**: Part N: Title *(coming soon)*
```

To:
```markdown
**Next**: [Part N: Title](/YYYY/MM/DD/series-part-N.html)
```
