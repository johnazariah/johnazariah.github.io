# Publish Blog Changes

Deploy changes to the live blog on GitHub Pages.

## Pre-Publish Checklist

Before publishing, verify:

1. **Post Front Matter**
   - [ ] `layout: post` is set
   - [ ] `title` is present and properly quoted
   - [ ] `tags` array is defined
   - [ ] `author: johnazariah` is set
   - [ ] `summary` provides a good description

2. **Content Quality**
   - [ ] Spelling and grammar checked
   - [ ] Code blocks have proper syntax highlighting (use ```language)
   - [ ] Images are in `assets/images/YYYY-MM-DD/` folder
   - [ ] All links work

3. **Tags**
   - [ ] All tags used have corresponding pages in `tags/` folder
   - [ ] Run `build-tags.sh` if new tags were added

## Local Preview (Optional)

Test locally before publishing:

```bash
bundle install          # First time only
bundle exec jekyll serve
```

Then visit `http://localhost:4000`

## Publish Steps

1. **Stage changes**:
   ```bash
   git add .
   ```

2. **Review what's being committed**:
   ```bash
   git status
   git diff --staged
   ```

3. **Commit with descriptive message**:
   ```bash
   git commit -m "Add post: Your Post Title"
   ```

4. **Push to GitHub**:
   ```bash
   git push origin main
   ```

5. **Verify deployment**:
   - GitHub Actions will build the site automatically
   - Check https://johnazariah.github.io for the live site
   - Build status visible at: https://github.com/johnazariah/johnazariah.github.io/actions

## Troubleshooting

- **Build failed**: Check GitHub Actions for error details
- **Post not showing**: Verify date isn't in the future (unless `future: true` in `_config.yml`)
- **Styles broken**: Clear browser cache or check CSS paths
- **Tags not working**: Ensure tag pages exist in `tags/` folder
