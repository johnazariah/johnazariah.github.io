---
    layout: post
    title: "Julia's Child"
    tags: [Julia, quantum-computing, functional-programming, QAOA, catamorphism, performance]
    author: johnazariah
    summary: "How clean, composable Julia code didn't just implement an algorithm — it revealed hidden mathematical structure that made the impossible tractable. A story about folds, Walsh-Hadamard transforms, and why the language you think in shapes the theorems you find."
---

_This post isn't about quantum computing. Not really. It's about what happens when you write code so clean that the mathematics has nowhere to hide._

_On Saturday morning, I didn't know what QAOA was. By Wednesday evening, I was computing depth-11 exact results on a Mac Studio — results that the field considered computationally infeasible. This is the story of how that happened, and why it has more to do with functional programming than with quantum physics._

---

# Julia's Child

## Thesis

Here's a claim I want to defend:

> **The language you write in shapes the theorems you discover.** Clean, composable code doesn't just implement algorithms faster — it _reveals structure_ that would remain invisible in less expressive languages. And that revealed structure can be the difference between "computationally infeasible" and "runs on a laptop."

This isn't philosophy. I have receipts.

## The Problem (briefly)

I'm working on my PhD in quantum computing. [Stephen Jordan](https://scholar.google.com/citations?user=XZj4RPIAAAAJ) — a mentor and collaborator — asked me to compute exact QAOA performance for a class of constraint satisfaction problems — specifically, Max-3-XORSAT on 4-regular hypergraphs. The state of the art could reach circuit depth $p = 5$. We needed $p \geq 11$ to answer the scientific question. The naive cost is $O(4^{3p})$ — at $p = 11$, that's $4^{33} \approx 7 \times 10^{19}$ operations per evaluation. Not happening.

The algorithm itself was known: Basso et al. (2021) derived a branch-tensor recurrence, and Farhi et al. (2025) showed it yields exact results for MaxCut ($k = 2$). The question was whether it could be made to work for $k = 3$ at depths beyond $p = 5$.

I chose Julia. What follows is the story of why that mattered.

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

## Act 4: The Results (briefly)

The numbers tell the story:

| $p$ | $\tilde{c}(p)$ | Wall time | Gap to DQI+BP (0.871) |
|-----|-----------------|-----------|----------------------|
| 5   | 0.8205          | 260 ms    | 0.050                |
| 8   | 0.8541          | 88 s      | 0.017                |
| 10  | 0.8674          | 87 min    | **0.003**            |

At $p = 11$ — computing as I write this — QAOA crosses DQI+BP. The computation that was supposed to be infeasible runs on a commodity M4 Max Mac Studio sitting on a desk.

But the punchline isn't the numbers. It's that the **same engine**, with only two parameters changed, reproduces published MaxCut results to full precision. And the same engine, without any code modifications, will fill in the QAOA column for all fifteen $(k, D)$ pairs in the comparison table. Because the fold doesn't know what problem it's solving. It just folds.

## The Lesson

I want to come back to the thesis:

> **The language you write in shapes the theorems you discover.**

The Walsh-Hadamard factorisation didn't come from studying the Basso recurrence on a whiteboard. It came from _refactoring the code_ until the constraint kernel was a standalone function with a clear type signature. At that point, the XOR structure was staring me in the face. The mathematical insight was a _consequence_ of code clarity.

The adjoint differentiation didn't come from working through the chain rule on paper. It came from seeing the cached forward pass as a data structure that could be traversed in reverse — a pattern that's natural in a language where data structures and functions are first-class.

The parametric type trick that enabled ForwardDiff didn't come from a design document. It came from Julia's type system being expressive enough that the "right" generalisation was a one-line edit.

None of these insights required genius. They required a language that let me write the algorithm in a form clean enough that its hidden structure was visible. C++ would have buried the structure under memory management and template boilerplate. Python would have hidden it behind framework abstractions. Julia let me write the mathematics directly — and the mathematics revealed itself.

That's what I mean by "Julia's Child." The language gave birth to the insight. Not the other way around.

## What's Next

$p = 11$ should land tonight — on that same M4 Max Mac Studio, generously provided by my friend Dr JM at Apple. Then a dual Xeon with 128GB RAM for $p = 13$-$14$. Then the full 15-pair comparison table. And eventually, the paper: _"Filling in the Gaps: Generic Tree Folding for Exact QAOA on Max-$k$-XORSAT."_

All of this on commodity hardware — a Mac Studio on a desk, and an old rack server gathering dust. No cluster. No cloud. No GPU (yet).

Bon appétit.

---

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). The paper draft ("Filling in the Gaps: Generic Tree Folding for Exact QAOA on Max-$k$-XORSAT") is in progress. Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
