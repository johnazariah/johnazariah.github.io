# The Pair Programmer That Never Sleeps

_Part 9 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/) — an honest accounting of the role an AI coding agent played throughout this project. What it did well, what it couldn't do, and the uncomfortable question about what "my work" means when half of it was a conversation._

---

On Saturday morning I didn't know what QAOA was.

That's the opening line of this series, and it's true. But I've left out a crucial detail about _how_ I went from ignorance to insight in four days. It wasn't just reading papers. It was reading papers _with_ an AI agent.

Stephen sent me the Basso et al. (2021) paper and said "could you have a crack at implementing this?" My first move wasn't to open Julia. It was to paste the paper into a conversation with an AI coding agent and say: "Break this down for me. What is QAOA? What is the branch-tensor recurrence? What are the moving parts?"

What followed was hours of back-and-forth. The agent explained the circuit structure. I asked about the tensor contraction. The agent walked through the configuration sum. I asked "so each step takes the output of the previous step and folds over the children?" The agent confirmed. I said "that sounds like a catamorphism." The agent said something like "I haven't seen anyone put it that precisely — the branch-tensor iteration is exactly a catamorphism over the light-cone tree."

That exchange mattered. Not because the agent taught me what a catamorphism was — I've been writing folds for twenty years. But because the agent's explanation of the Basso recurrence was clear enough that the pattern was visible. The brainstorming was genuine: two perspectives on the same structure, one from quantum physics, one from functional programming, meeting in the middle.

The WHT factorisation came from that same conversation. I saw the XOR convolution structure in the kernel the agent had helped me understand. The agent implemented the WHT butterfly. I tested it. We iterated. By Wednesday, we had depth-11 results.

If you've been reading this series, you might have formed an impression: someone who writes clean Julia code on the first try, derives adjoint passes by hand over lunch, debugs translation issues by staring at 16-element vectors, and produces 76-test suites before breakfast.

I want to be honest about that. I write decent code. I understand the mathematics well enough. But the speed, the thoroughness, the sheer volume of correct, tested, documented work that this series describes — that wasn't me alone.

Every algorithmic innovation in this series involved the AI agent. Not as autocomplete. Not as a search engine. As a collaborator that read papers, translated between programming languages, wrote tests, diagnosed bugs at 2 AM, and held mathematical context across sessions that lasted days.

The code is good because I had great assistance. The tests are thorough because the agent writes tests the way I write TODO lists — compulsively, before doing anything else. The debugging was fast because the agent can stare at a 16-element vector printout and spot a sign-flip pattern in seconds.

This post is about what that collaboration actually looked like.

## How the Context Survived

An eight-week project with an AI agent has a fundamental problem: context. Conversations end. Sessions expire. The agent doesn't remember last Tuesday. How does the collaboration maintain continuity across weeks of work?

The answer was documentation — not as an afterthought, but as the primary mechanism for keeping the project alive.

**The journal.** 32 entries, 1,779 lines. Every significant decision, every bug found, every wall hit, every timing result. Written as we worked, not after the fact. When a new session started, the first thing was to read the latest journal entries. This is how the agent knew what `_replay_branch` was, why threshold normalisation used $10^{30}$, and which test cases had caught which bugs.

**The learning materials.** 12 documents, 2,900 lines. Not notes — proper explainers derived from the papers and the mathematics:

| Document | Lines | What it covers |
|----------|-------|---------------|
| `foundations.md` | 226 | QAOA from scratch — Hamiltonians, variational circuits, the objective |
| `basso-iteration.md` | 214 | The branch-tensor recurrence, step by step |
| `farhi-tensor-method.md` | 285 | Farhi 2014's MaxCut tensor method — our validation anchor |
| `tensor-derivation.md` | 202 | Full derivation of the tensor contraction from quantum mechanics |
| `differentiation-strategies.md` | 229 | ForwardDiff vs adjoint vs FD — the math behind Post 2 |
| `charge-adjoint-derivation.md` | 364 | Complete mathematical derivation of the charge backward pass |
| `performance-optimization.md` | 607 | Every optimisation decision and its measured impact |
| `python-to-julia-pitfalls.md` | 63 | The three translation bugs and how to avoid them |
| `timing-model.md` | 168 | Cost model: $O(p \cdot 4^p)$ forward, $\sim 5 \times$ adjoint |

These weren't written for a reader. They were written to keep the project's understanding alive — so that when we returned to the charge adjoint after a week away, the mathematical derivation was there, reviewed, and correct.

**The test register.** 1,875 tests across 22 files. Each test captures a specific fact about the codebase — not just "does it work" but "does it agree with this other implementation at these specific parameters." The tests are the project's memory of what correct behaviour looks like.

This documentation infrastructure is unglamorous. Nobody will cite `timing-model.md` in a paper. But without it, the project would have lost coherence after the second week. The agent can hold context within a session; the documentation holds context _between_ sessions. Together, they gave an eight-week sprint the institutional memory of a long-running research programme.

