# Customize Blog Theme

Guide for customizing the blog's visual appearance.

## Theme Architecture

The blog uses a layered styling approach:

```
assets/css/style.scss          # Main entry point
  └── _sass/
      ├── jekyll-theme-architect.scss  # Base Architect theme
      ├── normalize.scss               # CSS reset
      ├── rouge-github.scss            # Original syntax highlighting (overridden)
      ├── code-blocks-onedark.scss     # ✨ Modern code syntax highlighting
      └── modern-enhancements.scss     # ✨ Typography & UI improvements
```

## Code Block Themes

The blog uses **One Dark Pro** inspired syntax highlighting. To customize colors, edit `_sass/code-blocks-onedark.scss`:

### Color Variables
```scss
$code-bg: #282c34;        // Background
$code-fg: #abb2bf;        // Default text
$code-keyword: #c678dd;   // Keywords (let, if, match)
$code-string: #98c379;    // Strings
$code-number: #d19a66;    // Numbers
$code-function: #61afef;  // Functions
$code-type: #e5c07b;      // Types
$code-operator: #56b6c2;  // Operators (|>, >>=)
$code-comment: #5c6370;   // Comments
$code-variable: #e06c75;  // Variables
```

### Alternative Themes

**Dracula Theme:**
```scss
$code-bg: #282a36;
$code-fg: #f8f8f2;
$code-keyword: #ff79c6;
$code-string: #f1fa8c;
$code-function: #50fa7b;
$code-type: #8be9fd;
$code-comment: #6272a4;
```

**GitHub Dark:**
```scss
$code-bg: #0d1117;
$code-fg: #c9d1d9;
$code-keyword: #ff7b72;
$code-string: #a5d6ff;
$code-function: #d2a8ff;
$code-type: #79c0ff;
$code-comment: #8b949e;
```

**Nord Theme:**
```scss
$code-bg: #2e3440;
$code-fg: #d8dee9;
$code-keyword: #81a1c1;
$code-string: #a3be8c;
$code-function: #88c0d0;
$code-type: #8fbcbb;
$code-comment: #616e88;
```

## Typography Settings

Edit `_sass/modern-enhancements.scss` to customize fonts:

```scss
// Font families
$font-body: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
$font-heading: 'Architects Daughter', $font-body;
$font-mono: 'JetBrains Mono', 'Fira Code', Consolas, monospace;

// Font sizes
body { font-size: 17px; }
#main-content h1 { font-size: 2.25em; }
#main-content h2 { font-size: 1.75em; }
#main-content h3 { font-size: 1.4em; }
```

### Recommended Code Fonts
- **JetBrains Mono** - Excellent ligatures, very readable
- **Fira Code** - Popular, good ligatures
- **Cascadia Code** - Microsoft's modern font
- **Source Code Pro** - Adobe's clean monospace

## Color Scheme

Edit the color variables in `modern-enhancements.scss`:

```scss
$color-primary: #2879d0;      // Links, accents
$color-primary-dark: #1e5a9e; // Hover states
$color-accent: #9ddcff;       // Highlights
$color-text: #374151;         // Body text
$color-heading: #1f2937;      // Headings
$color-bg: #ffffff;           // Background
$color-border: #e5e7eb;       // Borders
```

## Enabling Font Ligatures

The code blocks have ligatures enabled for beautiful operators:
- `->` becomes →
- `=>` becomes ⇒
- `|>` becomes ▷
- `>=` becomes ≥

This is controlled by:
```scss
code {
  font-feature-settings: "liga" 1, "calt" 1;
}
```

To disable ligatures:
```scss
code {
  font-feature-settings: "liga" 0, "calt" 0;
}
```

## Adding Dark Mode (Future Enhancement)

To add a dark mode toggle, you would need to:

1. Add CSS custom properties (CSS variables)
2. Create dark theme overrides
3. Add JavaScript for toggle functionality

Example structure:
```scss
:root {
  --color-bg: #ffffff;
  --color-text: #374151;
}

[data-theme="dark"] {
  --color-bg: #1a1b26;
  --color-text: #a9b1d6;
}

body {
  background: var(--color-bg);
  color: var(--color-text);
}
```

## Testing Changes Locally

```bash
bundle exec jekyll serve --livereload
```

Changes to SCSS files will auto-compile and refresh the browser.

## Quick Customization Examples

### Make code blocks rounder
```scss
// In code-blocks-onedark.scss
pre {
  border-radius: 12px;  // Increase from 8px
}
```

### Add code block title bar effect
```scss
pre {
  position: relative;
  padding-top: 2.5em;
  
  &::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 2em;
    background: darken($code-bg, 5%);
    border-radius: 8px 8px 0 0;
  }
}
```

### Increase line spacing in code
```scss
pre code {
  line-height: 1.8;  // Increase from 1.6
}
```
