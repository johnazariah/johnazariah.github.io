# From Saturday to Co-Author Handoff

Use this prompt to resume the blog publishing/dependency cleanup work from another machine or a fresh VS Code session.

```text
We are in /Users/johnaz/johnazariah.github.io.

Current known checkpoint:
- HEAD/main/origin/main should include commit 768e398: publish(series): schedule from-saturday-to-coauthor rollout
- This commit published the From Saturday to Co-Author series as dated posts:
  - 2026-05-29 post 1
  - then Monday/Thursday cadence through 2026-06-29 post 10
- It removed `future: true` from _config.yml and set explicit timezone/scheduled rebuild behavior.
- It added a daily GitHub Actions workflow to rebuild Pages so future-dated posts become active automatically.
- It rewrote intra-series links from /tags/from-saturday-to-coauthor/ to direct dated post URLs.
- It removed screenshot placeholder/scaffold language and made Posts 9/10 text-only quotes/email framing.
- It fixed Post 2 YAML front matter by quoting the summary containing a colon.
- Docker/containerized Jekyll build had succeeded before the crash loop.
- Focused validation had indicated:
  - today’s Post 1 generated in _site
  - future Post 2 was held back
  - no screenshot placeholders remained
  - no tag-page cross-links remained in published series posts

Important operational note:
- VS Code / terminal output was unstable and repeatedly crashed or returned exit 130 during heavy validation.
- Avoid heavy Docker validation unless needed. Start with lightweight commands.

Next steps:
1. Run a lightweight `git status --short` to confirm the tree is clean.
2. If clean, do not make another checkpoint commit.
3. Address Dependabot alerts next.
4. Prefer GitHub CLI/API or inspect Gemfile/Gemfile.lock locally, but avoid long-running builds until necessary.
5. If dependency changes are made, validate with the smallest possible build command, ideally once.
6. Commit and push dependency fixes directly to main; John said no PRs for the blog.

Useful facts:
- The user wants direct-to-main publishing for this blog, not PRs.
- The first series post should be live on 2026-05-29.
- Future-dated posts rely on daily Pages rebuilds plus Jekyll future-post exclusion.
- The series source drafts remain under future-series/saturday-to-coauthor-series/posts/.
- Published copies are under _posts/ with filenames `YYYY-MM-DD-saturday-to-coauthor-NN-<title>.md`.
```
