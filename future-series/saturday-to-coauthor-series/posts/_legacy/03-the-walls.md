# The Walls

_Part 3 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — four problems that nearly ended the project: floating-point overflow, flat loss landscapes, a GPU dead end, and the sheer scale of depth-13 on a cluster. Each wall taught us something about when to push through and when to walk away._

---

By the end of [Part 2](/tags/from-saturday-to-coauthor/), we had an evaluator that was fast ($O(p^2 \cdot 4^p)$ via WHT) and an adjoint that was cheap (1.6× forward). We were computing MaxCut results at $p = 11$ on a Mac Studio. The optimizer was converging. The numbers matched published values.

Then we started pushing to higher arity — $k = 4, 5, 6, 7$ — and higher regularity — $D = 5, 6, 7, 8$. The comparison table had fifteen $(k, D)$ pairs. We needed all of them.

Four walls were waiting.

---

## Wall 1: Overflow

At $(k = 7, D = 8)$, the evaluator started returning $\tilde{c} = 21.44$.

That's not a typo. The expected satisfaction fraction — a number that must lie between 0 and 1 — was twenty-one. The optimizer accepted it cheerfully as the best value seen so far (it beats $\tilde{c} = 0.88$ in a raw `argmax`), warm-started the next depth from those angles, and propagated the corruption forward. By $p = 10$, every result was garbage.

The root cause: the branch tensor recurrence raises arrays to the $(k{-}1)$-th and $(D{-}1)$-th power at every step. At $k = 7, D = 8$, that's element-wise `^6` and `^7` applied to complex numbers whose magnitudes can exceed 1. After 9 steps: $(1.1)^{7 \times 9} \approx 10^{27}$. Float64 overflows at $10^{308}$, but the intermediate products overflow much earlier because they're multiplied together in the constraint fold.

### First fix: always normalise (wrong)

Divide by the max magnitude at every step, track the accumulated scale in log space. Sounds clean. It was — and it destroyed the signal.

The physical information in the branch tensor lives in the _relative_ magnitudes between entries. At $p = 12$, the largest entry might be $10^{200}$ and the smallest might be $10^{198}$. Their ratio carries the signal. Normalise at every step and you're dividing $10^{200}$ by $10^{200}$, getting entries near 1.0 — but the two-digit difference that carried the physics is now at the 200th decimal place, well below Float64's 15-digit resolution.

The result: $\tilde{c} = 0.500$ everywhere. Technically correct (no overflow!), physically meaningless (the signal is crushed).

### Second fix: threshold normalisation (right)

Only normalise when the max magnitude exceeds $10^{30}$. This threshold is chosen so that even at degree 7, the powered values stay within Float64 range: $(10^{30})^7 = 10^{210} < 10^{308}$. At moderate magnitudes — where the relative differences still carry signal — the tensor is left alone.

The backward pass operates entirely on normalised intermediates (all magnitudes $\leq 1$), with scale factors detached from the gradient. This works because $\partial(\max|x|)/\partial\theta$ is a sparse selection operator — it's nonzero for at most one element, and its contribution to the gradient is negligible.

One line changed, one constant chosen. The evaluator went from producing garbage at $(7, 8)$ $p = 9$ to producing valid values through $p = 12$.

But not at $p \geq 10$ for $k \geq 6$.

### The deeper problem: catastrophic cancellation

At $(k = 6, D = 7)$ $p = 10$, the evaluator returned $\tilde{c} = 3.23$. Not overflow — the magnitudes were all below 1.5. The problem was that $\sim$2 million complex terms nearly cancelled, and the residual that makes $\tilde{c} \neq 0.5$ lived below Float64's 15-digit precision.

