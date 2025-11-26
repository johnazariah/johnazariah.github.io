# Add Image to Post

Add images to blog posts with proper organization.

## Image Storage Convention

Store images in date-organized folders:
```
assets/images/YYYY-MM-DD/image-name.png
```

Example: `assets/images/2025-11-26/diagram.png`

## Instructions

1. **Create the date folder** (if it doesn't exist):
   ```bash
   mkdir -p assets/images/2025-11-26
   ```

2. **Copy your image** to the folder

3. **Reference in Markdown**:
   ```markdown
   ![Alt text description](/assets/images/2025-11-26/image-name.png)
   ```

## Image Best Practices

### Sizing
- Keep images under 500KB when possible
- Use appropriate formats:
  - **PNG**: Screenshots, diagrams, text-heavy images
  - **JPG**: Photos, complex images
  - **SVG**: Vector graphics, icons
  - **GIF**: Simple animations

### Accessibility
Always include descriptive alt text:
```markdown
![A flowchart showing the monad bind operation](/assets/images/2025-11-26/monad-flow.png)
```

### Responsive Images (Optional)
For better mobile experience, you can add width constraints:
```html
<img src="/assets/images/2025-11-26/large-diagram.png" alt="Description" style="max-width: 100%;">
```

### Centered Images
```html
<p align="center">
  <img src="/assets/images/2025-11-26/diagram.png" alt="Description">
</p>
```

## Example Usage in Post

```markdown
---
    layout: post
    title: "Understanding Recursion"
    tags: [recursion, functional-programming]
    author: johnazariah
    summary: A visual guide to recursion.
---

### The Recursion Pattern

Here's how recursion works:

![Recursion tree diagram showing function calls](/assets/images/2025-11-26/recursion-tree.png)

As you can see in the diagram above...
```
