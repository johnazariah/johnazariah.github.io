# Saturday-to-Coauthor Series — Handoff Notes

> Working notes for the AI agent picking this back up on bare metal (where
> `~/Phd/qaoa-xorsat-research/` is reachable). Last updated 2026-05-26.

## TL;DR

- 10-post series in `posts/`. Originally drafted as 9; **post 08 was missing**
  and has now been drafted from scratch (see `posts/08-p-14-and-what-it-means.md`).
- All cross-references in posts 07/09/10 already pointed at post 08 — no
  renumbering was needed.
- Post 01 has a new italicised dedication to Stephen Jordan.
- Post 08 currently contains six `PLACEHOLDER_*` markers (real research data
  the agent in the devcontainer couldn't reach) and a "Watch this space"
  status note for an in-flight round of memory improvements to the Julia
  instrumented forward.
- The headline result in post 08 is currently asserted as
  $\tilde{c} \geq 0.8896$ at $p=14$ on MaxCut $(k=2, D=3)$, in flight on a
  128 GB Xeon — beating Goemans–Williamson's worst-case $0.8786$.

## Post 08 — Open Data Points

These are the placeholders in `posts/08-p-14-and-what-it-means.md`. All
should be sourced from `~/Phd/qaoa-xorsat-research/results/` (or wherever
the converged-angles tables live in that repo) for MaxCut, $k=2$, $D=3$.

| Placeholder | What it is | Likely source |
|---|---|---|
| `PLACEHOLDER_p11_c` | Converged $\tilde{c}$ at $p=11$ | depth-ladder summary CSV / JLD2 |
| `PLACEHOLDER_p12_c` | Converged $\tilde{c}$ at $p=12$ | same |
| `PLACEHOLDER_p13_c` | Converged $\tilde{c}$ at $p=13$ | same |
| `PLACEHOLDER_p14_wall` | Wall-clock of converged $p=14$ run on Xeon | run logs / diagnostics file |
| `PLACEHOLDER_dqi_bp` | DQI+BP value on 3-regular MaxCut | classical-comparison notes |
| `PLACEHOLDER_regev` | Regev+FGUM lower bound | classical-comparison notes |

When the in-flight Xeon $p=14$ run converges, also update:
- The "$\tilde{c} \geq 0.8896$ (in flight, not yet converged)" line in the
  numbers table to the final converged value.
- The same lower-bound phrasing in `posts/10-what-julia-taught-me-about-mathematics.md`.

## Post 08 — Memory-Improvement Story (in flight)

The first $p=14$ attempt was on the 64 GB Mac Studio. RSS climbed past
**122 GB**, macOS swap-compressed it for a while, then OOM-killed Julia
silently — no traceback. The cause is **working-set scaling at $p=14$**,
not a leak: every level of the instrumented forward retains its per-channel
$V$, $F$, and coefficient tensors, and at $p=14$ those are $4\times$ larger
than at $p=13$.

The second run was started on the 128 GB Xeon — chosen specifically because
the diagnostics module had shown this was working-set, not a leak. That
run is the source of the in-flight $\geq 0.8896$ value.

A further round of memory improvements to the instrumented forward is in
progress (user-reported). The goal: bring the $p=14$ working set back inside
64 GB so the Mac Studio can run it directly. **When that lands, update the
"Status note as of writing" block at the bottom of the Xeon-run section**
and the surrounding narrative — the headline thesis ("$p=14$ on commodity
hardware") becomes literally true rather than almost true.

The relevant edit block in `posts/08-p-14-and-what-it-means.md` is the
paragraph beginning `[**Status note as of writing:**` — currently flags
that the Xeon run is "almost true" commodity hardware. After memory work
lands, either:
- (a) rewrite that paragraph to describe the Mac Studio re-run with new
  RSS, OR
- (b) keep it and add a "**Update:**" stanza below if the Mac run
  happened post-publication.

## Claims to Sanity-Check Before Publish

These are factual claims in post 08 (and elsewhere) that should be cross-
checked against the research repo / paper sources before pushing live:

1. **$\tilde{c}_\infty \approx 0.9326$** — the infinite-depth limit for
   3-regular MaxCut. Used as the "ceiling" against which $p=14$ progress
   is measured.
2. **Goemans–Williamson SDP $\geq 0.8786$** — the worst-case approximation
   ratio. The headline verdict in post 08 ("yes, at $p=14$, on this family,
   exact finite-depth QAOA beats the GW worst-case guarantee — by at
   least 0.011 and counting") rests on the comparison $0.8896 > 0.8786$.
3. **Adjoint v1 cost ratio ~6× forward**, **v2 ~4.5× forward** — derived
   from post 07's narrative ("dropped from ~6× to ~4.5× after removing
   `_replay_branch`"). Verify against actual benchmark logs.
4. **v2 timings** of 373s / 1646s / 6653s for $p = 11, 12, 13$ — copied
   from SERIES-PLAN.md line 165. Verify against run logs.
5. **v1 timing** of 421s at $p=11$ — same source. Verify.
6. **FD would take ~100 hours** at $p=11$ — back-of-envelope. Verify or
   reframe as "easily 50+ hours, we didn't bother".

## Outstanding Tone/Voice Suggestions (Initial Review)

These were raised in the broad sanity-and-tone-check pass but **not yet
acted on** — pending user decision:

### Post 09 (`09-the-pair-programmer-that-never-sleeps.md`)
- **Two `"we're not _stealing_ from JPM"` mentions** (approx. lines 113
  and 153). The loaded word `stealing` in italics, adjacent to "JPM",
  reads more uncomfortably than the author likely intends. Suggested
  softer phrasings: "we're not _copying_", "we're not _appropriating_",
  or rephrase to "this isn't lift-and-shift — we re-derived the
  mathematics in Julia, with credit to the QOKit team throughout".
- **AI authorship framing** — somewhere near the top, an explicit
  sentence separating *paper* authorship (humans, peer review) from
  *blog* drafting (collaborative with agent). One line is enough; right
  now a careless reader could conflate the two.
- **"Neither of us is the author. Both of us are."** — quotable and
  potentially lifted out of context on social media. Consider adding one
  qualifying sentence before/after about what specifically was
  collaborative (drafting, debugging, test generation) vs what was not
  (research direction, mathematical claims, paper-level decisions).

### Post 01 (`01-the-fold-that-changed-everything.md`)
- Opening sentence is slightly long. Optional tightening — not urgent.

### Posts 07 and 09
- A couple of staccato three-word-sentence runs that read like LinkedIn
  cadence rather than John's usual conversational voice. Optional.

## What Already Got Done

These changes are committed to the working tree:

- **Post 01**: Added italicised dedication to Stephen Jordan immediately
  after the "_Part 1 of..._" subtitle, before the first `---` rule.
- **Post 08**: Created from scratch (~265 lines). Sections: opening framing,
  three attempts (FD / adjoint v1 / adjoint v2 instrumented), the Mac
  Studio crash (now corrected with the real 122 GB RSS / 64 GB story —
  earlier draft fabricated a "warm-start allocated more" speculation that
  has been removed), the Diagnostics module, the Xeon run (with status
  note about in-flight memory improvements), the numbers table, classical
  comparison, the verdict, and "What It Means" with deliberately narrow
  claims.
- **Post 10**: Updated the table row for $p=14$ from `**computing...**`
  to `**$\tilde{c} \geq 0.8896$, running on Xeon**`. Will need a second
  pass when the converged value is known.

## Publish-Time Checklist (for whoever lands this)

- [ ] Resolve all 6 `PLACEHOLDER_*` markers in post 08.
- [ ] Update post 08 status-note paragraph to reflect memory-improvement
      resolution (or keep as-is + add "Update" stanza).
- [ ] Update post 10's $p=14$ table row when converged value is known.
- [ ] Sanity-check the six numeric claims listed above.
- [ ] Decide on Post 09 softenings (3 items).
- [ ] Add Jekyll front matter to each post (none of the future-series
      drafts have it yet — see `_posts/` for the indented-YAML convention
      this blog uses).
- [ ] Create tag page `tags/from-saturday-to-coauthor/index.html` if a
      series tag is wanted (check `tags/` for existing convention).
- [ ] Run `build-tags.sh` after adding any new tags.
- [ ] Verify all `previous`/`next` series-navigation links match
      published post URLs (these are currently relative slugs; the
      build will need to resolve them).
- [ ] Preview locally before push.

## Environment Notes

- The agent that wrote this is running in a **dev container** at
  `/workspaces/johnazariah.github.io`. The host machine's
  `~/Phd/qaoa-xorsat-research/` is **not** bind-mounted into the
  container — that's why placeholders exist instead of real numbers.
- To give the agent reach to the research repo when reopening on bare
  metal: either open this repo directly in VS Code (no container), or
  add a `mounts` entry in `.devcontainer/devcontainer.json` of the form
  `source=${localEnv:HOME}/Phd/qaoa-xorsat-research,target=/host/qaoa,type=bind,readonly`
  and rebuild.

## File Map (this series only)

```
future-series/saturday-to-coauthor-series/
├── SERIES-PLAN.md           # Original 10-post plan (unchanged)
├── HANDOFF.md               # This file
└── posts/
    ├── 01-the-fold-that-changed-everything.md       # MOD: dedication added
    ├── 02-three-gradients-and-a-type-parameter.md
    ├── 03-the-walls.md
    ├── 04-1875-reasons-to-sleep-at-night.md
    ├── 05-the-algebra-that-runs-itself.md
    ├── 06-learning-from-the-masters.md
    ├── 07-the-manual-adjoint-manually.md
    ├── 08-p-14-and-what-it-means.md                 # NEW (this session)
    ├── 09-the-pair-programmer-that-never-sleeps.md  # see suggestions above
    └── 10-what-julia-taught-me-about-mathematics.md # MOD: p=14 row updated
```
