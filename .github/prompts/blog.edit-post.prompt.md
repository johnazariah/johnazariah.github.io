# Edit Existing Post

Update or modify an existing blog post.

## Finding Posts

Posts are located in `_posts/` with the naming format:
```
YYYY-MM-DD-post-title-slug.md
```

To find a specific post:
```bash
# List all posts
ls _posts/

# Search by keyword in filename
ls _posts/*monad*

# Search by content
grep -r "search term" _posts/
```

## Common Edits

### Update Post Content
Simply edit the Markdown content below the front matter.

### Add Update Date
Add an `update_date` to the front matter to show when the post was last modified:

```yaml
---
    layout: post
    title: "Original Title"
    tags: [tag1, tag2]
    author: johnazariah
    summary: Original summary.
    update_date: 2025-11-26
---
```

This displays "Updated: Nov 26, 2025" in the post header.

### Change Post Date
1. Update the filename: `YYYY-MM-DD-title.md`
2. The URL will change, so update any internal links

### Add/Remove Tags
Update the `tags` array in front matter:
```yaml
tags: [new-tag, existing-tag]
```

Remember to run `build-tags.sh` if you add new tags.

### Update Summary
Modify the `summary` field - this appears at the top of the post.

### Fix Title
Update both:
1. The `title` field in front matter
2. Optionally the filename slug (this changes the URL)

## Post Structure Reference

```markdown
---
    layout: post
    title: "Post Title"
    tags: [tag1, tag2]
    author: johnazariah
    summary: Brief description.
    update_date: 2025-11-26  # Optional
---

### Main Heading

Content goes here...

#### Subheading

More content...

```code
code examples
```

- Bullet points
- More points

1. Numbered lists
2. Work too

> Blockquotes for emphasis

[Links work](https://example.com) like this.

![Images too](/assets/images/2025-11-26/image.png)
```

## After Editing

1. Preview locally (optional): `bundle exec jekyll serve`
2. Commit changes: `git add _posts/your-post.md && git commit -m "Update: post title"`
3. Push to publish: `git push origin main`