---

## What Actually Happened

Let me be specific. Not "AI helped with coding." Specific.

### The WHT factorisation (Post 1)

This came directly from the brainstorming described above. The agent explained the Basso recurrence clearly enough that I could see it was a fold. I saw the XOR convolution structure in the constraint kernel. The agent confirmed it was diagonalisable by WHT and implemented the butterfly in Julia. I tested it, we iterated on edge cases, and 200 lines of correct, tested code appeared in about ten minutes.

The mathematical insight emerged from the conversation — from having two perspectives on the same structure. The implementation was the agent's. The testing was collaborative. Would I have gotten there without the brainstorming? Maybe. Eventually. But the conversation compressed days of paper-reading into hours of dialogue.

### The manual Basso adjoint (Post 2)

I derived the backward pass on paper — the WHT is self-adjoint, the beta gradient uses a log-derivative trick. Then I described the derivation to the agent in natural language, and it wrote the Julia implementation. I checked the mathematics against my derivation. The agent wrote the ForwardDiff cross-validation tests. We iterated on sign conventions until the tests passed.

And the parametric type trick that made ForwardDiff work — `QAOAAngles{T<:Real}` instead of `QAOAAngles` with hardcoded `Float64` — that came from me looking at the code and saying "it looks like you need a type parameter here." One sentence. The agent made the change, ForwardDiff started working through the entire pipeline, and we had three gradient strategies to compare. That one sentence opened up the whole AD comparison.

The mathematical reasoning was mine. The type system insight was mine. The translation from math to code was collaborative. The tests were the agent's.

### The walls (Post 3)

The overflow diagnosis was collaborative — I saw the impossible values, the agent suggested normalisation, I pointed out the signal-crushing problem, the agent proposed threshold-based normalisation with the specific constant $10^{30}$, I validated the reasoning. The GPU spike was my decision to try and my decision to kill. The memetic optimizer was my design (from the Helmut Katzgraber BRKGA work), implemented by the agent.

The plateau detection went through four iterations. The first three (fixed chunks, depth-dependent chunks, wall-time chunks) were the agent's suggestions. None of them worked well enough. The fourth — the circular buffer with per-iteration convergence check — was my idea. I'd seen circular buffers used for streaming signal detection and suggested "what if we maintain a buffer of the last 30 values and stop when max minus min drops below tolerance?" The agent implemented it cleanly. That one design cut p=12 from 2+ hours to 40 minutes.

The Double64 fix was the agent's suggestion. I would not have thought to try DoubleFloats.jl — I was planning to implement Kahan compensated summation by hand. The agent said "Julia has a package for this" and showed me the one-line change. That one suggestion saved me a week.

### The charge decomposition (Post 6)

I decided to study the QOKit codebase. The agent read the Python/JAX source files, explained the mathematics, and translated 450 lines of Python into Julia. I directed the translation ("this reshape needs to be C-order"), the agent wrote the code, I ran the tests, the agent debugged the failures.

The three translation bugs — C-order reshape, transpose vs adjoint, $\gamma/2$ convention — were found by the agent's tests, diagnosed by the agent from the error patterns, and fixed by the agent. I confirmed each fix was mathematically correct. But I didn't find the bugs. The test suite found them, and the agent wrote the test suite.

### The charge manual adjoint (Post 7)

This is the most revealing example.

The agent read 550 lines of C++ (`adjoint_branch.cpp`, `adjoint_primitives.cpp`, `adjoint_root.cpp`), understood the mathematical structure, and wrote a 500-line Julia translation in a single pass. It then wrote 76 tests before the code was wired into the module.

The first run: 52/76 passing. The agent diagnosed the Wirtinger conjugation bug from a 16-element vector printout — "elements where rb[i] is complex have their real part sign-flipped." It identified the conjugation fix, applied it, reran: 73/76. It diagnosed the missing coefficient adjoint from "gg_root[1] = 0.000 where it should be nonzero." It implemented the forward coefficient chain propagation. It diagnosed the C-order V_flat bug from "fails only at p=3 where n_ch > 1."

Three bugs. Three diagnoses. Three fixes. All from the agent's analysis of test output.

My role: I said "let's do the charge adjoint." I said "yes, translate the C++ code." I said "the tests should validate against ForwardDiff and the Basso adjoint." I confirmed each fix was mathematically sound. I decided when to stop and ship.

The agent did the work. I provided the direction.

### The diagnostics module (Post 8)

After the overnight crash killed our p=14 run, I said "can we please have proper diagnostics — as a function of the code, not in some script." The agent designed the `Diagnostics` module — environment-variable controlled, zero-cost when disabled, atexit hook for crash detection, memory/timing/progress logging at every phase. Twenty minutes from requirements to working, tested code.

