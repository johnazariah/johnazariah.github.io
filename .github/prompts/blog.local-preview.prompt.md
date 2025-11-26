# Local Preview

Run the blog locally to preview changes before publishing.

## Prerequisites

1. **Ruby** installed (2.7+ recommended)
2. **Bundler** gem installed: `gem install bundler`

## First-Time Setup

```bash
# Install dependencies
bundle install
```

## Run Local Server

```bash
bundle exec jekyll serve
```

Or with live reload:

```bash
bundle exec jekyll serve --livereload
```

Or to include draft posts:

```bash
bundle exec jekyll serve --drafts
```

## Access the Site

- **Local URL**: http://localhost:4000
- **Live reload port**: 35729 (if enabled)

## Common Options

| Flag | Purpose |
|------|---------|
| `--livereload` | Auto-refresh browser on changes |
| `--drafts` | Include posts in `_drafts/` folder |
| `--future` | Show posts with future dates |
| `--incremental` | Faster rebuilds (experimental) |
| `--port 4001` | Use different port |

## Stopping the Server

Press `Ctrl+C` in the terminal.

## Troubleshooting

### "Could not find gem" error
```bash
bundle install
```

### Port already in use
```bash
bundle exec jekyll serve --port 4001
```

### Permission errors on Windows
Run PowerShell as Administrator, or use WSL.

### Changes not appearing
- Hard refresh browser: `Ctrl+Shift+R`
- Check for YAML syntax errors in front matter
- Restart Jekyll server
