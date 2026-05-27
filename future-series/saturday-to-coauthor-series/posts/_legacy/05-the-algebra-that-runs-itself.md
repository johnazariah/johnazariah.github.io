# The Algebra That Runs Itself

_Part 5 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — how the CostAlgebra pattern turned out to be a tagless final encoding, and why recognising that gave us extensibility, correctness, and zero-cost abstraction all at once._

---

In [Part 1](/tags/from-saturday-to-coauthor/), I refactored the Basso evaluator to separate the constraint kernel from the fold. That refactor led to the WHT discovery. But I glossed over the abstraction itself — the `CostAlgebra` type that bundles the problem definition.

This post is about that abstraction. It's the most programming-languages-flavoured post in the series, and it connects directly to a pattern I've written about before: [tagless final](/2025/12/12/tagless-final-01-froggy-tree-house.html). If you've read that series, you'll recognise the shape immediately. If you haven't, this post stands alone — but you might enjoy the connection.

---

## The Problem: Open for Extension

The evaluator was built for Max-$k$-XORSAT. But Stephen's comparison table also included MaxCut — a special case where $k = 2$ and the clause sign flips. The Basso recurrence handles both, but the constraint kernel, the root observable, and the relationship between parity expectation and satisfaction fraction all differ.

The naive approach: `if` statements everywhere.

```julia
# Don't do this
if problem == :maxcut
    kernel = maxcut_kernel(angles)
    observable = maxcut_observable()
    c̃ = (1 - parity) / 2
elseif problem == :xorsat
    kernel = xorsat_kernel(angles)
    observable = xorsat_observable()
    c̃ = (1 + parity) / 2
end
```

Every new problem variant means touching every function. The fold engine, the adjoint, the optimizer — all need `if` branches. This is the textbook open-closed violation, and it's how bugs get introduced at 2 AM when you're adding "just one more case."

## The Solution: Parametrise the Algebra

Instead of branching on the problem type, I defined a trait — a set of functions that any problem must provide:

```julia
struct CostAlgebra
    k::Int
    D::Int
    clause_sign::Int
end

# The trait methods
arity(a::CostAlgebra) = a.k
default_clause_sign(k) = k == 2 ? -1 : 1
constraint_kernel(a, angles) = ...
root_observable_kernel(a, angles) = ...
expectation_from_parity(a, parity) = (1 + a.clause_sign * parity) / 2
```

The fold engine takes a `CostAlgebra` and doesn't know or care what problem it represents:

```julia
function evaluate(algebra::CostAlgebra, angles, p)
    # ... fold over light-cone tree ...
    parity = root_contraction(...)
    expectation_from_parity(algebra, parity)
end
```

MaxCut is `CostAlgebra(2, D, -1)`. Max-3-XORSAT is `CostAlgebra(3, D, +1)`. The fold engine is unchanged. The adjoint is unchanged. The optimizer is unchanged.

## Where I've Seen This Before

If you've read my [tagless final series](/2025/12/12/tagless-final-01-froggy-tree-house.html), this shape should look familiar.

In tagless final, a "program" is a function parameterised by an interpreter. The program doesn't know what the interpreter does — it just calls the interface. Different interpreters can evaluate the program, pretty-print it, optimise it, or verify it. The program is written once; the interpretations are open for extension.

Here, the "program" is the light-cone tree structure — the shape of the Basso recurrence, fixed by the QAOA circuit depth $p$ and the tree parameters. The "interpreter" is the `CostAlgebra` — the problem-specific definitions that give meaning to the tree's nodes.

| Tagless Final | QAOA Evaluator |
|---------------|----------------|
| Program | Light-cone tree structure |
| Interpreter | CostAlgebra |
| Evaluate | `basso_expectation(params, angles)` |
| Pretty-print | (not implemented, but possible) |
| Differentiate | `basso_expectation_and_gradient(...)` |
| Verify | Cross-evaluator congruence tests |

This isn't an analogy. It's the same pattern, applied to a different domain.

## Why It Matters: Four Payoffs

### 1. Extensibility without modification

Adding a new problem — say, Max-2-LINSAT or weighted XORSAT — means defining a new `CostAlgebra` with the appropriate kernel. The fold engine, the adjoint, the optimizer, the test harness: none of them change. This is what "open for extension, closed for modification" actually looks like when you take it seriously.

### 2. Cross-validation is structural

The congruence tests from [Part 4](/tags/from-saturday-to-coauthor/) work _because_ the algebra is parametric. Validating the fold engine on MaxCut (where we have published reference values) validates it for XORSAT too — because the engine doesn't know which problem it's solving. The only thing that changes is the algebra, and the algebra is three functions that can be tested in isolation.

### 3. Gradients are algebra-aware

The adjoint doesn't differentiate through `if` branches. It differentiates through the algebra's trait methods, which are smooth functions of the angles. The gradient of `constraint_kernel` is a well-defined mathematical object regardless of whether the algebra represents MaxCut or XORSAT. This is why the same adjoint code works for both — not because we wrote generic gradient code, but because the algebra made the gradient structure visible.

### 4. Zero-cost in practice

Julia compiles through LLVM. When you call `evaluate(CostAlgebra(3, 4, 1), angles, 10)`, the compiler specialises the entire fold for $k = 3, D = 4$, `clause_sign = 1`. The algebra struct is erased at compile time. No virtual dispatch, no heap allocation, no runtime branching. The generic engine runs at exactly the same speed as a hand-written, hardcoded Max-3-XORSAT evaluator.

This is the point where Julia's design philosophy pays off. In most languages, abstraction has a runtime cost — virtual methods, dynamic dispatch, indirection. In Julia, parametric types and multiple dispatch give you abstraction at the source level and specialisation at the machine level. The abstraction is _literally free_.

## The Uncomfortable Admission

I didn't set out to implement tagless final. I set out to avoid copy-pasting the evaluator when Stephen asked for MaxCut results alongside XORSAT. The pattern emerged from the refactor — separate the kernel, parametrise the fold, give the problem definition a name.

But once I recognised what it was, I leaned into it. The cost algebra became the central organising principle of the codebase. Every function that touches problem-specific logic goes through the algebra. Every test that validates the engine works by instantiating the algebra with MaxCut parameters and checking against published values.

The pattern didn't make the code faster. The WHT did that. The pattern made the code _trustworthy_ — because it separated the part we could validate (MaxCut, with known answers) from the part we couldn't (XORSAT, with no published finite-$D$ results), while guaranteeing that both use the same engine.

That separation is the tagless final promise: write the program once, interpret it many ways, and trust that the interpretations are consistent because they share the same structure.

---

_Next: [Learning From the Masters](/tags/from-saturday-to-coauthor/) — how studying the QOKit team's published implementation taught us a deeper mathematical structure that removed a full factor of $p$ from the evaluator._

_Previous: [1,875 Reasons to Sleep at Night](/tags/from-saturday-to-coauthor/)_

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). Key files for this post: [`src/cost_algebra.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/src/cost_algebra.jl) (the CostAlgebra trait), [`test/test_cost_algebra.jl`](https://github.com/johnazariah/qaoa-xorsat/blob/main/test/test_cost_algebra.jl) (MaxCut vs XORSAT dispatch tests). Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
