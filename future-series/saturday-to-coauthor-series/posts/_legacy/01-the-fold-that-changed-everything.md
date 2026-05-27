# The Fold That Changed Everything

_Part 1 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — how clean Julia code revealed hidden mathematical structure in a quantum computing problem, and turned "computationally infeasible" into "runs on a laptop."_

_This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao) — colleague, friend, and the person who kept my interest in quantum computing alive long after I'd left the field professionally. Stephen and I very nearly got to work together at Microsoft Research years ago; for reasons that had nothing to do with either of us, it didn't happen. So in a real sense this collaboration was a long time coming — and it was worth the wait._

---

On Saturday morning, I had only come across QAOA as a quantum optimization algorithm, long ago, in my hoary, pre-COVID days at Microsoft Quantum.

By Wednesday evening, I was computing depth-11 exact results on a Mac Studio — results that the field considered computationally infeasible. Eight weeks later, I was a co-author on a Google Quantum AI paper.

This is the story of how that happened, and why it has more to do with functional programming than with quantum physics. This first post covers the single insight that made everything else possible: recognising a loop as a fold, and finding a 65,000× speedup hiding inside a clean abstraction.

---

## The Phone Call

[Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao) and I were colleagues at Microsoft Quantum. But Stephen has a gift for keeping conversations alive across years, continents, and career changes. He's kept my interest in quantum computing burning with insightful discussions long after I left the field professionally.

One of those conversations turned into a problem. His team was writing a paper comparing algorithms for Max-$k$-XORSAT on regular hypergraphs, and they had exact results for every algorithm _except_ QAOA. The Basso et al. (2021) branch-tensor recurrence existed in theory, but nobody had made it work at finite degree $D$ beyond shallow depths. The QAOA column in their comparison table was blank.

"Could you have a crack at implementing this?" he asked.

The state of the art could reach circuit depth $p = 5$. They needed $p \geq 11$ to answer the scientific question. The naive cost is $O(4^{3p})$ — at $p = 11$, that's $4^{33} \approx 7 \times 10^{19}$ operations per evaluation. Not happening.

I chose Julia. What follows is the story of why that mattered.

## The Loop

The Basso branch-tensor iteration looks like this. You take an initial tensor (all ones), and apply a "step" function $p$ times. Each step combines child branch tensors with a constraint kernel to produce the next-level tensor:

```julia
current = ones(ComplexF64, N)
for t in 1:p
    child_weights = f_function.(configs) .* current
    kernel = constraint_kernel(angles)
    current = constraint_fold(child_weights, kernel) .^ degree
end
```

If you're a physicist, this is a tensor network contraction. If you're a functional programmer, this is something else entirely.

It's a **fold**. A catamorphism. The tree is being consumed bottom-up, with an algebra dictating what happens at each node.

I'm a functional programmer. I can't not see that. So I refactored.

## The Refactor

I separated the _what_ (the problem definition) from the _how_ (the fold):

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

**Nothing about the algorithm changed.** The numbers came out identical. This was a refactor, not an optimisation. I separated concerns and gave names to the parts.

And then the algebra showed me something.

## The Structure That Was Hiding

With the constraint kernel $\kappa$ pulled out as a standalone function, I could stare at it in isolation. Here's what it computes for each branch configuration $a$:

$$\kappa(a) = \cos\!\Bigl(\tfrac{1}{2}\sum_i \gamma_i \cdot s_i(a)\Bigr)$$

where $s_i(a) = (-1)^{a_i}$ is the spin eigenvalue of bit $i$.

And the constraint fold — the bottleneck, the $O(4^{kp})$ operation — was this:

$$S(a) = \sum_{b_1, \ldots, b_{k-1}} \kappa(a \oplus b_1 \oplus \cdots \oplus b_{k-1}) \cdot \prod_j g(b_j)$$

Wait. Read that again. The kernel depends on its arguments _only through the bitwise XOR_. That's a **convolution** on $\mathbb{Z}_2^{2p+1}$. And convolutions on finite abelian groups are diagonalised by the appropriate Fourier transform.

For $\mathbb{Z}_2^n$, the Fourier transform is the **Walsh-Hadamard Transform** (WHT). It's the binary analog of the DFT:

$$\hat{S} = \hat{\kappa} \cdot \hat{g}^{k-1}$$

One WHT, one element-wise power, one inverse WHT. Cost: $O(p \cdot 4^p)$ per step. The full iteration: $O(p^2 \cdot 4^p)$.

For $k = 3$ at $p = 8$: **65,000× faster** than the naive approach.

## The Part I Want You to Notice

This factorisation was hiding in the original code. The XOR structure was always there in the Basso recurrence. But in the original formulation — a dense sum over exponentially many configurations — it was invisible. It took _separating the kernel from the fold_ to make the convolution structure obvious.

The clean code didn't just implement the algorithm. It **revealed** that the bottleneck operation had exploitable algebraic structure.

I want to be precise about what happened here, because it's easy to romanticise:

1. I refactored the code to separate the constraint kernel from the fold. This was a **programming** decision, motivated by wanting a clean abstraction — not a mathematical insight.

2. With the kernel isolated, I noticed it depended on its arguments only through XOR. This was a **mathematical** observation, but one that was only possible because the code was clear enough to make it visible.

3. I recognised the XOR-dependent sum as a convolution on $\mathbb{Z}_2^n$ and applied the WHT. This was a **mathematical** technique, but the opportunity to apply it was created by the refactor.

The abstractions were not imposed top-down. They were read off from code that was clear enough to expose its own structure.

> **The language you write in shapes the theorems you discover.** Clean, composable code doesn't just implement algorithms faster — it _reveals structure_ that would remain invisible in less expressive languages.

This isn't philosophy. It's what happened on a Wednesday afternoon, staring at a function that had been pulled out of a loop.

## The Result

With the WHT factorisation, the Basso evaluator went from hopeless to practical:

| Depth $p$ | Naive $O(4^{3p})$ | WHT $O(p^2 \cdot 4^p)$ | Speedup |
|-----------|-------------------|-------------------------|---------|
| 5 | $4^{15} \approx 10^9$ | $25 \cdot 4^5 \approx 25{,}600$ | 40,000× |
| 8 | $4^{24} \approx 3 \times 10^{14}$ | $64 \cdot 4^8 \approx 4.2\text{M}$ | 65,000× |
| 11 | $4^{33} \approx 7 \times 10^{19}$ | $121 \cdot 4^{11} \approx 508\text{M}$ | $10^{11}\times$ |

Depth 11 went from centuries to seconds. On a laptop.

But $O(p^2 \cdot 4^p)$ was just the beginning. The evaluator needed gradients for optimisation, and the gradient story is where Julia's type system earned its keep. That's [the next post](/tags/from-saturday-to-coauthor/).

---

_Next: [Three Gradients and a Type Parameter](/tags/from-saturday-to-coauthor/) — how Julia's parametric types made it trivial to compare finite differences, automatic differentiation, and a hand-derived adjoint in one codebase._

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). Key files for this post: [`src/wht.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/wht.jl) (WHT butterfly), [`src/basso_finite_d.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/basso_finite_d.jl) (branch tensor iteration), [`docs/innovations.md`](https://github.com/johnazariah/qaoa-xorsat/blob/main/docs/innovations.md) (all ten innovations with code pointers). Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
