---
    layout: post
    title: "The fold under the tree"
    tags: [quantum-computing, scientific-computing, Julia, from-saturday-to-coauthor, functional-programming, catamorphism]
    author: johnazariah
    summary: "Part 2 of the project report. The first algorithmic insight: a recurrence turned out to be a fold over a tree, and a fold turned out to be a Walsh-Hadamard transform."
---

_Part 2 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/). [Part 1 sets the scene](/2026/05/29/saturday-to-coauthor-01-saturday.html). This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao)._

In Part 1 we left Saturday morning with three papers, a 03:47 email, and no idea what quantum approximate optimisation was. This post is about what happened between then and Monday evening.

It is about a recurrence in the literature that turned out to be a fold over a tree, and about a fold that turned out to be a convolution.

---

## The recurrence

The papers Stephen sent linked through to Basso, Farhi, Marwaha and Zhou (2021). They give a way to compute the QAOA expectation value at depth $p$ exactly, without simulating any quantum circuit. The method is a tensor recurrence: it walks up the tree of operator light-cones, level by level, and produces a number at the root.

It looks, on the page, like this:

$$
T^{(t+1)}_a \;=\; \sum_{b_1, \ldots, b_{k-1}} \kappa\bigl(a \oplus b_1 \oplus \cdots \oplus b_{k-1},\, \gamma_t\bigr) \, \prod_{j=1}^{k-1} g(b_j, \beta_t) \cdot T^{(t)}_{b_j}
$$

and like this in code:

```julia
function basso_step(current, angles, k, t)
    next = zeros(ComplexF64, length(current))
    for a in 0:length(current)-1
        s = zero(ComplexF64)
        for bs in iter_branches(a, k-1)
            s += kernel(a, bs, angles, t) *
                 prod(g(b, angles, t) * current[b+1] for b in bs)
        end
        next[a+1] = s
    end
    next
end
```

(The real code is more careful than this; the structure is what matters here.)

If you are a physicist, this is a tensor-network contraction with a light-cone constraint pattern. If you are a functional programmer, this is something else.

It is a fold.

---

## A fold, and an algebra

A fold consumes a recursive data structure bottom-up using two pieces of information: a *seed*, which says what to do at the leaves, and a *combine*, which says what to do at each internal node given the results from its children. In a language with sum types, a fold over a tree is one short function. In Julia, the same idea is expressed by parametrising the loop on an *algebra* that carries the per-node operations.

Once the recurrence is read as a fold, the constituent parts come apart cleanly:

- a *seed*: at the leaves of the light-cone tree, every tensor entry is one;
- a *child weight*: how to weight a child branch (the function $g$);
- a *constraint kernel*: $\kappa$, the cosine of a sum of signed angles, applied once per node;
- a *constraint fold*: the inner sum over $b_1, \ldots, b_{k-1}$ that combines $k-1$ children into one parent;
- a *root observation*: what to do at the top to read out the number.

Five named pieces. The fold itself, having been given those five pieces, is one short loop:

```julia
function evaluate(algebra::CostAlgebra, angles, p)
    current = seed(algebra)
    for t in 1:p
        current = step(algebra, angles, current, t)
    end
    root(algebra, angles, current)
end
```

We have not done anything mathematically yet. The numbers this code produces are bit-for-bit the same numbers the original code produced. This is a refactor, not an optimisation. The only thing that has changed is the names of the parts.

That is the move I want to be precise about. *Naming the parts is the algorithmic insight.* Until the kernel had its own name, it lived inside the body of an inner loop, alongside everything else the loop was doing. Once it lived in a function of its own, it could be stared at.

So we stared at it.

---

## What the kernel actually depends on

The constraint kernel, written out, is

$$
\kappa(a, b_1, \ldots, b_{k-1}, \gamma) \;=\; \cos\!\Bigl( \tfrac{\gamma}{2} \sum_i s_i\bigl(a \oplus b_1 \oplus \cdots \oplus b_{k-1}\bigr) \Bigr)
$$

where $s_i(x) = (-1)^{x_i}$ extracts the sign of bit $i$. Read that twice. The kernel takes $k$ arguments, each a bit-string of length $2p+1$, and combines them in exactly one way: by **bitwise XOR**. Everything else it does to its arguments is a function of that XOR alone.

Which means the constraint fold

$$
S(a) \;=\; \sum_{b_1, \ldots, b_{k-1}} \kappa\bigl(a \oplus b_1 \oplus \cdots \oplus b_{k-1}\bigr) \prod_j h(b_j)
$$

