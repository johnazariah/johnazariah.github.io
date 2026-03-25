---
    layout: post
    title: "Julia's Child"
    tags: [Julia, quantum-computing, functional-programming, QAOA, catamorphism, performance]
    author: johnazariah
    summary: "How a functional programmer's instinct — recognising a tensor network contraction as a fold — led to a Walsh-Hadamard factorisation, a manual adjoint, and exact QAOA results that were supposed to be computationally infeasible."
---

_This post is the story of a weekend. Not the kind where you fix a CSS bug and call it productive — the kind where you accidentally make a computational breakthrough because you couldn't stop yourself from refactoring a loop into a fold._

---

# Julia's Child

## The Assignment

My PhD supervisor asked me a simple question: _"Can you compute exact QAOA performance for Max-3-XORSAT on 4-regular hypergraphs?"_

The context: [Stephen Jordan](https://scholar.google.com/citations?user=XZj4RPIAAAAJ) and collaborators at Google had published a [Nature paper](https://www.nature.com/articles/s41586-024-08033-4) on Decoded Quantum Interferometry (DQI) — a clever quantum algorithm that reduces optimisation to error-correcting code decoding. They had a comparison table with four algorithm columns (Prange, DQI+BP, Regev+FGUM, simulated annealing) across fifteen problem instances. One column was missing: **QAOA** — the Quantum Approximate Optimisation Algorithm.

The state of the art, per [Basso et al. (2021)](https://arxiv.org/abs/2110.14206), could evaluate QAOA exactly on these structures. But for 3-body constraints ($k = 3$), the cost was $O(4^{3p})$ per evaluation — exponential in three times the circuit depth. Previous work had reached $p = 5$ at most. At $p = 5$, QAOA scores about 0.82 on our target problem. DQI+BP scores 0.871. The comparison was unresolved: _does QAOA ever catch up?_

To answer that, we needed $p \geq 11$. The naive approach would take years.

## The Insight No One Asked For

I'm a functional programmer. I can't help it. When I see a recursive computation over a tree, I see a **fold** — a catamorphism. When I see the same function called with different parameters, I see a **parametric algebra**. When I see a loop doing the same thing $p$ times, I see a **recurrence** that should be separated from the data it operates on.

So when I sat down with the [Basso finite-$D$ branch-tensor iteration](https://arxiv.org/abs/2110.14206) — a perfectly good numerical algorithm that contracts a tensor network from leaves to root on a light-cone factor graph tree — I did what any self-respecting FP person would do.

I refactored it into a fold.

```julia
# The branch tensor iteration IS a catamorphism
current = ones(Complex{T}, N)
for _ in 1:p
    current = step(algebra, current)
end
```

This was supposed to be a cosmetic change. The algorithm was the same. The numbers came out the same. I just separated the _what_ (the algebra — mixer weights, constraint kernels, root observable) from the _how_ (the fold — iterate from leaves to root, accumulate a branch tensor).

But then something happened that doesn't usually happen when you refactor for clarity: the refactored code _showed me something new_.

## The Walsh-Hadamard Discovery

With the fold explicit, the constraint-fold step stood out as the bottleneck. For $k \geq 3$, it was a sum over all $2^{2p+1}$ branch configurations, for each of $k - 1$ children. This is the $O(4^{kp})$ cost.

But staring at the clean, separated kernel function, I noticed it depended on its arguments only through their **bitwise XOR**. That's a convolution on $\mathbb{Z}_2^{2p+1}$. And convolutions on finite abelian groups are diagonalised by the appropriate Fourier transform — in this case, the **Walsh-Hadamard Transform**.

$$\hat{S} = \hat{\kappa} \cdot \hat{g}^{k-1}$$

One WHT, one element-wise power, one inverse WHT. Cost: $O(p \cdot 4^p)$ per step, $O(p^2 \cdot 4^p)$ total. For $k = 3$ at $p = 8$, this is **65,000× faster** than the naive approach.

I would not have seen this in a 500-line C++ function with manual memory management and loop indices. The mathematical structure was invisible until the code was clean enough to expose it. _The language didn't implement the math — it revealed it._

## Three Gradient Methods Walk Into a Bar

With the evaluator fast, we needed to _optimise_ over $2p$ continuous angle parameters using L-BFGS. That requires gradients.

**Attempt 1: Finite differences.** The sensible default. Worked fine at $p = 3$. At $p = 4$, the optimiser hit its iteration limit. At $p = 5$, it burned 91 seconds and 50,000 evaluations trying to converge, then gave up. The gradient noise floor exceeded the convergence tolerance. Dead end.

**Attempt 2: ForwardDiff.jl.** Julia's automatic differentiation library. I made one change — turned `QAOAAngles` into a parametric type `QAOAAngles{T<:Real}` — and ForwardDiff's dual numbers flowed through 500 lines of evaluation code _unmodified_. At $p = 5$: converged in 17 iterations, 2.9 seconds. 31× faster than finite differences, and it actually converged.

But ForwardDiff carries $2p$ dual number partials through every operation. At $p = 8$, each gradient costs 19× a plain evaluation. At $p = 12$, it would be 24×.

**Attempt 3: Manual adjoint.** I derived the backward pass by hand. The key insight: **the WHT is its own adjoint** (the Hadamard matrix is symmetric). So the backward pass through the constraint fold is just... another WHT applied to the cotangent. The $\beta$ gradient uses a log-derivative trick: $-\tan\beta$ for cosine factors, $\cot\beta$ for sine factors.

Cost: **1.6× a single evaluation, independent of $p$**. At $p = 8$, that's 12× faster than ForwardDiff. The speedup grows linearly with depth.

All three gradient methods coexist in the same codebase. I ran them head-to-head on the same problem at the same depths, in the same Julia binary, with a keyword argument toggle. Try doing that in C++.

## The Numbers

| $p$ | $\tilde{c}(p)$ | Wall time | Gap to DQI+BP |
|-----|-----------------|-----------|---------------|
| 1   | 0.6761          | 1.7 s     | 0.195         |
| 5   | 0.8205          | 260 ms    | 0.050         |
| 8   | 0.8541          | 88 s      | 0.017         |
| 9   | 0.8613          | 6.5 min   | 0.010         |
| 10  | 0.8674          | 87 min    | **0.003**     |

DQI+BP = 0.871. The gap is closing at a steady rate (~0.006 per step). **At $p = 11$, QAOA crosses DQI+BP.** That computation is running right now, on the same laptop I'm writing this on.

## The Punchline

The fold engine is generic. MaxCut? Set $k = 2$ and `clause_sign = -1`. We reproduced Farhi et al.'s published MaxCut results to full precision — **no code changes, just different algebra parameters**. The same engine fills in the entire missing QAOA column across all fifteen $(k, D)$ pairs in Jordan et al.'s comparison table.

The code is ~700 lines of Julia. It runs on a laptop. It has 714 tests and 100% coverage. The evaluator, the three gradient methods, the optimiser, the fold algebra, the WHT — all in one composable package.

## Why Julia

This project would have been an order of magnitude harder in Python (framework lock-in for AD, no parametric dispatch, numpy indirection hiding the algebra) and two orders of magnitude more error-prone in C++ (templates for generics, manual memory for the branch-tensor cache, no composable AD ecosystem, and critically — the boilerplate would have hidden the fold structure that led to the WHT discovery).

Julia gave us:
- **Parametric types** → ForwardDiff worked with a one-line change
- **Multiple dispatch** → the adjoint was a new method, not a rewrite
- **Zero-cost abstractions** → the generic fold runs at the same speed as hand-written Float64 code
- **LLVM compilation** → the hot path (WHTs, broadcasts) generates the same machine code as C

The performance "sacrifice" for all this expressivity? Zero. The WHT is memory-bandwidth-bound at every depth. Julia and C++ produce the same SIMD code for the same butterfly.

## What's Next

$p = 11$ should land tonight. Then the dual Xeon with 128GB RAM for $p = 13$-$14$. Then the full 15-pair comparison table. And eventually, the paper: _"Filling in the Gaps: Generic Tree Folding for Exact QAOA on Max-$k$-XORSAT."_

The fold engine is the lasting contribution. The QAOA numbers are the headline. The Walsh-Hadamard factorisation is the trick. The adjoint is the performance enabler. But the thing I'm proudest of is this: **the mathematical insight came from writing clear code**. Not the other way around.

Bon appétit.

---

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). The paper draft is in progress. Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
