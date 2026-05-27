# FACTS

Single source of truth for the rewrite. All numbers and dates here are
verified against the qaoa-xorsat repo journal, results CSVs, or John's
direct correction (`✦` marks the latter — these override the journal where
they disagree).

---

## Dedication

**The series is dedicated to Stephen Jordan.**

This is the lens that resolves every editorial question.

### Quoting Stephen: the guard rule

The email evidence is unusually rich and tempting to quote. **Do not
quote Stephen extensively, and never quote him on any relative
comparison of John's contribution to the JPMorganChase team's
contribution.** Specifically:

- The Sat 28 Mar quote ("Your results are in some ways more helpful
  to me because you have shared your code and data...") is for FACTS
  only. It does not appear in any post. Stephen's private
  encouragement to John must not be read as Stephen disparaging the
  JPM authors he is also co-authoring with.
- Where the substance of that quote is needed (open code, cross-
  checks against baselines, two-groups-agreeing framing), paraphrase
  in author's voice. Do not attribute to Stephen.
- The Wed 25 Mar 10:41 "independent confirmation" line is safe to
  quote: it frames cross-validation as collegial, not competitive.
- The Wed 25 Mar 01:51 scoop email is safe to quote: it celebrates
  JPM if anything, since it is Stephen explaining their result to
  John.
- The Sat 21 Mar problem statement is safe to quote: it is the
  setup.
- Default rule: when in doubt, paraphrase. JPM co-authors will read
  this series too.

### The operative reframe

**Not** "look how smart I was."

**Instead**: *"This is a great mentor and encourager, and this is how
we showed our respect to him by giving this problem everything we got."*

The "we" is deliberate. It includes John, his AI collaborators (Gemini,
Claude, Copilot), the tools (Julia, the Mac Studio, the JPM-derived
QOKit code), and the people he learnt from along the way. The series
is *the receipt* for what a stranger and his toolbox did with the
opening Stephen gave them. It is not a personal-achievement narrative.

### Implications

- The interpersonal arc is told to **acknowledge Stephen's generosity
  and encouragement** of a stranger who walked into his field cold.
  It is not told to dramatise John's perseverance.
- Where the two readings collide \u2014 \"John kept working after being
  scooped\" vs \"Stephen made room for an outsider's results in a
  paper that didn't need them\" \u2014 the second reading wins. Every time.
- The technical posts (2\u20138) are framed as *what we built with the
  problem Stephen gave us*, not *what I figured out*.
- The AI-collaboration post (Post 9) lands as part of the \"we\" \u2014
  another set of collaborators that helped do justice to the problem,
  disclosed to Stephen on Day 2.
- Stephen reviewing the series for accuracy is also Stephen receiving
  the dedication. The prose has to read well to him.

## The two stories, told in parallel

The series carries two arcs in tandem and the prose should keep them
distinguishable:

1. **The technical story.** Saturday email → catamorphism → WHT →
   manual adjoint → plateau detection → Double64 → charge
   decomposition → p=14 on a Mac. This is the project-report spine.
   Posts 2–8 are mostly this story.
2. **The trust-building story.** A stranger emails on a Saturday →
   the stranger sends LLM study notes on Monday → the stranger
   ships preliminary numbers on Tuesday → the scoop lands on
   Wednesday → Stephen's response on Wednesday and Saturday makes
   room for the stranger anyway. Posts 1, 9, and 10 carry most of this
   story; the rest of the series threads it lightly.

**Both arcs share the same evidence base (emails, journal, code) but
have different protagonists.** The technical arc's protagonist is the
problem. The interpersonal arc's protagonist is **Stephen**, not John.
This is what makes the dedication work without becoming maudlin.

---

## The two phases

| Phase | Dates | What | Output |
|-------|-------|------|--------|
| 1. XORSAT campaign | 21 Mar 2026 – 27 Apr 2026 | The "Saturday email to arXiv submission" arc, including the Wed 25 Mar scoop and the Sat 28 Mar co-author invitation. Stephen's original target was "the week of April 13"; actual submission was 27 Apr. | arXiv paper submitted **27 Apr 2026** ([arXiv:2604.24633](https://arxiv.org/pdf/2604.24633)). Stephen Jordan is the lead author; the paper has twelve authors in total, John being one of the eleven who joined the lead. **Do not phrase this as "John was co-author with Stephen and the JPM team"** — that overstates John's position in the author list. The accurate framing is: "John was one of the twelve authors." |
| 2. MaxCut follow-on | 27 Apr 2026 – present | Pushing depth on a single (k=2, D=3) for a second paper | p=14 D=3 done on Mac, p=14 D=4 done on Mac, p=14 D=5 in flight |

The blog series tells **both** phases. The "co-author" milestone belongs
to Phase 1. The "p=14 on a Mac" milestone belongs to Phase 2.

---

## The plot twist (Phase 1's actual narrative shape)

The series was originally pitched as "Saturday email → co-author in eight
weeks." The emails reveal a tighter, more dramatic arc:

1. **Sat 21 Mar** — the ask lands. John doesn't know what QAOA is.
2. **Sun–Tue 22–24 Mar** — four-day sprint. Cata recognition Sunday;
   WHT Monday; first numbers Tuesday morning; p=9 (3,4) Tuesday
   afternoon, already past the prior published state of the art.
3. **Tue 24 Mar evening** — John commits the validation methodology in
   an email (independent random-seed re-optimisation, not
   warm-starting from Farhi's angles), shares the repo, asks Stephen
   directly whether they can disclose to arXiv.
4. **Wed 25 Mar 01:51** — the scoop. JPMorganChase has been running
   the same calculation on a cluster, up to p=17. Stephen tells John
   it's over.
5. **Wed 25 Mar 10:32** — John's reply: *Hehehehe. I got p=11 on my
   Mac. There may be something interesting in my approach as well.*
6. **Wed 25 Mar 10:41 → 11:49** — Stephen's tone shifts from "sorry"
   to **"Very convincing! Now run the experiments."** The methodology
   wins the respect; the headline number doesn't have to.
7. **Fri 27 Mar** — John sends a whacked-together draft paper with
   the table of numbers.
8. **Sat 28 Mar** — Stephen invites John as co-author, **on the
   merits of shared code, shared data, and sanity-checks against
   Eddie's published numbers** — not on having pushed p further than
   JPM. (JPM is in the paper too, on their own merits.)

What this means for the series:

- **The contribution is not "first to p=N."** JPM got there first. The
  contribution is *independent verification on commodity hardware,
  with open code and a documented methodology*. Post 10 has to land
  this, and Post 1 has to set it up.
- **Post 1's arc is the sprint + scoop + comeback**, not just "the
  email landed and eight weeks later I was a co-author."
- **Post 7 ("Learning from the Masters") is literally about
  cross-validation against the JPM team's code (QOKit)** — the
  charge-decomposition study reads, in the emails' light, as the
  natural follow-on to Stephen's "join forces, not race" advice.
- **Post 9 (AI as collaborator) has documentary evidence from Day 2**:
  John told Stephen *on Monday morning* that Gemini and Claude were
  how he got up to speed. The disclosure is original, not retrofitted.

---

## The email that started it

- **Date**: Saturday 21 March 2026, 03:47
- **From**: Stephen Jordan <stephenjordan@google.com>
- **Subject thread**: Re: FW: MOD-84867 arXiv – important notification regarding submit/7372396
- **Problem statement (verbatim)**: "numerically calculating the fraction
  of constraints that can be satisfied by QAOA on D-regular max-k-XORSAT",
  particularly **k=3, D=4**.
- **Why exact, not asymptotic**: existing methods (arXiv:2110.14206)
  are only accurate to corrections of order $1/D$. Stephen needed
  precise small-$(k,D)$ numbers to compare QAOA against DQI.
- **Method hint**: light-cone of local observables conjugated by
  limited-depth QAOA. References: Farhi 2014 (arXiv:1411.4028) and a
  2025 paper (arXiv:2503.12789).
- **The ask, verbatim**: "if one writes high performance code or finds
  it on some existing github repository and then runs it on a big
  cluster computer, it should be possible to push the direct method
  out to some reasonable p".

### The baseline table Stephen sent (the QAOA column was missing)

| (k,D) | Prange | SA | DQI+BP | Regev+FGUM |
|-------|--------|--------|--------|-----------|
| (3,4) | 0.875 | 0.9366 | 0.87065 | 0.89187 |
| (3,5) | 0.8 | 0.9005 | 0.81648 | 0.83607 |
| (3,6) | 0.75 | 0.8712 | 0.77562 | 0.78361 |
| (3,7) | 0.71428 | 0.8492 | 0.74727 | 0.76024 |
| (3,8) | 0.6875 | 0.8287 | 0.72351 | 0.72943 |
| (4,5) | 0.9 | 0.9279 | 0.8597 | 0.92158 |

(Table continues past (4,5) in the original email; truncated here at the
edge of the screenshot.) The XORSAT campaign's job was to fill the
missing **QAOA** column with exact numbers at the highest $p$ we could
reach. Phase 1 produced QAOA values that beat DQI+BP on 13 of 15 pairs.

### Screenshot use across the series

Stephen has been asked to review the series for correctness and
applicability. Permission-in-progress to include **cropped screenshots**
of the following emails. Universal constraints:

- **Do not** show From / To / Subject headers, email addresses, or the
  message metadata bar.
- Crop to the body text only.
- Captions use prose-style dates (e.g. "Stephen's email, 21 March 2026").
- Files live under `assets/images/<post-date>-from-saturday-to-coauthor-NN/`.
- Source: VS Code chat image cache (`~/Library/Application
  Support/Code/User/workspaceStorage/vscode-chat-images/`). Crop at
  drafting time, not before.

### Placement policy

- **Post 1 carries no email images.** The trailer is prose-only. Any
  quoted phrases from the emails go inline in body text or as
  blockquotes, with a sourced date. This keeps the post fast and
  avoids the "look at all these emails" effect that the sterile-tone
  rule is meant to prevent.
- **Post 10 carries the bookends**: image 1 (Stephen's Saturday 21 Mar
  ask) at the top, image 7 (Stephen's Saturday 28 Mar co-author
  offer) near the close. Two Saturdays one week apart, in the
  collaborator's own words. The body of Post 10 reflects between
  them.
- **Post 9 carries the AI-credit evidence** (2a + 2b), because the
  whole post is about that.
- **Post 5 or Post 7 carries the validation methodology** (image 5),
  wherever "independent confirmation" lands in the technical arc.
- **Images 3, 4, 6 (scoop / comeback / "Very convincing")** are
  *optional supporting evidence* for Post 10, used only if Post 10
  needs the arc shown rather than told. Default: hold them in reserve
  and let the bookends do the work alone. Decide at draft time.

### Asset table

| # | Email | Crop | Used in |
|---|-------|------|---------|
| 1 | Sat 21 Mar 03:47 Stephen's ask | "The problem I have in mind..." through the bottom of the visible baseline table | **Post 10 opening bookend** |
| 2a | Mon 23 Mar 08:17 John's first reply | Crop body only: "Hi Stephen" through "Let me know what works best for you" and sign-off. **Specifically include the line "Gemini and Claude did a great job in explaining QAOA to me..."** — this is the Day-2 AI-credit evidence. Strip the "Get Outlook for Mac" footer. | Post 9 (AI-credit-from-Day-2 evidence) |
| 2b | Mon 23 Mar 10:03 John's LLM-notes attachment | Crop to show: the `study-notes-for-stephen` attachment chip (197.2 KB) at the top, then the body "Here are some study notes from my LLM sessions where I tried to fully understand and spec out the problem...I have a lot of maths to learn." Strip the "Get Outlook for Mac" footer. | Post 9 (companion to 2a) |
| 3 | Wed 25 Mar 01:51 Stephen's scoop | "I just got off a videoconference..." through "...you have been scooped. Sorry about that." | Post 10 (optional supporting evidence; default reserve) |
| 4 | Wed 25 Mar 10:32 John's comeback | "Hehehehe / I got it running p=11 on my Mac Studio. There may be something interesting in my approach as well" | Post 10 (optional; pairs with #3; default reserve) |
| 5 | Tue 24 Mar 18:17 John's validation argument | "We re-optimized from scratch..." through "...3+ decimal places is a stronger validation than reproducing his angles would be." | Post 5 (tests) or Post 7 (cross-validation), wherever "independent confirmation" lands |
| 6 | Wed 25 Mar 11:49 Stephen's "Very convincing!" | Full body of the 11:49 email, from "Very convincing!" through "...cluster computer that you could use for this." | Post 10 (optional supporting evidence; default reserve) |
| 7 | Sat 28 Mar 08:06 Stephen's co-author offer | "I think the most likely course of action is to weave your numbers into the attached paper and include you as a coauthor..." through "...are very reassuring." | **Post 10 closing bookend** |

---

## Phase 1 timeline

| Date | Milestone | Source |
|------|-----------|--------|
| Sat 21 Mar | Project inception. Stephen's email lands at 03:47. John doesn't know what QAOA is. Spends the day reading the three referenced papers (arXiv:2110.14206, arXiv:1411.4028, arXiv:2503.12789), getting the maths explained (LLM sessions), and recognising the branch-tensor recurrence as a **catamorphism over the light-cone tree**. ✦ | Entry 1, email, John |
| Sun 22 Mar | Continues paper-reading and spec work. First Julia code, structured as the cata. | Entries 2, 11–16, email |
| Mon 23 Mar 08:17 | John replies to Stephen: "So I think I've made a crack at understanding the problem and coming up with some intuition around what needs to be done!" Same email gives explicit credit on Day 2: **"Gemini and Claude did a great job in explaining QAOA to me and getting the kind of intuition that I needed for how it works."** ✦ Worth quoting in Post 9; the AI-credit was on the table from the start, not retrofitted. | email 2 |
| Mon 23 Mar 10:03 | John sends "study notes from my LLM sessions where I tried to fully understand and spec out the problem...I have a lot of maths to learn." Attached `study-notes-for-stephen.pdf` (197.2 KB). Documentary evidence for Post 9: AI involved from Day 2, openly disclosed to the collaborator. | email 2 |
| Mon 23 Mar | Tree + tensor primitives; Basso explainer audited; refactoring the cata reveals XOR-convolution structure; WHT diagonalises it. | Entries 17, 18, innovation summary §1 |
| Tue 24 Mar 06:28 | John to Stephen: "I got a preliminary implementation done... DQI is better than QAOA at p=5 for (k=3, D=4) — I'm still working out some loop optimizations and then maybe I can start scaling up p to get some results." Attached: `study-notes-for-stephen` (124.8 KB). | email 2 |
| Tue 24 Mar | Manual adjoint implemented (Entry 19); **p=9 for k=3, D=4, c̃ = 0.8613, wall ≈ 56 min** on M4 Mac Studio. Already past the published p=5 state of the art. ✦ | Entry 19 |
| Tue 24 Mar 17:33 | John to Stephen: "PS — I am expecting it to beat DQI at P=11 ... the difference is 0.003 at P=10." | email 3 |
| Tue 24 Mar 18:17 | John explains the validation methodology: **"We re-optimized from scratch — independent L-BFGS multistart optimization with random initial angles and warm-starting between depths. We did not use Farhi's published optimal angles as seeds. I thought this would be better for confidence... it could be argued that the fact that our independently optimized values match his to 3+ decimal places is a stronger validation than reproducing his angles would be."** ✦ This is the line that turns the work from "a re-run" into "independent confirmation." | email 4 |
| Tue 24 Mar 18:51 | John: **"I've invited you to collaborate on the repo. Do you believe there's nothing we can disclose to arXiv from what we have right now? The p=10 result is genuine..."** Also: "I was whacking together an Azure Batch subscription for this - might cost me a few hundred bucks." Pushing for disclosure; sharing the code. ✦ | email 4 |
| Tue 24 Mar overnight | Pushes to p=10, p=11 on Mac Studio. | inferred from email + Entry 19 |
| **Wed 25 Mar 01:51** | **Stephen drops the scoop bombshell**: "I just got off a videoconference with some researchers at JPMorganChase. Last week at APS March meeting I mentioned to one of them that I was interested in this question of exact analysis of QAOA at specific small k and D. I didn't think anything would come of it. But it turns out they have run a big computation on their cluster computer over the last few days that goes up to p=17 or so. **So it looks like you have been scooped.** Sorry about that. Now that you are up to speed on QAOA and lightcone methods I will think a little bit about other questions you can address." ✦ | email 3 |
| **Wed 25 Mar 10:32** | **John's reply**: "Hehehehe / I got it running p=11 on my Mac Studio. **There may be something interesting in my approach as well** ... have they published? I can get you a draft this afternoon." ✦ | email 3 |
| Wed 25 Mar 10:41 | Stephen: "Let me know what you find. Among other things, **it is valuable to get independent confirmation to make sure the results are correct**." This sentence — "independent confirmation" — is the seed of John's contribution to the eventual paper. ✦ | email 3 |
| **Wed 25 Mar 11:49** | **Stephen's tone shifts after seeing the methodology**: "Very convincing! Now that you have built the apparatus, the next step is to run the experiments. The key questions are whether QAOA can be shown to beat simulated annealing and/or Regev+FGUM at some k and D. To find out one must sweep over various k and D and also push p as far as possible and optimize the angles as highly as possible. Probably UTS has a cluster computer that you could use for this." ✦ This is the moment the conversation pivots from "sorry you got scooped" to "now run the experiments." | email 4 |
| Wed 25 Mar 11:56 | Stephen (separate email): "I'll give some thought to strategy and get back to you. Your results are good (and remarkably rapid) but in my opinion it is better to build one higher-impact paper than to publish a stream of smaller incremental results. Also, **it might be a better idea to join forces with the JPMorganChase team than to race against them**." The race ends; the collaboration begins. ✦ | email 4 |
| Wed 26 Mar | Metal GPU spike — abandoned. | Entry 20 |
| Fri–Tue 27–31 Mar | Plateau detection iterated four times; Mac pushed to p=12 for (3,4), c̃ = 0.8769. | Entry 21 |
| **Fri 27 Mar 14:24** | John sends draft paper + draft numbers: "Here are some draft numbers for the big table. I'm still running p=9-11 for some of them. I have prepared the containers to run on Azure when my allocation resets tomorrow - I'll fire up a batch job to get to 13/14 because the work is all memory constrained now. But the values already look promising - QAOA seems to have beaten Regev in a couple places too. Also - in my haste to get something to you **I've whacked the paper together** - please don't read anything into the order of the authors or anything...I'm extremely open to any and all suggestions you have." ✦ | email 5 |
| **Sat 28 Mar 08:06** | **The co-author invitation.** Stephen: "I think the most likely course of action is to **weave your numbers into the attached paper and include you as a coauthor**. Over the past week or so, two people from JPMorganChase have done similar calculations to yours. I would likely add them too. In the table I would put whichever QAOA scores are the highest. It is possible that they have pushed p a little bit further than you since they have a big cluster computer to run on. But **your results are in some ways more helpful to me because you have shared your code and data and documented what you have done. Also, the sanity checks against Eddie's published numbers are very reassuring**. ... Probably we will try to post the manuscript to the arxiv during the week of April 13." Attached: `Locally_Quantum_D...` (438 KB) — the in-progress manuscript. ✦ | email 5 |
| Wed 1 Apr | SLURM scripts written. First overflow diagnosis ($\tilde c = 21.44$ at (7,8)). The bug was invisible on the Mac because local arities never grew large enough to overflow `Float64`; the first SLURM run on Stephen's cluster, pushed to higher arities, returned nonsense. Stephen reported the failure back bemused. The missing-bounds-check failure mode was the proximate cause of propagation: the optimiser's `argmax` treated $\tilde c = 21.44$ as a candidate truth and warm-started from it. ✦ | Entries 22, 23 |
| Thu 2 Apr | Normalised evaluator (first attempt: always-normalise → signal crush). Azure fleet results merged. | Entries 24, 25 |
| Fri 4 Apr | Threshold normalisation lands (the $10^{30}$ rule). | Entry 26 |
| Sat–Sun 5–6 Apr | Memetic/swarm optimiser implemented; (7,8) goes from failing at p=3 to c̃ = 0.789 at p=8. | Entry 27 |
| Mon 7 Apr | First Azure cloud attempt: didn't scale for our workload. ✦ Pivoted to Stephen's SLURM cluster. p=13 results land: (3,4) p=13 = 0.8807, (3,5) p=13 = 0.8429. | Entry 28 |
| Mon–Tue 6–7 Apr | Dual-Xeon P710 added; Azure fleet decommissioned ($50 total spend). | Entry 29 |
| Tue 8 Apr | Warm-start path bug fixed; swarm deployed on Stephen's cluster. | Entry 30 |
| Fri 10 Apr | Double64 fix lands. (6,7) p=10 goes from broken 3.23 to valid 0.813. | Entry 31 |
| Fri–Sat 1–2 May | CUDA backend + streaming gradient checkpointing. The GPU model exists; no suitably grunty hardware to run it on. | Entry 32 |
| **Mon 27 Apr** | **Paper submitted to arXiv.** ✦ End of Phase 1. | John |

---

## Phase 2 timeline (MaxCut k=2 D=3 push)

| Date | Milestone | Source |
|------|-----------|--------|
| Late Apr – mid-May | MaxCut sweep on Mac at p=11..13 | `results/maxcut-k2-d3-sweep.csv` |
| **Sun 3 May 05:44** | **Verifier handoff to the joint repo.** John pushes verifier code to `github.com/stephenjordan/fgum/pull/1`, updates each folder's README, runs the verifier to p=14 on the Mac with $\epsilon \leq 10^{-12}$ against angles supplied by the JPMC team. Email cc's the full co-author list (Khan, Shaydulin, Boulebnane, Mandal, Shutty, Rubin, Chailloux, Buzet, Ragavan). Abid Khan's repo referenced by URL, not vendored. ✦ | email, Sun 3 May 2026 |
| Tue 26 May – Wed 27 May | p=14 D=3 memory stabilisation: five fixes in `src/charge_manual_adjoint.jl` (peak RSS drops from ~122 GB projected to 55.65 GB measured). Mac Studio completes the run. | Entry 33 |
| **26 May – 28 May** | **p=14 D=3..7 complete on the Mac**, warm-started from saved p=13. All five rows converged. Campaign ongoing at D ≥ 8. See result block below. ✦ | `results/maxcut-k2-p14-timing.csv` |

---

## p=14 result block (the headline of Post 8 and the Phase 2 table in Post 10)

D=3..7 are converged at p=14. D=8 and D=9 are shown at their current best depth (p=12 from the sweep); both are still being pushed towards p=14 on the Mac. Hardware: Mac Studio M4 (64 GB unified memory), Julia 1.12.5, `autodiff = :charge_adjoint`, warm-started from the previous p. Sources: `results/maxcut-k2-p14-timing.csv` for D=3..7 at p=14; `results/maxcut-k2-d8-sweep.csv` and `results/maxcut-k2-d9-sweep.csv` for D=8, D=9 at p=12.

| $D$ | max $p$ | $\tilde c$         | wall (s) | wall (h) | iters | evals | peak RSS (GB) | DQI bound | gap     |
|-----|---------|--------------------|----------|----------|-------|-------|---------------|-----------|---------|
| 3   | 14      | 0.891384992947     | 32 343.4 | 8.98     | 58    | 172   | 55.65         | 0.85355   | +0.038  |
| 4   | 14      | 0.831514625400     | 18 957.9 | 5.27     | 35    | 100   | 56.51         | 0.78868   | +0.043  |
| 5   | 14      | 0.801254018370     | 22 500.6 | 6.25     | 42    | 120   | 56.39         | 0.75000   | +0.051  |
| 6   | 14      | 0.771627233507     | 17 859.0 | 4.96     | 33    | 95    | 54.95         | 0.72361   | +0.048  |
| 7   | 14      | 0.752436836526     | 20 852.3 | 5.79     | 38    | 111   | 55.05         | 0.70412   | +0.048  |
| 8   | 12      | 0.731075836176     | 26 498.3 | 7.36     | —     | —     | —             | 0.68898   | +0.042  |
| 9   | 12      | 0.717763513540     |  9 711.1 | 2.70     | —     | —     | —             | 0.67678   | +0.041  |

Total wall clock for the five p=14 rows: ~31.25 hours. Branch: `feature/charge-adjoint-memory-fix`. Key commit: `74cf598`. D=8 and D=9 are queued/in flight at higher p; update those rows on completion. The sweep CSVs do not record RSS or iter/eval counts directly (those columns are `—`); fill in from the p=14 timing CSV when those runs converge.

Classical benchmarks for $k=2$ $D$-regular MaxCut:
- Goemans–Williamson worst case: $\approx 0.8786$ (any graph, worst-case guarantee).
- DQI explicit upper bound: $\tfrac{1}{2} + \tfrac{1}{2\sqrt{D-1}}$; values above as the "DQI bound" column.
- Infinite-depth ceiling for 3-regular MaxCut: $\approx 0.9326$ (depth-14 at D=3 is $\approx 0.041$ below).

All five p=14 values clear the DQI explicit bound at their $D$ by 0.04–0.05; only D=3 additionally clears the GW worst-case guarantee.

---

## The five memory fixes (Entry 33 verbatim)

Two buckets, used to organise Post 8:

**Time for space (drop, replay):**
1. `_bwd_root!` removed `fhist` (~60 GB peak contributor); replaced with
   on-demand forward replay per backward step.
2. `_bwd_branch!` Phase 1 removed `p1s` history (~13 GB transient);
   replaced with replay from seed via two ping-pong buffers.
3. `charge_expectation_and_gradient`: eager cache freeing — drops
   consumed `cache.children[lv]`, `cache.states[lv]`,
   `cache.F_levels[lv-1]` immediately.

**Space for time (precompute, alias):**
4. `_bwd_root!` buffer strategy cleanup: precomputed `w_final` once;
   hoisted large temporaries outside the loop; explicit ping-pong for
   factor adjoints.
5. `_charge_branch_instrumented`: m==1 alias optimisation
   (`t_normalized = F_powered`); avoids one full extra vector per level.

---

## Cluster setup (corrected)

The blog should describe Phase 1's production compute as:

- **15-node SLURM cluster** (Stephen's, at Google Quantum AI) ✦
- **Dual Xeon P710 workstation** (John's, 128 GB, 32 cores)
- **Mac Studio M4** (John's, 64 GB, 12 cores)

The Azure attempt is a *story beat*, not part of the production setup:
"we tried Azure first, it didn't scale, we moved to Stephen's SLURM
cluster." Total Azure spend was $50.

---

## Joint-paper collaborators (verified from 3 May 2026 email thread) ✦

The paper [arXiv:2604.24633](https://arxiv.org/pdf/2604.24633) has
twelve authors. Stephen Jordan is the lead. John is one of the
eleven who joined. The other collaborators, as visible on the 3 May
2026 thread (`Re: [EXTERNAL]Re: arXiv New submission -> 2604.24633`),
are:

| Name | Affiliation | Email (institutional) | Notes |
|------|-------------|-----------------------|-------|
| Stephen Jordan | Google Quantum AI | `stephenjordan@google.com` | Lead author; the dedicatee of this series |
| Abid A Khan | JPMorganChase | `abid.a.khan@jpmchase.com` | Provided a verifier repo John referenced (see Phase 2 timeline, Sun 3 May) |
| Ruslan Shaydulin | JPMorganChase | `ruslan.shaydulin@jpmchase.com` | Also a QOKit contributor (`rsln-s` on GitHub) |
| Sami Boulebnane | JPMorganChase | `sami.boulebnane@jpmchase.com` | |
| Noah Shutty | Google | `shutty@google.com` | |
| Nicholas Rubin | Google | `nickrubin@google.com` | |
| Andre Chailloux | INRIA | `andre.chailloux@inria.fr` | |
| Quentin Buzet | INRIA | `quentin.buzet@inria.fr` | |
| Seyoon Ragavan | MIT | `sragavan@mit.edu` | |
| Avijit Mandal | Duke / independent | `avijit.mandal@duke.edu`, `avijitbesu1995@gmail.com` | Listed under both addresses on the thread |

(One additional co-author is on the arXiv submission but not visible on
this particular thread; total = 12.)

### Attribution rules across the series

- **The JPM team** — Abid Khan, Ruslan Shaydulin and Sami Boulebnane on
  the joint paper — is responsible for the charge decomposition (the
  $\mathbb{Z}_2 \times \mathbb{Z}_2$ representation-theory observation
  that strips a factor of $p$ from the forward cost). They derived it,
  they published it, and they ship it as part of **QOKit**, their
  open-source toolkit at `github.com/jpmorganchase/QOKit` (JPMorganChase
  Global Technology Applied Research). For the series, treat QOKit as
  the *open-source artefact* of the JPM team's contribution to the
  joint paper, not as a separate team. When the series refers to the
  contribution, name the JPM team (with QOKit as the code via which
  it was learnable).
- **The JPM co-authors on John's joint paper** are exactly Khan,
  Shaydulin, and Boulebnane. Where the series thanks the JPM team,
  these are the names. Ruslan Shaydulin is also a top QOKit
  contributor (`rsln-s` on GitHub); Danylo Lykov is the other major
  QOKit name on the SC-W 2023 simulator paper but is not on the joint
  paper as a co-author.
- **The non-JPMC collaborators** (Shutty, Rubin, Chailloux, Buzet,
  Ragavan, Mandal) are not named individually in the series body but
  are acknowledged collectively ("the rest of the eventual co-authors")
  in Post 10's thank-yous.
- **Stephen alone gets the dedication.** This is the whole point.

### Phase 2 collaboration evidence (3 May 2026 email) ✦

On Sun 3 May 2026 05:44 AM John emailed Stephen and Abid Khan (cc'ing
the full co-author list above):

> I've put in the verifier code and updated the README's for each of
> the folders. I have run the verifier to p=14 on my little Mac and
> have epsilon <= 1E-12. Thank you for the angles ☺
>
> https://github.com/stephenjordan/fgum/pull/1
>
> I have referenced the repo kindly provided by
> @abid.a.khan@jpmchase.com but have not included it into this repo.

What this email establishes for the series:
- Phase 2 collaboration is **active and bidirectional**. JPMC supplied
  angles; John supplied independent verification on commodity hardware.
- The verifier work is on `github.com/stephenjordan/fgum/pull/1`. The
  verifier ran to p=14 on the Mac with $\epsilon \leq 10^{-12}$.
- Abid Khan's repo is referenced by URL but not vendored; that's the
  honest collaboration model (Phase 1's was the same).
- The smiley face is John's; the email is informal. The collaboration
  is friendly.

**Things the journal got wrong** and need flagging if anyone re-reads it:
- Entry 28 and the Algorithmic Innovation Summary both say "50-node
  SLURM cluster" / "50 × 2.7 TB". The real number is 15.
- The various Azure-fleet beats in Entries 22–29 are real but
  scaffolding; they're not the cluster the paper's results ran on.

---

## Performance numbers we can defend

These are the only quantitative claims to use until verified or replaced:

- **Catamorphism recognition** (Sat 21 Mar): the branch-tensor
  recurrence is a fold over the light-cone tree, parametrised by an
  algebra. This is the *first* algorithmic insight — it precedes the
  WHT, makes the WHT visible, and is the seed of the CostAlgebra
  (Post 6, tagless final) and the "language shapes theorems" thesis
  (Post 10). Worth surfacing in Post 2's opening and again in Post 10.

- **WHT factorisation** (Mar 22–23): $O(4^{3p}) \to O(p^2 \cdot 4^p)$.
  At k=3, p=8: **the maths said 65 000× improvement, the wall clock
  said 11 minutes** (down from "never completes"). Both numbers are
  the right answer to different questions; quote both together.
- **ForwardDiff via type parameter** (Mar 24): finite differences
  can't converge at p≥4 due to gradient noise. ForwardDiff is 31×
  faster than FD at p=5 *and* the only method that converges.
- **Manual Basso adjoint** (Mar 24): 1.6× a single forward
  evaluation, independent of p. At p=8, 12× faster than ForwardDiff.
- **Plateau detection** (Mar 27–31): cut p=12 (3,4) from 2+ hours
  to ~40 minutes.
- **Charge decomposition** (post-paper study of QOKit code): strips
  a factor of $p$ from the forward cost. Per the old Post 6 table,
  measured speedups were 15× at p=8, 26× at p=10, 78× at p=12 —
  *but* re-check these against actual benchmarks before reusing.
- **Charge manual adjoint**: 4.5× a single forward evaluation.
- **Test harness**: 22 test files, 1875 assertions.

---

## Things NOT to claim (the previous draft's fabrications)

- The "v1 vs v2" adjoint timing comparison at p=9..13 (373 s / 1646 s /
  6653 s, etc.) — these numbers do not appear anywhere in the repo.
- "122 GB RSS overnight Mac crash" — the real first failure was
  jetsam at lunch during the first adjoint gradient, not an overnight
  OOM at 122 GB.
- "≥0.8896 in flight on Xeon" as the p=14 headline — never happened.
  The Xeon was not used for p=14. The Mac produced the real number.
- Any speedup table at the end of Post 10 that contains a p=14 row
  citing the Xeon.
- **"QOKit published by Kunal Marwaha, Jonathan Wurtz and Ruslan
  Lykov at JPMorganChase"** — wrong on all three names.
  Marwaha is a real QAOA researcher but not a QOKit author; Wurtz is
  at QuEra, not JPMC; "Ruslan" is Shaydulin's first name, not
  Lykov's (Lykov's first name is Danylo). This attribution appeared
  in early drafts and in `SERIES-PLAN.md`'s Post-7 outline; it is
  wrong and was removed from Posts 7 and 10 on 27 May 2026. Use the
  Joint-paper collaborators table above for any individual
  attribution.

---

## Writing constraints

From John's preferences:
- No emojis.
- No em-dashes (—). Use semicolons, colons, commas, or restructure.
- En-dashes (–) for number ranges are fine.
- Matter-of-fact tone; no apologetic hedging.
- F# / functional-programming perspective is native; Julia is the
  implementation language.

**Tone discipline (overrides any "big reveal" instinct from the email evidence)**:

- The series is a **project report**, not a victory lap. That is a
  rule about *framing*, not a vow of solemnity. John's blog has a
  sense of humour and uses it; humour is allowed where it is honest.
  What is not allowed is dressing the work up as a personal triumph.
- **The series is dedicated to Stephen.** Anything that reads as
  self-congratulation, underdog framing, or scoop-revenge breaks the
  dedication and has to be rewritten. The trust-building arc is told
  to honour the mentor, not to flatter the mentee.
- **State what happened. Do not editorialise about it.** "Stephen
  replied at 11:49 that the results were convincing" — not "Stephen
  was won over." Let the reader draw the line.
- **No self-congratulation.** No "and that's how I beat the cluster."
  No "the underdog won." The contribution is independent verification
  on commodity hardware; that's the framing, full stop.
- **No retrospective gloating about the scoop.** It happened. John
  kept working. The work was useful. That's the entire arc.
- **No obsequious self-deprecation either.** "If you find I am
  enjoying myself too much please tell me" is the wrong tonal note;
  it sounds apologetic and performatively humble. Trust the reader to
  notice if the framing goes wrong; do not pre-empt the criticism.
- When quoting John's own emails ("Hehehehe", "I've whacked the paper
  together"), keep them as quoted artefacts in context, not as
  punchlines. The reader gets to find them funny; the author doesn't
  point at them.
- The headline number (p=14 on a Mac) appears, sourced and dated.
  It is not described as impressive. It is what was measured.
- Where the evidence is dramatic (the screenshots), the surrounding
  prose should be flatter than usual to compensate. The screenshots
  do the lifting.

From earlier conversation:
- Post 1 names no acronyms (no WHT, no ForwardDiff, no charge, no
  adjoint). Plain English only. The trailer.
- Post 9: soften "stealing from JPM" to "learning from colleagues".
  Closing aphorism stays but qualifies.
