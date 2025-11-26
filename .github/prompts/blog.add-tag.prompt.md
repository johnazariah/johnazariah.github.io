# Add New Tag

Generate tag index pages for the blog's tagging system.

## How Tags Work

1. Posts declare tags in their front matter: `tags: [tag1, tag2]`
2. Each tag needs an index page at `tags/<tag-name>/index.html`
3. The tag page uses the `tagpage` layout to list all posts with that tag

## Instructions

### Option 1: Add a Single Tag

Create a new tag index page:

```html
---
layout : tagpage
tag : your-tag-name
---
```

Save to: `tags/<your-tag-name>/index.html`

### Option 2: Regenerate All Tags

The `build-tags.sh` script scans all posts and regenerates tag pages:

```bash
./build-tags.sh
```

**Note**: This script requires bash. On Windows, use Git Bash or WSL.

### Option 3: Manual Bash Approach

```bash
# Get all unique tags from posts and create tag pages
for tag in $(grep -roh 'tags:\s*\[.*\]' _posts/*.md | \
  sed 's/tags:\s*\[//' | sed 's/\]//' | tr ',' '\n' | \
  sed 's/^[[:space:]]*//;s/[[:space:]]*$//' | sort -u); do
    tagdir="tags/$tag"
    mkdir -p "$tagdir"
    cat > "$tagdir/index.html" << EOF
---
layout : tagpage
tag : $tag
---
EOF
done
```

## After Adding Tags

1. Verify the tag page exists in `tags/<tag-name>/index.html`
2. The sidebar will automatically show the tag with post count
3. Commit and push the new tag page(s)