The fix: [DoubleFloats.jl](https://github.com/JuliaMath/DoubleFloats.jl), which provides `Double64` — a double-double arithmetic type with ~31 digits of precision. Because the evaluator was already parameterised by element type $T$ (remember the type parameter from [Part 2](/tags/from-saturday-to-coauthor/)?), the change was:

```julia
angles = QAOAAngles(Double64.(γ), Double64.(β))
```

One line. The entire 500-line pipeline — WHTs, constraint folds, power operations, root contraction — ran in Double64 without modification. Measured overhead: 3–5× (not the 10–100× I'd feared). We ran the swarm optimizer in Float64 for speed and re-evaluated winners in Double64 for correctness.

Validation: at $(6, 7)$ $p = 10$, Float64 returns 3.23 (broken). Double64 returns 0.813 (valid). At low $(k, D)$ where Float64 has sufficient precision, both agree to $10^{-9}$.

That type parameter earned its keep twice in two weeks.

## Wall 2: The GPU Detour

With the evaluator working, the bottleneck was wall time. The M4 Mac Studio has a 40-core GPU sitting idle. Could we use it?

The initial spike was discouraging. **Metal.jl does not support Float64.** Not emulated, not slow — hard-rejected. `MtlArray{ComplexF64}` throws immediately. Our entire pipeline runs in `Complex{Float64}`. Dead on arrival for a direct port.

But we didn't walk away. We built a full GPU stack anyway.

Metal works in Float32, and Stephen's NVIDIA cluster supports CUDA with Float64. So we built a backend-agnostic GPU pipeline using [KernelAbstractions.jl](https://github.com/JuliaGPU/KernelAbstractions.jl) — the same kernels run on Metal (Float32) and CUDA (Float64) with auto-detection:

```
1. CUDA  (NVIDIA, ComplexF64) — Stephen's SLURM cluster
2. Metal (Apple Silicon, ComplexF32) — Mac Studio  
3. nothing (CPU fallback)
```

Eight GPU source files: forward pass, backward pass, gradient checkpointing, fused kernels, a custom WHT kernel, and an optimizer wrapper. Seven test files validating against CPU results. The GPU forward, backward, and checkpointed backward all passed cross-validation against the CPU evaluator.

The results were mixed:

| Backend | Precision | Where it helped |
|---------|-----------|----------------|
| CUDA Float64 | Full | Stephen's cluster — exact match with CPU |
| Metal Float32 | Limited | Mac — forward only, gradient errors too large for optimisation at $p \geq 5$ |

The Metal GPU gave us ~5× at $p = 11$ for forward evaluation — useful for quick sweeps but not for gradient-based optimisation. CUDA gave us Float64 GPU acceleration on the cluster. Neither replaced the CPU adjoint as the primary production path, but the CUDA backend was a genuine contributor to the cluster runs.

### The lesson

This wall was more nuanced than "tried GPU, didn't work." We hit a precision wall on Metal, pushed through with a dual-backend architecture, and got a useful tool for specific contexts. Knowing when a dead end is really dead — vs when it's just dead _for your current use case_ — is the engineering judgement call.

## Wall 3: The Landscape

Even with overflow fixed and precision sorted, the high-$(k, D)$ pairs were stuck at $\tilde{c} = 0.5$.

Not a bug. The landscape is genuinely flat at random angles. At $(7, 8)$, standard L-BFGS with warm-starting from $p - 1$ fails at $p = 3$. Most starting points see no gradient signal at all — the expected satisfaction fraction is exactly 0.5 (the trivial value) across the entire basin.

The signal exists, but it lives in narrow basins separated by vast plateaus. L-BFGS starts on a plateau, sees zero gradient, declares convergence, and reports $\tilde{c} = 0.500$. Technically converged. Physically useless.

### The swarm

The fix came from an unexpected direction: a [blog series I wrote five years ago](/2021/12/10/scientific-computing-with-fsharp-3.html) about implementing BRKGA — a Biased Random-Key Genetic Algorithm — in F# under the tutelage of [Dr. Helmut Katzgraber](https://scholar.google.com/citations?user=s1PfsM8AAAAJ&hl=en&oi=ao). Helmut taught me that when a gradient-based optimizer can't find the basin, you let a _population_ of candidates explore the landscape, cull the failures, breed the survivors, and let natural selection do the work.

So I built a memetic optimizer. One hundred random starting points. Each gets a short L-BFGS burst — 20 iterations, just enough to see if there's signal. Kill the worst half. Replenish with crossovers from the survivors and fresh random starts. Repeat for 10 generations.

The critical insight was **early exit**: at $p \geq 6$, the warm-started candidate from the previous depth dominates within 1–3 generations. The remaining 7–9 generations were wasting 100× compute. When three consecutive generations show no improvement, stop the swarm and run a full 1280-iteration L-BFGS polish on the winner.

The swarm finds the basin. L-BFGS converges it. Best of both worlds.

Result: $(7, 8)$ went from failing at $p = 3$ to $\tilde{c} = 0.789$ at $p = 8$. All fifteen pairs now had valid results at depths where the standard approach collapsed.

### Plateau detection: four tries to get it right

A subtler convergence problem: at high $p$, each L-BFGS evaluation takes minutes. The standard stopping criterion — iterate until `g_norm < tolerance` — wastes hours when the optimizer has functionally converged but the gradient norm hovers above the threshold.

I went through four iterations of plateau detection:

1. **Fixed iteration chunks** (100 iterations, check, continue) — too coarse
2. **Depth-dependent chunks** — better, still wasted 30–40% of compute
3. **Wall-time chunks** — checked every 5 minutes, but missed fast convergence at low $p$
4. **Circular buffer with per-iteration check** — maintained a buffer of the last 30 objective values. If $\max - \min < \texttt{g\_abstol}$, stop immediately.

The fourth design worked. At $p = 12$: converged at 45 iterations (~40 minutes) instead of running the full 200 iterations (2+ hours). The optimizer was already done; it just didn't know it.

## Wall 4: Scale

A Mac Studio can compute $p = 12$ in forty minutes. $p = 13$ needs 84 GB of RAM — more than the 64 GB available — and days of wall time. The comparison table needed $p = 13$ results.

The computation spread across five machines:

| Machine | RAM | Role |
|---------|-----|------|
| Mac Studio M4 | 64 GB | Development + $p \leq 12$ for $k = 3$ family |
| Azure E8as\_v5 fleet (5×) | 64 GB each | Parallel sweep of all 15 pairs |
| Azure E16as\_v5 | 64 GB | Swarm optimizer for hard $(k \geq 5)$ pairs |
| P710 Xeon workstation | 128 GB | $(5,7)$ and $(5,8)$ swarm chains |
| Stephen's SLURM cluster | 50 × 2.7 TB | $p = 13$–15 production runs |

Coordination was primitive but effective: git branches and CSV files. Each machine read the best angles from a shared CSV, optimised the next depth, wrote the result back. `collect-all-results.jl` merged results across machines with monotonicity filtering (if $\tilde{c}$ drops at $p + 1$, the $p$ result is flagged) and overflow detection.

The warm-start package — `prepare-cluster-run.jl` — generated ready-to-submit SLURM configs from the composite best angles across all machines. Stephen ran it on 55 nodes. Three days later: $(3,4)$ $p = 13 = 0.881$ and $(3,5)$ $p = 13 = 0.843$ — both new records, both beating DQI+BP.

The fold engine didn't know it was running on five architectures across three continents. It just folded.

Total Azure spend for the entire campaign: ~$50.

---

## The Scorecard After the Walls

By mid-April, all four walls were behind us. The comparison table was filling in:

| Wall | Symptom | Fix | Cost |
|------|---------|-----|------|
| Overflow | $\tilde{c} = 21.44$ | Threshold normalisation | 1 line changed |
| Cancellation | $\tilde{c} = 3.23$ | Double64 | 1 line changed |
| GPU precision | Metal Float64 rejected | Dual-backend (Metal F32 + CUDA F64) | 8 files, 7 test files |
| Flat landscape | $\tilde{c} = 0.500$ | Memetic optimizer | 2 days |
| Slow convergence | Hours wasted | Circular buffer plateau detection | 4 iterations |
| Scale | 64 GB insufficient | Five machines, git, CSV | $50 |

Twelve of fifteen $(k, D)$ pairs beat DQI+BP. Five beat Regev+FGUM. But the evaluator was still $O(p^2 \cdot 4^p)$, and we were about to discover that somebody else had already found a way to remove that extra factor of $p$.

---

_Next: [1,875 Reasons to Sleep at Night](/tags/from-saturday-to-coauthor/) — how do you know your exact evaluator is exact? The multi-layered testing architecture that gave us confidence to publish numbers in a Google Quantum AI paper._

_Previous: [Three Gradients and a Type Parameter](/tags/from-saturday-to-coauthor/)_

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). Key files for this post: [`src/adjoint.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/adjoint.jl) (threshold normalisation in the forward pass), [`src/optimization.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/optimization.jl) (swarm optimizer + plateau detection), [`src/gpu_backend.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/gpu_backend.jl) (CUDA/Metal auto-detection). Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
