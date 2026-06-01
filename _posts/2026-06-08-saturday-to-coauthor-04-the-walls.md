---
    layout: post
    title: "The walls"
    tags: [quantum-computing, scientific-computing, Julia, from-saturday-to-coauthor, optimization, performance]
    author: johnazariah
    summary: Part 4 of the project report. Four walls between the evaluator and the table the paper needed, and when each wall asked us to push through versus walk away.
---

_Part 4 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/). [Part 3 covered the gradient story](/2026/06/04/saturday-to-coauthor-03-three-gradients-in-one-codebase.html). This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao)._

By the end of [Part 3](/2026/06/04/saturday-to-coauthor-03-three-gradients-in-one-codebase.html), the evaluator and the manual adjoint together produced trustworthy QAOA values at moderate $(k, D)$ and moderate $p$ in seconds. The Wednesday-morning emails were behind us. The paper Stephen's team was assembling needed the same set of numbers at high $(k, D)$ and the highest $p$ we could reach.

Pushing further turned up four walls. Three of them were technical; one was about architecture. None of them was in the original plan; all of them were in the way. This post is what those walls were, what we tried, and which ones taught us to walk away rather than push through.

---

## Wall 1: the GPU spike, abandoned

Wednesday 26 March, the day after the scoop email and the "Hehehehe" reply, the evaluator was working at $p = 11$ on a Mac. The 40-core Apple GPU on the Mac Studio was sitting idle. The natural next thought was to use it.

The natural next experiment is a spike: write the smallest amount of code that proves the idea is viable. The spike failed inside an hour. Apple's `Metal.jl` does not support `Float64`. It does not support it slowly, behind emulation, with a performance warning; it rejects it. The entire forward pass and adjoint are complex-double precision, and there is no way to relax that without losing the gradient signal at the very depths we wanted to reach.

The set of paths forward from there was small. We could maintain a parallel `Float32` pipeline only for the Mac GPU, accept the precision loss for low-$p$ forward sweeps, and pay the cost of keeping two backends in sync forever. We could wrap the whole thing in a backend-agnostic GPU framework that ran `Float32` on Metal and `Float64` on CUDA elsewhere, and pay the cost of writing it, and the cost of being a second-class citizen on both platforms. Or we could keep the CPU pipeline as the production path and revisit GPU only if the CPU pipeline ran out of road.

We chose the third option, and moved on. The day's net output was a half-day spent and a directory deleted. The lesson cost almost nothing because we recognised the dead end before it grew teeth.

