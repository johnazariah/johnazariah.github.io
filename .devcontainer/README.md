# Jekyll Blog Development Container

This devcontainer provides a complete Jekyll development environment for the blog.

## Prerequisites

- **Docker** installed (via Docker Desktop or Docker in WSL2)
- **VS Code** with Dev Containers extension (`ms-vscode-remote.remote-containers`)

## Quick Start

### Using VS Code

1. Open the blog folder in VS Code
2. Press `F1` → "Dev Containers: Reopen in Container"
3. Wait for the container to build and dependencies to install
4. Run the Jekyll server:

   ```bash
   bundle exec jekyll serve --livereload --host 0.0.0.0
   ```

5. Open <http://localhost:4000> in your browser

## What's Included

The devcontainer uses the official Microsoft Jekyll image which includes:

- **Ruby 3.x** with Bundler pre-installed
- **Jekyll** and GitHub Pages compatible gems
- **Node.js** for asset processing

### VS Code Extensions (auto-installed)

- Markdown All in One
- Markdown Preview GitHub Styles
- Markdown Lint
- Liquid template support
- F# (Ionide)
- Python
- GitHub Copilot

## Ports

| Port  | Purpose                         |
|-------|--------------------------------|
| 4000  | Jekyll server                   |
| 35729 | LiveReload (auto-refresh browser) |

## Common Commands

```bash
# Start server with live reload
bundle exec jekyll serve --livereload --host 0.0.0.0

# Build site only (no server)
bundle exec jekyll build

# Build with drafts
bundle exec jekyll serve --drafts --livereload --host 0.0.0.0

# Build for production
JEKYLL_ENV=production bundle exec jekyll build

# Update dependencies
bundle update
```

## Troubleshooting

### Port already in use

```bash
bundle exec jekyll serve --livereload --host 0.0.0.0 --port 4001
```

### Gems out of date

```bash
bundle update github-pages
```

### Changes not appearing

- Hard refresh browser: `Ctrl+Shift+R`
- Check for YAML syntax errors in front matter
- Restart Jekyll server

### Container won't start

Try rebuilding without cache:
`F1` → "Dev Containers: Rebuild Container Without Cache"