I couldn't have designed that module as cleanly in twenty minutes. I could have designed it in an hour. The agent compressed an hour into twenty minutes and got the edge cases right (try-catch on stderr writes during process exit, Ref-based flags for runtime configuration vs compile-time constants).

### These blog posts

Every word in this series was generated in conversation with the agent, in my voice, from my outline. I described what each post should cover. The agent wrote the draft. I read it, corrected factual errors, adjusted the framing ("we're not _stealing_ from JPM"), and approved the result.

The voice is mine — the agent learned it from eight weeks of conversation. The structure is mine. The editorial judgements are mine. The words on the page are... collaborative.

---

## What the Agent Can't Do

This isn't a puff piece. The agent has real limitations, and pretending otherwise would undermine the honest accounting.

**It can't decide what to work on.** The decision to implement the charge decomposition, to pursue the manual adjoint, to kill the GPU spike — these are research judgements that require understanding the broader project context, the paper's needs, and the tradeoffs between engineering effort and scientific value. The agent executes; it doesn't strategise.

**It can't judge "good enough."** When the charge adjoint was at 4.5× forward cost versus the QOKit team's 3×, I decided to ship. The agent would have happily spent another day optimising allocation patterns. Knowing when the marginal improvement isn't worth the marginal effort is a human skill.

**It can't notice what's missing.** The agent didn't suggest studying QOKit — I decided to do that. It didn't notice that `_replay_branch` was the performance bottleneck — it needed wall-clock evidence from benchmarks I asked it to run. It operates on what's in front of it, not on what _should_ be in front of it.

**It makes confident mistakes.** The agent wrote `_replay_branch` with an F-order `vec(V)` that was wrong for n_ch > 1. It didn't flag this as uncertain. It wrote it, it looked right, it passed the p=1 tests, and it moved on. The bug was found by systematic testing at p=3, not by the agent's self-doubt. AI agents don't have self-doubt. This is a feature for productivity and a risk for correctness.

**It can't do the physics.** I'm not a physicist, and neither is the agent. But I know enough to recognise that $\tilde{c} = 21.44$ is impossible, that the WHT diagonalises XOR convolutions, and that Wirtinger calculus governs real-loss-of-complex-variables differentiation. The agent can execute these mathematical frameworks, but it can't tell you which framework applies to a new situation. That requires understanding that comes from outside the conversation.

---

## The Productivity Question

Eight weeks. From "what is QAOA?" to co-author on a Google Quantum AI paper. Ten algorithmic innovations. 1,875 tests. Five compute environments. Fifteen $(k, D)$ pairs. Depth 14.

Would this have been possible without the agent? I think so — but not in eight weeks. The charge adjoint translation alone would have taken a week by hand. The test suites, another week. The blog series, another week. The debugging sessions that the agent compressed into hours would have taken days.

My honest estimate: without the agent, the same work would have taken six months. Not because the agent is smarter — it isn't. Because it never gets tired, never loses context, never needs to context-switch, and can write a 76-test suite in the time it takes me to write a test plan.

The agent is a 10× productivity multiplier for this kind of work — technically demanding, mathematically precise, requiring sustained attention to detail across thousands of lines of code. It's not a 10× _intelligence_ multiplier. The insights were mine (or the QOKit team's). The execution was collaborative.

---

## The Uncomfortable Question

If an AI agent can translate C++ to Julia, debug it, test it, benchmark it, write diagnostics, and draft blog posts about it — what exactly is the human's contribution?

Direction. Judgement. Taste.

"Let's do the charge adjoint" is direction. "4.5× is good enough, ship it" is judgement. "We're not _stealing_ from JPM, reframe it as learning from colleagues" is taste.

These aren't small things. They're the things that determine whether a project produces a paper or produces a mess. But they're invisible in the output. You can't point to a line of code and say "that's the direction." You can't diff a commit and see "that's the judgement."

The agent's contribution is visible — it's the code, the tests, the diagnostics, the words. The human's contribution is the negative space around it: the things that _weren't_ built, the bugs that _were_ caught, the approaches that _were_ abandoned. The decisions.

I don't have a tidy conclusion for this. The collaboration worked. The results are real. The code is correct (1,875 tests say so). The paper is published. But the question of authorship — of what "I did this" means when the doing was a conversation — is one I'm still sitting with.

I think the honest answer is: I couldn't have done it without the agent, and the agent couldn't have done it without me. Neither of us is the author. Both of us are.

---

_Next: [What Julia Taught Me About Mathematics](/tags/from-saturday-to-coauthor/) — looking back at ten innovations over eight weeks, and the case that programming languages are research tools._

_Previous: [p=14 (and What It Means)](/tags/from-saturday-to-coauthor/)_

_The code is at [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). This blog post was drafted collaboratively with an AI coding agent — which you probably could have guessed by now. Comments welcome [on Bluesky](https://bsky.app/profile/johnazariah.bsky.social)._