(There is a postscript: a CUDA prototype was eventually built, after the Phase 1 paper went to arXiv. It exists; it passes its tests; the production paper's numbers did not depend on it. It is in the repo as a thing that may matter later.)

---

## Wall 2: a landscape flat enough to fool the optimiser

The next wall was subtler. At high $(k, D)$, the optimiser stopped finding anything: it returned $\tilde c = 0.500$, which is exactly the value of QAOA at zero angles, and exactly what L-BFGS reports when it starts on a plateau and walks nowhere.

This is not a bug in the evaluator. The QAOA landscape at high arity and high regularity has wide regions where the gradient is genuinely below the optimiser's tolerance. L-BFGS sees no slope, declares convergence on the spot, and reports the trivial value. The information about where the actual basin sits exists; it lives elsewhere, and L-BFGS does not know how to look for it from a flat start.

There were two pieces to climbing out of this.

**A population, instead of a path.** Five years ago I wrote [a small F# series](/2021/12/10/scientific-computing-with-fsharp-3.html) on implementing a Biased Random-Key Genetic Algorithm under the tutelage of [Dr. Helmut Katzgraber](https://scholar.google.com/citations?user=s1PfsM8AAAAJ&hl=en&oi=ao), who has been quietly patient with me on optimisation matters for as long as I have known him. The lesson from that series is the lesson here: when a gradient method cannot find the basin, give up on the single path; spawn a population of candidates, give each a small budget, kill the failures, breed the survivors, and let the population locate the basin even where the gradient is flat. So I built a memetic optimiser: one hundred random starts, a short L-BFGS burst on each, cull the worst half, replenish by crossover from the survivors and fresh random starts, repeat. Once the population stops improving across three consecutive generations, hand the winner to a long L-BFGS polish and let it converge cleanly inside the basin the swarm found. For the pair $(7, 8)$ that had been failing at $p = 3$ with vanilla L-BFGS, the memetic version reached $\tilde c \approx 0.789$ at $p = 8$. The credit on this one goes more to Helmut than to me.

**A stopping criterion that respected the wall clock.** A separate version of "stuck" was the convergence detector itself: at high $p$, a single evaluation costs minutes; the default tolerance on the gradient norm hovers above its threshold for far longer than the run has been functionally complete; the optimiser keeps walking long after $\tilde c$ has stopped moving. We went through four versions of plateau detection before one worked properly: fixed iteration chunks (too coarse); depth-dependent chunks (still wasteful); wall-time chunks (missed fast convergence at low $p$); finally, a circular buffer of the last 30 objective values, with a stopping rule that triggered the moment $\max - \min$ across the buffer fell below the absolute tolerance. The first three each came with a few hours of conviction that this version was the one. They were not. The fourth held. At $p = 12$ for $(3, 4)$ it cut the wall time from a little over two hours to about 40 minutes for the same converged value $\tilde c = 0.8769$.

The pattern in both pieces is the same: when a tool's failure mode is silent, the fix is rarely to tune the tool's parameters. The fix is to surround the tool with a different procedure that knows what failure looks like and reacts to it.

---

## Wall 3: when the numbers themselves were the bug

The evaluator and the optimiser together at moderate arity now produced sensible numbers. Pushed to high arity, they produced numbers that should have been impossible, and the word *impossible* is doing real work in that sentence. The expected satisfaction fraction $\tilde c$ lives in the closed interval $[0, 1]$ by the definition of the problem; values outside that interval are not wrong answers, they are non-answers. The first piece of infrastructure that should have existed from day one and did not was a bounds check that refused those values at the source, halted the run, and told the optimiser something had gone wrong rather than letting the optimiser consume the output as if it meant something. Without that check, the optimiser dutifully treated any value it received as candidate truth, and acted on it.

The first failure mode of this kind was overflow, and the embarrassing way I learned about it was that I did not see it on my own machine at all. For the cases I could actually run locally on the Mac, the constraint-tensor magnitudes never grew large enough for `Float64` to overflow; the regime that exposed the bug was a regime my hardware could not reach. The first SLURM run on Stephen's cluster, at higher arities than the Mac could handle, came back with a bemused note from Stephen: my code was producing nonsense. At $(k = 7, D = 8)$ the evaluator was returning $\tilde c = 21.44$, which is not "wrong by a factor of twenty" so much as "a thing the problem cannot produce." The naive `argmax` in the optimiser, in the absence of any bounds check, had cheerfully treated the nonsense as the best value seen so far, warm-started the next depth from those angles, and propagated the corruption forward into every result that depended on it. The root cause was straightforward in retrospect: the branch-tensor recurrence raises arrays to the $(k - 1)$-th and $(D - 1)$-th powers at every step. At $k = 7$ and $D = 8$, after a few steps, intermediate magnitudes that were modestly larger than one became astronomical. `Float64` overflows at $10^{308}$ and the products in the constraint fold exceeded that long before the final answer. The bug had been there the entire time. My hardware had been too small to see it.

The first attempt at a fix was wrong. I divided through by the maximum magnitude at every step, tracked the accumulated scale in log space, and was very pleased with how clean the bookkeeping looked. It crushed the signal. At high $p$ the physical information in the branch tensor lives in the *relative* magnitudes between entries, and at the magnitudes we were reaching, two entries that differed by a few parts in a hundred in their leading digits differed by an amount that lived below `Float64`'s fifteen digits of precision once everything was scaled to $O(1)$. The evaluator stopped overflowing. It also returned $\tilde c = 0.500$ everywhere, which is to say it stopped meaning anything.

The second attempt was the threshold rule. Normalise only when the maximum magnitude exceeds $10^{30}$; below that, leave the tensor alone. The constant comes from working backwards: at degree seven, $(10^{30})^7 = 10^{210}$ stays inside `Float64`'s exponent range with room to spare, while $10^{30}$ is far above the regime where the relative differences between entries get rounded away. One line of code; one constant chosen. The $(7, 8)$ evaluator went from garbage at $p = 9$ to valid values through $p = 12$. The first attempt's clean bookkeeping went into the bin, which was where it belonged.

The deeper problem was a different kind of representation failure. At $(k = 6, D = 7)$, $p = 10$, the evaluator returned $\tilde c = 3.23$ with all intermediate magnitudes below $1.5$. This was not overflow; nothing in the pipeline had grown larger than it should. It was the same categorical impossibility from the section opener, arriving by a different route: roughly two million complex terms nearly cancelled, and the small residual that should have been $\tilde c \approx 0.8$ lived below `Float64`'s fifteen-digit precision floor, leaving the answer dominated by rounding noise. The bounds check, by this point installed, flagged the run. There was no scaling fix for this. The numbers themselves needed more bits.

The same type parameter from [Part 3](/2026/06/04/saturday-to-coauthor-03-three-gradients-in-one-codebase.html) earned its keep again. [`DoubleFloats.jl`](https://github.com/JuliaMath/DoubleFloats.jl) provides a `Double64` type with about thirty-one digits of precision, implemented as a pair of `Float64`s. Because the evaluator was already polymorphic over its element type, the change was, again, one line in the angles structure. The same 500-line pipeline, the same WHT butterflies, the same constraint folds, the same root contraction, recompiled with `Double64` and gave $(6, 7)$ $p = 10$ a value of $\tilde c = 0.813$. The Float64 result, $3.23$, had been wrong by a factor of four. The production strategy that emerged was to run the swarm in `Float64` for speed and re-evaluate winners in `Double64` for correctness; on the cases where both ran, they agreed at high $p$ to within nine digits.

The one-line type parameter from Tuesday morning, written for `Dual{Float64}`, was the same one-line type parameter that absorbed `Double64` two weeks later. We had not planned for the second use. We had not had to.

---

## Wall 4: scale, and the production rig

A Mac Studio reached $p = 12$ for the $(3, k)$ family inside an hour each. The paper wanted $p = 13$. That depth needed considerably more memory than the Mac had, and considerably more wall time than was sensible for a single machine to spend.

The first attempt at scale was a small fleet on Azure. It did not pan out. Our workload is memory-bound, mostly serial inside each run, and benefits from a thicker single node more than from a wider pool of thin ones. The Azure batch fleet we set up did not handle the access pattern well, and the total Azure spend across the entire campaign came in at fifty dollars before we shut it down. A short and useful negative result.

The production rig for the rest of Phase 1 was three pieces:

- **A dual-Xeon P710 workstation.** 128 GB of memory, 32 cores; the local thick node for cases where the Mac ran out of room.
- **The Mac Studio.** 64 GB of unified memory; the development machine, and the production machine for everything that fit.
- **Stephen's 15-node SLURM cluster at Google Quantum AI.** This is where $p = 13$ and a number of the harder $(k, D)$ pairs eventually ran, with Stephen submitting jobs and the same Julia binary running unchanged on every node.

Coordination was unglamorous. Each machine pulled best-known angles from a shared CSV, optimised the next depth, and pushed its result back. A merge script reconciled across machines with a monotonicity filter (if $\tilde c$ ever went down with depth on a given $(k, D)$ instance, the lower-depth result was flagged for re-examination) and an overflow guard. The Julia binary did not know it was running across machines on different operating systems on two continents. It just folded.

The numbers that came out of the cluster runs were the ones that ended up in the paper's table. For $(3, 4)$ at $p = 13$, $\tilde c = 0.8807$; for $(3, 5)$ at $p = 13$, $\tilde c = 0.8429$. Both beat the strongest classical comparator in the table at those parameters. The full table that Phase 1 produced has QAOA values that beat DQI + BP on thirteen of fifteen $(k, D)$ pairs we ran. Stephen, separately, had been running the same code on his cluster and was producing the same numbers, which was the first and best validation we had that none of the fixes in the previous three sections had broken anything.

---

## What walls cost, in the end

The shape of these four walls is the shape of most real performance work: the GPU one cost a half-day and was abandoned; the landscape one cost a week and produced two pieces of permanent infrastructure (the swarm, the plateau detector); the representation one cost a week and changed two lines of code that mattered, after first changing some lines that did not; the scale one cost a few hundred dollars worth of decisions and one negative result on Azure before the cluster picked up the load.

Three of the four walls have something in common worth saying out loud: the first attempt was wrong in a way that was easy to talk yourself into. Always-normalise looked tidier than threshold-normalise. Fixed iteration chunks looked simpler than a circular buffer. The Azure fleet looked like the modern answer to "we need more compute." All three were defensible at the whiteboard and all three failed on the actual numbers. The fix in each case was to take the failure mode seriously enough to redesign around it, not tune around it.

There is an obvious next question lurking under all of this. The same fold was now running with several different evaluators (Basso forward, Basso adjoint, swarm-wrapped, eventually a `Double64` recompilation) across fifteen $(k, D)$ instances of Max-$k$-XORSAT, under more than one optimisation strategy. None of that had required modifying the engine. The same architectural point applies even more strongly to a second problem family the engine was already structured for, and that Phase 2 went on to exercise; that is the subject of [Part 5](/2026/06/11/saturday-to-coauthor-05-the-algebra-that-runs-itself.html), on the algebra that runs itself.

---

_Next: **The algebra that runs itself**, on the cost algebra as a tagless final encoding, and how the same fold engine ended up running two unrelated problem families and four gradient strategies without modification._

_Code: [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). Threshold normalisation is in `src/adjoint.jl`; the swarm optimiser and plateau detector are in `src/optimization.jl`; the cluster orchestration scripts are under `scripts/`._
