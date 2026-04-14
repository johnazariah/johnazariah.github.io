#!/bin/bash

# Generate tag index pages from post front matter
rm -rf tags

# Extract tags from front matter, normalize, and create pages
grep -h "^    tags:" _posts/*.md \
  | sed 's/\r//g' \
  | sed 's/^    tags: *\[//; s/\] *$//' \
  | tr ',' '\n' \
  | sed 's/^ *//; s/ *$//' \
  | sort -u \
  | while read -r tag; do
      [ -z "$tag" ] && continue
      # Use lowercase directory name for URL consistency
      dir=$(echo "$tag" | tr '[:upper:]' '[:lower:]' | tr ' ' '-')
      mkdir -p "tags/$dir"
      cat > "tags/$dir/index.html" <<EOF
---
layout: tagpage
tag: $tag
---
EOF
    done
