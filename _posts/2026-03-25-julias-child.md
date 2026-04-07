---
    layout: post
    title: "Julia's Child"
    tags: [Julia, quantum-computing, functional-programming, QAOA, catamorphism, performance]
    author: johnazariah
    summary: "How clean, composable Julia code didn't just implement an algorithm — it revealed hidden mathematical structure that made the impossible tractable. A story about folds, Walsh-Hadamard transforms, and why the language you think in shapes the theorems you find."
    update_date: 2026-04-07
---

_This post isn't about quantum computing. Not really. It's about what happens when you write code so clean that the mathematics has nowhere to hide._

_On Saturday morning, I didn't know what QAOA was. By Wednesday evening, I was computing depth-11 exact results on a Mac Studio — results that the field considered computationally infeasible. Two weeks later, I was a co-author on a Google Quantum AI paper. This is the story of how that happened, and why it has more to do with functional programming than with quantum physics._

---

# Julia's Child

_The title has a double meaning, and both are intended. [Julia Child](https://en.wikipedia.org/wiki/Julia_Child) made French cuisine accessible to American home cooks by demystifying technique and insisting that good results come from clarity, not complexity. [Julia](https://julialang.org) the programming language did the same thing for this project: it made a computationally intractable quantum physics problem accessible to a functional programmer by letting the mathematics speak clearly through the code. The result — the "child" — is a set of exact numbers that weren't supposed to be computable on commodity hardware._

## Thesis

Here's a claim I want to defend:

> **The language you write in shapes the theorems you discover.** Clean, composable code doesn't just implement algorithms faster — it _reveals structure_ that would remain invisible in less expressive languages. And that revealed structure can be the difference between "computationally infeasible" and "runs on a laptop."

This isn't philosophy. I have receipts.

## The Problem (briefly)

I'm a PhD student in quantum information at UTS. [Stephen Jordan](https://scholar.google.com/citations?user=XZj4RPIAAAAJ) — a researcher at Google Quantum AI — had a software problem. His team was writing a paper comparing algorithms for Max-$k$-XORSAT on regular hypergraphs, and they had results for every algorithm _except_ QAOA. The Basso et al. (2021) branch-tensor recurrence existed in theory, but nobody had made it work at finite degree $D$ beyond shallow depths. The QAOA column in their comparison table was blank.

"Could you have a crack at implementing this?" he asked.

The state of the art could reach circuit depth $p = 5$. They needed $p \geq 11$ to answer the scientific question. The naive cost is $O(4^{3p})$ — at $p = 11$, that's $4^{33} \approx 7 \times 10^{19}$ operations per evaluation. Not happening.

I chose Julia. What follows is the story of why that mattered — and how "having a crack" at a software problem turned into a research collaboration, with Stephen running the code on Google's cluster, debugging overflow symptoms alongside me at midnight, and ultimately inviting me as co-author on the paper.

## Act 1: The Fold Nobody Asked For

The Basso branch-tensor iteration is a loop. It takes an initial tensor (all ones), and applies a "step" function $p$ times. Each step combines child branch tensors with a constraint kernel to produce the next-level tensor. Here's what it looks like as a loop:

```julia
current = ones(ComplexF64, N)
for t in 1:p
    child_weights = f_function.(configs) .* current
    kernel = constraint_kernel(angles)
    current = constraint_fold(child_weights, kernel) .^ degree
end
```

If you're a physicist, this is a tensor network contraction. If you're a computer scientist, this is... well, it's a **fold**. A catamorphism. The tree is being consumed bottom-up, with an algebra dictating what happens at each node.

I'm a functional programmer. I can't not see that. So I refactored:

```julia
struct CostAlgebra
    k::Int          # constraint arity
    D::Int          # regularity
    clause_sign::Int # +1 for XORSAT, -1 for MaxCut
end
```

The algebra bundles everything problem-specific: the mixer weight function $f$, the constraint kernel $\kappa$, the constraint fold, and the root fold. The iteration becomes:

```julia
function evaluate(algebra, angles, p)
    current = ones(N)
    for t in 1:p
        current = step(algebra, angles, current)
    end
    root_fold(algebra, angles, current)
end
```

**Nothing about the algorithm changed.** The numbers came out identical. This was a refactor, not an optimisation. I separated the _what_ (the algebra) from the _how_ (the fold).

And then the algebra showed me something.

## Act 2: The Structure That Was Hiding

With the constraint kernel $\kappa$ pulled out as a standalone function, I could stare at it in isolation. Here's what it computes for each branch configuration $a$:

$$\kappa(a) = \cos\!\Bigl(\tfrac{1}{2}\sum_i \gamma_i \cdot s_i(a)\Bigr)$$

where $s_i(a) = (-1)^{a_i}$ is the spin eigenvalue of bit $i$.

And the constraint fold — the bottleneck, the $O(4^{kp})$ operation — was this:

$$S(a) = \sum_{b_1, \ldots, b_{k-1}} \kappa(a \oplus b_1 \oplus \cdots \oplus b_{k-1}) \cdot \prod_j g(b_j)$$

Wait. Read that again. The kernel depends on its arguments _only through the bitwise XOR_. That's a **convolution** on $\mathbb{Z}_2^{2p+1}$. And convolutions on finite abelian groups are diagonalised by the appropriate Fourier transform.

For $\mathbb{Z}_2^n$, the Fourier transform is the **Walsh-Hadamard Transform** (WHT). It's the binary analog of the DFT:

$$\hat{S} = \hat{\kappa} \cdot \hat{g}^{k-1}$$

One WHT, one element-wise power, one inverse WHT. Cost: $O(p \cdot 4^p)$ per step. The full iteration: $O(p^2 \cdot 4^p)$. For $k = 3$ at $p = 8$: **65,000× faster** than the naive approach.

Now here's the thing I want you to notice: **this factorisation was hiding in the original code.** The XOR structure was always there in the Basso recurrence. But in the original formulation — a dense sum over exponentially many configurations — it was invisible. It took _separating the kernel from the fold_ to make the convolution structure obvious.

The clean code didn't just implement the algorithm. It **revealed** that the bottleneck operation had exploitable algebraic structure.

> _The abstractions were not imposed top-down; they were read off from code that was clear enough to expose its own structure._

## Act 3: Why Julia Specifically

Let me be concrete about what Julia's design contributed. This isn't language advocacy — it's an engineering case study.

### Parametric types enabled automatic differentiation

To optimise the QAOA angles, we need gradients. Julia's `ForwardDiff.jl` provides automatic differentiation by propagating _dual numbers_ — values that carry both the function value and its derivative.

For this to work, every function in the pipeline must accept dual numbers instead of plain `Float64`s. In most languages, this would require rewriting the evaluation code or wrapping it in a framework.

In Julia, I made _one change_:

```julia
# Before
struct QAOAAngles
    γ::Vector{Float64}
    β::Vector{Float64}
end

# After
struct QAOAAngles{T<:Real}
    γ::Vector{T}
    β::Vector{T}
end
```

That's it. Julia's parametric type system infers `T = Dual{Float64, N}` when ForwardDiff calls the function, and the _entire 500-line evaluation pipeline_ — WHTs, constraint folds, branch tensor iterations — works with dual numbers without a single line changed.

This experiment revealed something crucial: **finite differences cannot converge at $p \geq 4$** because the gradient noise floor exceeds the optimiser's tolerance. Without ForwardDiff, we would have been stuck at $p = 3$.

### Multiple dispatch enabled the manual adjoint

ForwardDiff carries $2p$ dual partials through every operation, making each gradient cost $\sim 2p$ times a plain evaluation. At $p = 8$, that's 16× overhead.

So I derived the backward (adjoint) pass by hand. The key insight: the WHT is its own adjoint (the Hadamard matrix is symmetric), so the backward pass through the constraint fold is just another WHT applied to the cotangent. The $\beta$ gradient uses a log-derivative trick: $-\tan\beta$ for cosine factors, $\cot\beta$ for sine factors.

In Julia, I added the adjoint as a _new function_ alongside the existing evaluator:

```julia
function basso_expectation_and_gradient(params, angles; clause_sign=1)
    cache = forward_pass(params, angles; clause_sign)
    γ_grad, β_grad = backward_pass(cache)
    (cache.value, γ_grad, β_grad)
end
```

No existing code was modified. Multiple dispatch routes the optimiser to the adjoint when gradients are needed, and to the plain evaluator when they aren't. The adjoint costs **1.6× a single evaluation**, independent of $p$ — compared to ForwardDiff's $2p$ times.

### Zero-cost abstraction means the generality is free

Julia compiles through LLVM. The generic `QAOAAngles{Float64}` specialises to exactly the same machine code as if I'd written `Float64` everywhere. The WHT butterfly, the broadcast multiplications, the trigonometric table lookups — they all compile to the same SIMD instructions regardless of whether the source code is generic or concrete.

The cost algebra abstraction? The compiler erases it. At runtime, the fold over a `CostAlgebra` with `k=3, D=4, clause_sign=1` generates specialised code — no virtual dispatch, no heap allocation, no indirection. The generic engine runs at the same speed as a hand-written, hardcoded evaluator for Max-3-XORSAT.

### The three-way AD experiment

This is the part that wouldn't have been possible in any other language I know.

I ran all three gradient strategies — finite differences, ForwardDiff, and the manual adjoint — on the **same problem, at the same depths, in the same Julia binary**, toggled by a keyword argument:

```julia
optimize_depth_sequence(k, D, 1:10; autodiff=:finite)   # finite differences
optimize_depth_sequence(k, D, 1:10; autodiff=:forward)   # ForwardDiff
optimize_depth_sequence(k, D, 1:10; autodiff=:adjoint)   # manual adjoint
```

| Method | $p = 5$ time | $p = 8$ time | Converges at $p \geq 4$? |
|--------|-------------|-------------|--------------------------|
| Finite diff | 91 s | — | ❌ |
| ForwardDiff | 9.5 ms | 971 ms | ✅ |
| **Adjoint** | **0.85 ms** | **81 ms** | ✅ |

This head-to-head comparison — which directly informed the production design — would have required three separate codebases in C++. In Python, you'd need to wrap the evaluator in JAX or PyTorch, losing the mathematical transparency of the fold.

## Act 4: The Results

The numbers tell the story. Here's the primary target — $(k{=}3, D{=}4)$ — through $p = 13$:

| $p$ | $\tilde{c}(p)$ | Wall time | Gap to DQI+BP (0.871) |
|-----|-----------------|-----------|----------------------|
| 5   | 0.8205          | 260 ms    | 0.050                |
| 8   | 0.8541          | 88 s      | 0.017                |
| 10  | 0.8674          | 11 min    | **0.004**            |
| 11  | 0.8725          | 10 min    | **−0.002** ← crosses |
| 12  | 0.8769          | 40 min    | **−0.006**           |
| **13** | **0.8807**   | **84 hr** | **−0.010**           |

At $p = 11$, QAOA crosses DQI+BP. At $p = 12$, it crosses Prange. At $p = 13$ — computed on Stephen Jordan's 50-node SLURM cluster at Google — QAOA leads DQI+BP by a full percentage point. At $(3, 5)$, $p = 13$ yields $\tilde{c} = 0.843$, crossing the Regev+FGUM bound of $0.836$.

But the story didn't stop at the $k = 3$ family. We filled in the QAOA column for all fifteen $(k, D)$ pairs in the Jordan et al. comparison table — the column that had been blank.

The $k = 3$ family (easiest — warm-starting works reliably):

| $(k, D)$ | depth $p$ | $\tilde{c}$ | vs DQI+BP | vs Prange |
|-----------|-----------|--------------|-----------|-----------|
| (3, 4) | 13 | **0.881** | beats ✓ | beats ✓ |
| (3, 5) | 13 | **0.843** | beats ✓ | beats ✓ |
| (3, 6) | 13 | **0.814** | beats ✓ | beats ✓ |
| (3, 7) | 12 | **0.783** | beats ✓ | beats ✓ |
| (3, 8) | 12 | **0.801** | beats ✓ | beats ✓ |

The $k = 4$ family (needed fleet compute):

| $(k, D)$ | depth $p$ | $\tilde{c}$ | vs DQI+BP | vs Prange |
|-----------|-----------|--------------|-----------|-----------|
| (4, 5) | 12 | **0.869** | beats ✓ | beats ✓ |
| (4, 6) | 11 | **0.836** | beats ✓ | beats ✓ |
| (4, 7) | 11 | **0.856** | beats ✓ | beats ✓ |
| (4, 8) | 11 | **0.818** | beats ✓ | beats ✓ |

The $k \geq 5$ pairs (needed the memetic optimizer):

| $(k, D)$ | depth $p$ | $\tilde{c}$ | vs DQI+BP |
|-----------|-----------|--------------|-----------|
| (5, 6) | 11 | 0.785 | trailing |
| (5, 7) | 8 | 0.789 | trailing |
| (5, 8) | 7 | 0.769 | trailing |
| (6, 7) | 9 | **0.838** | beats ✓ |
| (6, 8) | 8 | 0.801 | trailing |
| (7, 8) | 8 | 0.789 | trailing |

Eleven of fifteen pairs beat DQI+BP. Five beat Regev+FGUM. To our knowledge, no prior exact finite-$D$ QAOA evaluation has been performed for $k \geq 3$.

And yes — the same engine, with only two parameters changed, reproduces published MaxCut results to full precision. The fold doesn't know what problem it's solving. It just folds.

## The Lesson

I want to come back to the thesis:

> **The language you write in shapes the theorems you discover.**

The Walsh-Hadamard factorisation didn't come from studying the Basso recurrence on a whiteboard. It came from _refactoring the code_ until the constraint kernel was a standalone function with a clear type signature. At that point, the XOR structure was staring me in the face. The mathematical insight was a _consequence_ of code clarity.

The adjoint differentiation didn't come from working through the chain rule on paper. It came from seeing the cached forward pass as a data structure that could be traversed in reverse — a pattern that's natural in a language where data structures and functions are first-class.

The parametric type trick that enabled ForwardDiff didn't come from a design document. It came from Julia's type system being expressive enough that the "right" generalisation was a one-line edit.

None of these insights required genius. They required a language that let me write the algorithm in a form clean enough that its hidden structure was visible. C++ would have buried the structure under memory management and template boilerplate. Python would have hidden it behind framework abstractions. Julia let me write the mathematics directly — and the mathematics revealed itself.

That's what I mean by "Julia's Child." The language gave birth to the insight. Not the other way around.

## What Happened Next

The original version of this post ended with "$p = 11$ should land tonight." It did. And then the walls started.

**The overflow wall.** At high arity — $(k{=}7, D{=}8)$ — the branch tensor entries grow exponentially through repeated `^(k-1)` and `^(D-1)` operations. By $p = 9$, they overflow Float64. The evaluator was returning `NaN` and `Inf`, and the optimizer was happily accepting them as "best values." (An overflowed $\tilde{c} = 21.44$ beats a valid $\tilde{c} = 0.88$ in a raw `argmax`.)

The fix was threshold-based normalisation: before each power operation, if the max magnitude exceeds $10^{30}$, divide by it and track the accumulated scale in log space. The backward pass operates entirely on normalised intermediates. Julia's parametric types meant the change was invisible to the rest of the pipeline — the optimiser, the adjoint, the fold engine all worked unchanged.

**The landscape wall.** Even with overflow fixed, the high-$(k, D)$ pairs were stuck at $\tilde{c} = 0.5$. Not overflow — the landscape is genuinely flat at random angles. Standard L-BFGS with warm-starting from $p - 1$ fails at $p = 3$ for $(7, 8)$. Most starting points see no signal at all.

The fix came from an unexpected direction: a [blog series I wrote five years ago](/2021/12/10/scientific-computing-with-fsharp-3.html) about implementing BRKGA — a Biased Random-Key Genetic Algorithm — in F# under the tutelage of [Dr. Helmut Katzgraber](https://scholar.google.com/citations?user=6l0K5KwAAAAJ). Helmut taught me that when a gradient-based optimizer can't find the basin, you let a _population_ of candidates explore the landscape, cull the failures, breed the survivors, and let natural selection do the work. The pattern stuck.

So I built a _memetic optimizer_ — a population-based search that combines Helmut's evolutionary approach with the L-BFGS polishing that Julia makes effortless. One hundred random starting points, short L-BFGS bursts, cull the worst half, replenish with crossovers from the survivors and fresh random starts. When the population stops improving (three stagnant generations), stop the swarm and run a full L-BFGS polish on the winner. The swarm finds the basin; L-BFGS converges it.

Result: $(7, 8)$ went from failing at $p = 3$ to $\tilde{c} = 0.789$ at $p = 8$. All fifteen pairs now have valid results at depths where the standard approach collapsed.

**The scale wall.** A Mac Studio can compute $p = 12$ in forty minutes but $p = 13$ needs 84 GB of RAM and days of wall time. So the computation spread: an Azure fleet of VMs, an old dual-Xeon P710 workstation running Windows, and Stephen Jordan's fifty-node SLURM cluster at Google — each machine warm-started from the best angles found by the others, coordinated through git branches and CSV files.

The fold engine didn't know it was running on five different architectures across three continents. It just folded.

## The Lesson (reprise)

I want to come back to the thesis one more time, now with the full story in hand:

> **The language you write in shapes the theorems you discover.**

The Walsh-Hadamard factorisation came from _refactoring the code_ until the constraint kernel was a standalone function. The normalisation fix came from _staring at the forward pass_ until the overflow pattern was clear. The memetic optimizer came from _watching the landscape_ through the evaluator's eyes.

In each case, the insight was a consequence of code clarity — not the other way around. Julia let me write the mathematics directly, and whenever the mathematics hit a wall, the code was clear enough to show me where to push.

That's still what I mean by "Julia's Child." The language gave birth to the insight. The insight gave birth to the numbers. And the numbers filled in a blank column in a comparison table that matters to people who care about the boundary between quantum and classical computation.

Stephen asked me to "have a crack" at a software problem. Two weeks later, I was a co-author on a Google Quantum AI paper. Not because I knew quantum physics — I still don't, not really — but because the code was clean enough to find structure that the physics had been hiding.

Bon appétit.

---

_The paper — "Optimization Using Locally-Quantum Decoders" by Shutty, Jordan, and Azariah — is available on [arXiv](https://arxiv.org). The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat) (DOI: [10.5281/zenodo.19211958](https://doi.org/10.5281/zenodo.19211958)). 1,741 tests passing. Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
