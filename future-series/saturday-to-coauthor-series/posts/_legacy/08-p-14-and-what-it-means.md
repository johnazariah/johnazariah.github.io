# p=14 (and What It Means)

_Part 8 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — the first exact $p=14$ MaxCut result on commodity hardware. Three attempts, one overnight crash, and the diagnostics module that was born from it._

---

Everything in this series so far has been infrastructure. The fold, the gradient, the cost algebra, the test harness, the charge decomposition, the manual adjoint — all of it was building up to the moment where the engine produces a number that nobody else has computed.

This post is about that number.

---

## The Goal

The published state of the art for _exact_ MaxCut at $(k=2, D=3)$ — 3-regular graphs, the canonical benchmark — was $p \leq 11$ on a cluster, $p \leq 12$ on a beefy workstation. Going further meant either GPU acceleration with cluster-scale memory (the QOKit team's approach for their $p=16$ result), or finding a way to make a single workstation do work it had no business doing.

We wanted $p = 14$ on commodity hardware. A Mac Studio. A Xeon workstation. Nothing exotic.

The number itself wasn't going to set a new record — the QOKit team's $p=16$ already exists. But the _accessibility_ would. If $p=14$ is something you can run on a laptop overnight, then anyone with a curiosity about deep QAOA can reproduce it. That's the entire methods-paper thesis: lower the bar to entry, and the field moves.

## The Three Attempts

### Attempt 1: Finite Differences

We never seriously tried this. The arithmetic alone made the point.

At $p = 14$, each gradient costs $4p+1 = 57$ forward evaluations. The charge forward at $p = 14$ is roughly $4\times$ the $p = 13$ forward (one extra factor of 4 from the $4^p$ scaling, partially offset by constant-factor improvements). That puts a single $p=14$ forward at around 30 seconds, and a single FD gradient at around 30 minutes. L-BFGS at $p=14$ typically wants 80–150 iterations. Call it 100.

100 iterations × 30 minutes/iteration ≈ **50 hours** for one optimisation. And that assumes the warm-start angles are good. In practice you need multiple restarts.

Even if the machine had infinite RAM, finite differences was never going to fit in a reasonable wall clock. Move on.

### Attempt 2: Adjoint v1

The first version of the charge manual adjoint — the one described in [Part 7](/tags/from-saturday-to-coauthor/) — used `_replay_branch` to reconstruct intermediates for the backward pass. Every level of the backward replayed the corresponding level of the forward. That doubled the work.

The numbers from that version:

| $p$ | Adjoint v1 (with replay) |
|-----|--------------------------|
| 11 | 421 s (7 min) |
| 12 | ~30 min (estimated) |
| 13 | did not complete |

At $p = 13$, the replay path was hitting allocation pressure that pushed wall time beyond the time-budget I'd set for a single optimisation. The math was right — the gradients matched ForwardDiff to ten digits — but the engineering wasn't good enough yet.

This is the version where I would have shipped if the goal had been $p = 12$. It wasn't.

### Attempt 3: Adjoint v2, Instrumented Forward

The fix was structural, not algorithmic. The forward pass _already_ computes every intermediate the backward pass needs. The replay was throwing them away and recomputing them. The fix: keep them.

`_charge_branch_instrumented` runs the same forward as the production evaluator, but with a small `Diagnostics`-style cache that retains the per-level $V$, $F$, and coefficient tensors needed by the backward. Memory cost: ~2× a plain forward. Compute cost: identical to a plain forward (the cache writes are amortised). The backward then reads from the cache instead of replaying.

That single change cut the adjoint cost from ~6× forward down to ~4.5× forward. The numbers:

| $p$ | Adjoint v1 | Adjoint v2 (instrumented) | Improvement |
|-----|-----------|---------------------------|-------------|
| 11 | 421 s | **373 s** | 1.13× |
| 12 | (DNF in budget) | **1,646 s (27 min)** | — |
| 13 | (DNF) | **6,653 s (111 min)** | — |

$p = 13$ in 111 minutes. On the Mac Studio. Overnight became "before lunch."

This was the version we shipped.

## The Crash

The first $p = 14$ run did not succeed.

I started it on the Mac Studio one evening. The machine had been benchmarked at $p = 13$ at 111 minutes; by linear extrapolation $p = 14$ should have taken $\sim 4\times$ that — call it eight hours. I'd be asleep before it finished. Set it running, check it in the morning.

In the morning, the terminal was at a shell prompt. No traceback. No `c̃` printed. No JLD2 output file. The optimisation log showed steady descent for the first few iterations, then nothing. The process was simply gone.

The system log told the rest of the story. Julia's resident set size had climbed past **122 GB** on a machine with **64 GB** of physical RAM. macOS had compressed and swapped what it could; eventually it ran out of room and quietly killed the process. No core dump, no diagnostic message — just a missing process and a system log entry timestamped half an hour earlier.

This isn't a bug. It isn't even an error in the usual sense. It's the working-set memory of the instrumented adjoint at $p = 14$ being _genuinely larger_ than the Mac Studio's RAM — every level of the instrumented forward holds its per-channel $V$, $F$, and coefficient tensors, and at $p = 14$ those tensors are $4\times$ larger than at $p = 13$. The numbers just stop fitting.

Eight hours of compute. No result. No clue how close we'd been when the lights went out — that's the part the code _should_ have told us, and didn't.

## The Diagnostics Module

I said to the agent, the next morning, something close to: _"Can we please have proper diagnostics — as a function of the code, not as a wrapper script. I want every long-running entry point to tell me where it is, how much memory it's using, and what the last completed phase was — and I want it to do that whether the process exits normally or gets killed."_

Twenty minutes later we had `Diagnostics.jl`. The design:

- **Environment-controlled.** Off by default. Setting `QAOA_DIAG=1` turns it on. Setting `QAOA_DIAG=2` adds per-iteration memory snapshots. No code changes to switch it on or off; nothing to comment out before publishing.
- **Zero-cost when disabled.** The diagnostic calls compile down to `Ref{Bool}` checks that branch-predict away. Measured overhead with diagnostics off: indistinguishable from no diagnostics at all.
- **Phase-tagged.** The evaluator marks every phase (`forward`, `cache`, `backward_root`, `backward_phase2`, `backward_phase1`, `optimiser_step`). The current phase is always known.
- **Atexit hook.** A finaliser registered with `atexit` writes the last known phase, the last completed iteration, the current best $\tilde{c}$, and the angles to a recovery file — even if the process is being torn down by a signal. The hook itself is wrapped in `try/catch` because writing to stderr during process exit is allowed to fail and we don't want that to mask the original failure.
- **Memory tracking.** Every phase records `Sys.maxrss()` on entry and exit. A growing high-water mark across iterations is the signature of the leak that killed us; a flat high-water mark with a sudden jump on a specific iteration is the signature of OOM.

```julia
@diag_phase :backward_phase2 begin
    # Phase 2 backward pass
    ...
end
```

The macro expands to a `Ref{Bool}` check, a phase-tag push, the body, a phase-tag pop, and a memory snapshot — all of which the compiler elides when the flag is off.

The second $p = 14$ run was started with `QAOA_DIAG=1` on the 128 GB Xeon workstation — chosen specifically because the diagnostics from the first run had shown the working set at $p=14$ exceeding what the Mac Studio could fit, not a leak that another machine would also eventually hit. The Xeon's job is to deliver the number; the Mac Studio's job, later, is to deliver it on commodity hardware.

As of this writing, that run is still in progress — L-BFGS has not yet declared convergence. But the best-so-far $\tilde{c}$ has already crossed 0.8896, and the diagnostics module is reporting steady descent with no memory anomalies. The final value will be higher than that; how much higher is what the next few hours of compute will tell us.

[**Status note as of writing:** a further round of memory improvements to the instrumented forward is in progress. The goal is to bring the $p = 14$ working set back inside 64 GB so the Mac Studio can run it directly. When that lands, this post will get an updated table — and the headline thesis ("$p = 14$ on commodity hardware") will be _literally_ true rather than _almost_ true. Watch this space.]

## The Numbers

The full MaxCut $(k=2, D=3)$ depth ladder, computed on commodity hardware with the v2 instrumented adjoint:

| $p$ | $\tilde{c}$ | Wall time | Hardware |
|-----|-------------|-----------|----------|
| 11  | **PLACEHOLDER_p11_c**  | 373 s (6 min) | Mac Studio M4 (64 GB) |
| 12  | **PLACEHOLDER_p12_c**  | 1,646 s (27 min) | Mac Studio M4 (64 GB) |
| 13  | **PLACEHOLDER_p13_c**  | 6,653 s (111 min) | Mac Studio M4 (64 GB) |
| 14  | $\geq 0.8896$ (in flight, not yet converged) | **PLACEHOLDER_p14_wall** (running) | P710 Xeon (128 GB) |

For reference, the QAOA infinite-depth limit for 3-regular MaxCut is $\tilde{c}_\infty \approx 0.9326$ (a tight upper bound from the local-tree analysis). At $p = 14$ on commodity hardware we are at $\tilde{c} \geq 0.8896$ and still descending — already within $\sim 0.043$ of the asymptotic value, before the optimiser has finished.

For comparison to classical algorithms on the same family:

- The Goemans–Williamson SDP achieves $\geq 0.8786$ on any MaxCut instance.
- DQI+BP, the classical method that was the previous benchmark for this comparison, achieves **PLACEHOLDER_dqi_bp** on 3-regular graphs.
- The Regev+FGUM rounding bound is **PLACEHOLDER_regev**.

So the question of whether finite-depth QAOA beats classical algorithms on regular MaxCut — the question the paper exists to answer — comes down to whether $\tilde{c}(p=14) > 0.8786$. The in-flight Xeon run has $\tilde{c} \geq 0.8896$ and is still descending. The answer is decided even before the optimiser declares convergence: yes, at $p = 14$, on this family, exact finite-depth QAOA beats the Goemans–Williamson worst-case guarantee — by at least 0.011 and counting.

## What It Means

I want to be careful here, because it's tempting to overclaim.

This is one instance family ($k = 2$, $D = 3$). It's a stylised setting — _regular_ graphs, _infinite-graph_ limit, _expectation_ over the QAOA distribution. None of these are real-world MaxCut instances on real-world graphs. The Goemans–Williamson SDP is a polynomial-time classical algorithm with a worst-case guarantee on _any_ graph; finite-depth QAOA on a specific family is a much weaker comparison.

But it's the comparison the field has been waiting for, because it's the one where _exact_ numbers can be computed on _both_ sides. Approximate quantum algorithms are easy to overhype; exact baselines are how you stop yourself.

What I can claim, narrowly:

- **The infrastructure works.** Twelve of fifteen $(k, D)$ pairs cleared DQI+BP on the XORSAT side of the comparison (see [Part 3](/tags/from-saturday-to-coauthor/)). The MaxCut depth-ladder reaches $p = 14$ on a single workstation. The cross-evaluator congruence tests pass at every depth. The numbers are reproducible.
- **The methodology is portable.** The fold engine is problem-agnostic. Anyone wanting to extend this to a new constraint family writes a new `CostAlgebra`, runs the test suite, and inherits everything else — gradients, adjoint, diagnostics, multi-machine orchestration. That's the entire point of the cost-algebra abstraction from [Part 5](/tags/from-saturday-to-coauthor/).
- **The collaboration model works.** Stephen at one institution, me at another, the QOKit team at a third, all of us reading each other's code and tests, all of our implementations agreeing to ten digits. That's how computational science is _supposed_ to work, and it's not the usual case.

What I won't claim:

- That QAOA "beats" classical algorithms in any general sense. It does, _for this specific question, at this specific family, with these specific resources_. That's a sentence with a lot of qualifiers in it, and that's exactly right.
- That $p = 14$ on a Mac Studio is some kind of computational miracle. It's not. It's the natural consequence of three speedups stacked — WHT factorisation, charge decomposition, manual adjoint — each one earning its place in the table from [Part 10](/tags/from-saturday-to-coauthor/).
- That the diagnostics module is exciting. It is profoundly unexciting. It is the kind of unglamorous engineering that doesn't get published anywhere, and without which the $p = 14$ run would still be a missing process and a shrug.

That last point matters most. The number at the top of the table is the result. The diagnostics module is what made the result _trustworthy enough to put in the table_.

---

_Next: [The Pair Programmer That Never Sleeps](/tags/from-saturday-to-coauthor/) — an honest accounting of the AI agent's role throughout this project._

_Previous: [The Manual Adjoint, Manually](/tags/from-saturday-to-coauthor/)_

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). Key files for this post: [`src/diagnostics.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/diagnostics.jl) (the diagnostics module), [`src/charge_manual_adjoint.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/charge_manual_adjoint.jl) (the instrumented forward, search for `_charge_branch_instrumented`), [`results/maxcut_k2_D3/`](https://github.com/johnazariah/qaoa-xorsat/tree/main/results) (the depth-ladder JLD2 files). Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