with $h(b) = g(b, \beta) \cdot T^{(t)}_b$ the weighted child tensor, is a **convolution** on the group $\mathbb{Z}_2^{n}$, where $n = 2p + 1$.

That is the second algorithmic move, and it is mathematical rather than linguistic: not "the code is now clearer" but "the operation, written with its hidden symmetry exposed, has a name and a standard treatment."

---

## The Walsh-Hadamard transform

Convolutions on finite abelian groups are diagonalised by the Fourier transform on the group. For $\mathbb{Z}_2^n$, the Fourier transform has a particularly simple form: the **Walsh-Hadamard transform**, computable in $O(n \cdot 2^n)$ by a butterfly almost identical to the FFT's, with no twiddle factors and no complex arithmetic. After WHT, the convolution becomes element-wise multiplication. After element-wise multiplication, an inverse WHT brings us home.

So the constraint fold, naively a sum over $2^{n(k-1)}$ terms, becomes:

1. WHT each weighted child tensor: $O(n \cdot 2^n)$.
2. Take the element-wise $(k-1)$-th power: $O(2^n)$.
3. Multiply element-wise by the WHT of the kernel: $O(2^n)$.
4. Inverse WHT: $O(n \cdot 2^n)$.

Per step of the iteration, that is $O(p \cdot 4^p)$. Over all $p$ steps, $O(p^2 \cdot 4^p)$.

The naive cost at $k = 3$ is $O(4^{3p}) = O(64^p)$. The exponent in the base dropped from $3p$ to $p$. For depth $p = 8$ at $k = 3$ the operation count fell by a factor of roughly $65{,}000$. On the actual machine, the same calculation went from "never completes" to **eleven minutes**, end to end.

Both numbers are the right answer to different questions. They are quoted together because both are what made the rest of the project possible.

---

## What was actually discovered, and where

Two things are easy to misread in the previous section, so it is worth separating them.

The first move, the catamorphism recognition, is a programming-language move rather than a mathematical one. The recurrence in the paper is already mathematically correct. Recognising it as a fold over a tree, with a separable algebra, is what a functional programmer brings to it. That recognition does not produce a faster algorithm by itself. What it produces is a *vocabulary*: a way of writing the algorithm in which each part has a name, and each part can be questioned independently.

The second move, the WHT factorisation, is a mathematical observation. The XOR-convolution structure was always in the recurrence; nobody hid it. But until the kernel was a function of its own, called once per node, looked at on its own page, the symmetry it had was decorative noise rather than something to exploit. **The naming made the symmetry visible. The visibility made the factorisation thinkable.**

This is the thesis the series will keep returning to: *the language you think in shapes the theorems you can see*. It is not a claim about Julia in particular; the same exercise in F# or Haskell would have surfaced the same structure. It is a claim about clean, abstracted code as a research instrument.

---

## AI for Insight

This discovery started in conversations with Claude and Gemini while I was still getting familiar with the mechanics of the computation.

I kept asking for clarifications about the branch recurrence, and two things became clearer. First, locally, the light-cone tree has exchangeable child subproblems, so the evaluator should compute one representative contribution and reuse it for the other copies. Second, globally, the contraction was not just a contraction: it was a bottom-up traversal of the tree. It was a fold.

That was the useful shape of the AI interaction here: Socratic coaching, not delegated agency. The functional-programming pattern was mine, but the conversation helped sharpen the explanation until the pieces separated cleanly enough to inspect. Once that happened, the next question became visible: what exactly is the combine step doing?

---

## What this bought us

By Monday evening, the evaluator was a small Julia program with a separated algebra, a WHT-based constraint fold, and a reproducible answer at low $p$ that agreed with published results from the original paper to many decimal places. The published state of the art for this method had stopped at $p = 5$. Tuesday morning, after the manual gradient that Part 3 covers, the same code reached $p = 9$ on a Mac in under an hour. The arc described in Part 1, from a Saturday email to a Wednesday "Hehehehe", begins here, with two named functions and a Fourier transform on $\mathbb{Z}_2^n$.

Nothing about Stephen's original ask required any of this. He asked for high-performance code on a big cluster. What he received, two days in, was a small Julia evaluator on a desktop, doing a thing his question had not strictly mentioned. The rest of the series is what happened after that.

---

_Next: **Three gradients in one codebase**, on why optimising the angles needs gradients, why three completely different ways of computing them ended up in the same codebase, and what Julia's type system did to keep them honest._

_Code: [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). The fold is in `src/basso_finite_d.jl`; the WHT butterfly is in `src/wht.jl`._
