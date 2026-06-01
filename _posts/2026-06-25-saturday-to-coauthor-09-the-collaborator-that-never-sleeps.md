---
    layout: post
    title: "The collaborator that never sleeps"
    tags: [quantum-computing, scientific-computing, Julia, from-saturday-to-coauthor, AI, software-engineering]
    author: johnazariah
    summary: Part 9 of the project report. The disciplines and techniques that made eight weeks of AI-assisted research productive. Journals, learning materials, tests, and the conversational patterns that kept the engine running.
---

_Part 9 of [From Saturday to Co-Author](/tags/from-saturday-to-coauthor/). [Part 8 covered $p = 14$ on a Mac](/2026/06/22/saturday-to-coauthor-08-fourteen.html). This series is dedicated to [Stephen Jordan](https://scholar.google.com/citations?user=dcSsY4cAAAAJ&hl=en&oi=ao)._

LaTeX replaced hand-set type. Google Scholar replaced the index catalogues at the back of bound journal volumes. Git replaced the practice of mailing each other tarballs. Each of those instruments, when it arrived, was treated for a while as something to be disclosed: "this paper was prepared with LaTeX." None of them are disclosed any more, because they are now part of how the work is done.

AI coding assistants are at the LaTeX-circa-1990 stage of that transition. This post is not an apology for using one across eight weeks of this project; it is a description of the techniques that made the collaboration work. The disclosure here is incidental. The discipline is the point.

The Monday 23 March emails to Stephen are still the starting point, but for a different reason than I would have framed them a few months ago. They are not a confession. They are the first instance of a *technique*: name the instrument explicitly, before any work product changes hands.

> *Gemini and Claude did a great job in explaining QAOA to me and getting the kind of intuition that I needed for how it works.*

And a few hours later, the follow-on with the LLM-derived study notes:

> *Here are some study notes from my LLM sessions where I tried to fully understand and spec out the problem. I have a lot of maths to learn.*

Naming the instrument early sets a frame that everything downstream inherits. Once "I used these tools to come up to speed" is in the record, it never needs to be relitigated. The collaboration with Stephen could proceed at the level of *the work*, not at the level of *how the work was produced*. That is the first technique. The remaining techniques are about how to keep producing work, at conversational latency, for eight weeks.

---

## The shape of the practice

Three AI coding assistants, used at different times for different things: Gemini, Claude, and Copilot. Hundreds of hours of paired sessions across eight weeks. Not as autocomplete. Not as a search engine. As a counterpart that could read a paper, hold a derivation in working memory across a conversation, write a tested Julia module, diagnose a sign-flip pattern in a printout of a sixteen-element complex vector, and lose the thread completely the moment a session ended.

The session-end amnesia is the hard constraint. It is also where most of the technique lives. Everything in the rest of this post is, in one way or another, an answer to the question: *how do you sustain a research project across many sessions with a collaborator whose memory resets at the end of each one?*

The answer is that you build a persistence layer outside the collaborator's head and you make reading it the first step of every session.

## The persistence layer

Three artefacts, all load-bearing.

**The journal.** Thirty-two entries, about 1,800 lines of Markdown. Every significant decision, every wall, every bug, every benchmark. Written *as* we worked, not after the fact. Every session began with the assistant reading the latest journal entries to know what state the project was in. The journal is not for me; it is for the next session's instance of the assistant. Writing the journal is paying the cost of session-end amnesia up front, deliberately, in a form that can be reread cold.

**The learning materials.** Twelve documents, around 2,900 lines: explainers derived from the papers, written as artefacts of understanding rather than notes. `basso-iteration.md` describes the recurrence step by step. `charge-adjoint-derivation.md` is the full mathematical derivation of the charge backward pass. `python-to-julia-pitfalls.md` is the three translation bugs and how to avoid them. These are the project's shared vocabulary. When a new session opens and we are about to work on the charge adjoint, the learning material is what the assistant reads to *think in the right language*. It is not for the audience; it is for the next instance of the instrument.

**The test register.** 1,875 assertions across 22 files, from [Part 6](/2026/06/15/saturday-to-coauthor-06-eighteen-hundred-reasons.html). Each one is a fact about what correct behaviour looks like at a specific point. The tests are the project's memory of *how it has failed and how it knows it is no longer failing*. They are also the verification harness that converts confident-mistake risk (covered below) into discovered-bug certainty.

Nothing about this documentation infrastructure is exciting. None of it is the kind of artefact a paper cites. Together, the three artefacts are why an eight-week project run across hundreds of sessions did not lose coherence by the end of the second week. They are the form the *discipline* takes. The discipline is not "use AI carefully"; it is "build the externalised memory the AI does not have."

## The session protocol

Every working session followed roughly the same shape.

1. Open with a state read: "Read journal entries 28 through the latest. We're working on the charge adjoint today. What's the current state?"
2. Set the phase: "Today's goal is to land the Phase-2 backward without the `_replay_branch` redundancy."
3. Work in small, verifiable steps. Write a test; run it; read the failure; diagnose; fix; re-run. The cycle is short. The assistant is good at this cadence and bad at long horizons.
4. Close with a write back: "Summarise the day into a journal entry. Include: what we changed, what we measured, what is now broken, what is next."

This is not novel. Pair programmers have used some version of this protocol for decades. What is different is that the partner does not remember step 4 by the time we open step 1 of the next session. So the closing summary is not collegial; it is structural. It is the last instruction of each session and the first input to the next.

## Three conversational patterns that worked

### Translate from paper

The most reliable shape of progress in this project was: derive the mathematics on paper, then ask the assistant to translate the derivation into Julia and write the tests that anchor it.

The manual Basso adjoint was built this way. I worked through the WHT-is-self-adjoint observation and the log-derivative trick for the beta gradient with pen and paper. I described the derivation to the assistant a step at a time. The assistant wrote the Julia, wrote the ForwardDiff cross-validation tests, and iterated on sign conventions until the tests passed.

The charge manual adjoint followed the same pattern. Seventy-six tests written *before* the adjoint code existed; first run, 52 passing; three bugs diagnosed from specific failure patterns; three fixes; final run, 76 passing.

The technique is to keep the mathematics in a form the human can verify (paper) and the code in a form the assistant can write (tested Julia). Each partner does the half the other one is less suited to.

### Recognise and delegate

The second pattern is recognition-followed-by-implementation. I would notice a structural pattern. I would describe it. The assistant would build it cleanly.

- *Catamorphism recognition.* The branch-tensor recurrence is a fold over the light-cone tree. The recognition took an afternoon of conversation; the implementation took an hour.
- *The type parameter for ForwardDiff.* "This needs a `T <: Real` parameter." One sentence, two minutes of edits, three downstream payoffs that absorbed for free over the next month.
- *Plateau detection v4.* I had seen the same circular-buffer-with-$\max - \min$ pattern in streaming signal-detection problems years earlier. I proposed it. The assistant implemented it cleanly. That implementation cut the $p = 12, (3, 4)$ run from over two hours down to about forty minutes.

The recognitions are the human contribution. The clean, tested, idiomatic Julia code is the assistant's. Neither half is the work; both halves together are.

### Query the ecosystem

The third pattern is the one that surprises me most often. I describe a need; the assistant names the library that already meets it.

When I saw the cancellation failure at $(k = 6, D = 7)$ in [Part 4](/2026/06/08/saturday-to-coauthor-04-the-walls.html), my plan was to implement Kahan compensated summation by hand. The assistant said "Julia has a package for this," pointed me at `DoubleFloats.jl`, and showed me the one-line change. The fix landed in an hour. I would have spent a week.

This pattern is what library-shaped memory looks like as an instrument. It is the same affordance Google Scholar gave to literature search: not a different *kind* of search, just a larger working set. The assistant has read more of the ecosystem than I have. That is a research multiplier.

## The compiler as co-verifier

The three conversational patterns above produce code. The next question is how fast that code's mistakes get found, and the calculus on that question has fundamentally changed.

For thirty years the dominant input to "which language should we use?" was *human fluency*. Pick the language the team knows; pick the language with the ecosystem; pick the language the organisation supports. The producer of the code was a human, and the language's job was to be accessible to that human.

When a substantial fraction of the code is generated by an LLM, that input weakens. The LLM is fluent in every mainstream language. The dominant question shifts to *which language gives the producer the most feedback per generation cycle?* On that measure, strongly typed languages with rich type systems, first-class immutability, and exhaustive pattern matching are strictly better tools for AI-assisted work than dynamically typed languages. Haskell, OCaml, F#, Rust, and Julia sit on one side of that line; Python, JavaScript, and Ruby sit on the other. The reason is mechanical, not aesthetic.

A type signature is a machine-readable specification. When the assistant generates a function, the compiler reads the specification and rejects code that violates it, immediately and at the offending site. The rejection is short, precise, and addressed to the assistant's next turn. The more of the function's intent the type system can encode (parametric polymorphism, algebraic data types, immutability by default), the more classes of mistake are caught before any test runs. The LLM does not need to flag uncertainty for the compiler to catch it; the compiler does the work the LLM cannot.

A dynamically typed language defers all of that verification to runtime. The LLM's code is accepted; bugs surface diffusely, far from their cause, when a wrong-shaped value finally hits a runtime operation that rejects it. Every check that a static type system would have run as a precondition to any test now has to be a test the LLM thought to write. The LLM becomes the only source of correctness for everything between code generation and execution. That is more work, surfaced later, less precisely.

Stated plainly: *strongly typed, immutable-by-default, functional-leaning languages compress the LLM's feedback loop; weakly typed, mutable-by-default languages stretch it*. The team's prior fluency in the chosen language matters less than it used to, because the team is increasingly not the producer.

Julia is this project's instance of the principle. It is a multiple-dispatch language with parametric types and immutable structs as the default. When the assistant generates a function in Julia, the compiler is the first reader. A whole class of mistake (wrong-shaped argument, real-vs-complex method, unintended mutation) is rejected before any test runs, with a message that names the offending site.

The `QAOAAngles{T<:Real}` parameter from [Part 3](/2026/06/04/saturday-to-coauthor-03-three-gradients-in-one-codebase.html) is the canonical example for this project. When a method needed real-valued angles, the signature said so. When the same method later had to accept `Dual`-valued angles for ForwardDiff, the parameter carried them. Two weeks later, when the Double64 fix from [Part 4](/2026/06/08/saturday-to-coauthor-04-the-walls.html) landed, the same parameter carried `Double64` for free. Bugs of the shape *did you mean `Float64` or did you mean `Dual`?* never existed in the codebase, because the compiler enforced an answer at every function boundary. The assistant could not generate code that smuggled the wrong scalar type past the type checker; in a permissive language, the same assistant would have done so silently, and the bug would have surfaced at evaluation time as a numerical mismatch with no specific line attached.

The Python-to-Julia translation in [Part 7](/2026/06/18/saturday-to-coauthor-07-learning-from-the-masters.html) is the most direct evidence. Two of the three translation bugs (C-order vs F-order reshape, transpose vs adjoint) are exactly the shape of mistake Python's duck typing waves through: a tensor with the wrong stride pattern looks like every other tensor until the numerical output is wrong. The Julia rewrite did not catch those bugs at compile time either (memory layout is below the type system in both languages), but the search space had already been compressed by the parametric kernels rejecting every wrong-typed input at the boundary; the convention bugs were localised to the few sites where the convention crosses. The Python team had to find them by debugging; the Julia rewrite localised them by typechecking.

The implication is worth stating in plain English, because it changes the answer to a question many teams are now asking. *If a substantial fraction of new code is going to be written in collaboration with an LLM, the language calculus now favours strongly typed, immutable-by-default, functional-leaning languages, on the merits of the producer's feedback loop, independent of the team's prior fluency.* This is not a holy war between languages; it is an engineering observation about which producer is doing the writing. Julia gave this project that. F# would have. Haskell would have. Rust would have. Python would have made the same work measurably harder, by exactly the amount the compiler is currently absorbing.

The compiler, in that specific sense, is a third member of the collaboration. The assistant proposes; the compiler disposes; the test harness then checks the parts the compiler cannot.

## The verification harness

There is one risk that the conversational style introduces and one discipline that converts it back into an asset.

The risk is *confident mistakes*. AI assistants do not have self-doubt. They produce code that looks right, prose that reads well, and attributions that sound authoritative, and they do not flag any of it as uncertain. The Wirtinger sign-flip in [Part 7](/2026/06/18/saturday-to-coauthor-07-learning-from-the-masters.html) is the type example: a confident, well-written, plausible-looking adjoint that happened to be wrong on a sign, and that passed every test that did not exercise complex inputs.

The discipline is the verification harness. Three layers of it.

- *Tests at $p \geq 2$.* Most translation bugs were invisible at $p = 1$ (the trivial case) and surfaced at $p = 2$ (the first non-trivial case). The pattern of always testing past the trivial regime was the single highest-leverage habit in the entire project.
- *Cross-evaluator congruence.* Two independent implementations agreeing to ten decimal places is much stronger evidence than one implementation passing its own tests. The Basso and charge evaluators were each other's harness. [Part 7](/2026/06/18/saturday-to-coauthor-07-learning-from-the-masters.html) is the technique applied at scale.
- *External truth.* Some claims have to be checked against sources outside the conversation. Code can be checked against tests; citations, dates, attributions and quotations have to be checked against the actual record. Every regular user of large language models has encountered the moment when a confidently asserted name, paper title, or formula turns out to be clean fiction. This is not a peculiar failure mode of any particular model; it is a property of the technology, and it does not get better by hoping. The discipline is the same as for code: do not accept a confident output until it has been checked against an external source. For a citation, the source is a literature search. For an attribution, an email thread or an institutional page. For a quotation, the original document. The check costs seconds; the absence of the check costs reputation. Over the course of drafting this series the harness caught the kind of mistake one would expect: a misattribution in an early draft of [Part 7](/2026/06/18/saturday-to-coauthor-07-learning-from-the-masters.html) that did not survive contact with the actual email record. The cure is the discipline, not vigilance against any one model.

The harness does not make the confident-mistake problem go away. It catches the mistakes after they are made. That is sufficient. It is also the discipline that makes the rest of the conversational style safe to use at speed.

## What the human does

The shape of the human contribution, after eight weeks of this:

- *Direction.* "Let us study QOKit next." "Spend a week on memory fixes for the Mac rather than running on a bigger machine." "Abandon the Metal GPU spike." None of these decisions could have been delegated. The assistant executes; it does not strategise.
- *Judgement.* "Four-and-a-half times forward is the number we ship." "The charge adjoint at this allocation profile is good enough; the gap to the published C++ implementation is not worth another week of pool work." Judgements of *good enough* are not in the assistant's frame. They are not a kind of mistake the assistant makes; they are a kind of decision it does not make.
- *Taste.* "We are not stealing from JPMorganChase; we are learning from colleagues. Reframe accordingly." The taste in framing is a craft skill. It is the most enjoyable part of the collaboration and the part that is most clearly human.
- *External-truth verification.* The discipline above.
- *The physics.* I am not a physicist; neither is the assistant. The assistant knows enough quantum mechanics to discuss it competently; it does not know which framework applies to a new problem until someone tells it. In this project, that someone was Stephen.

None of these is an absence. Each is a positive contribution. The shape of the human role in this project is not "what the assistant could not do"; it is *direction, judgement, taste, verification, and the physics*. The instrument extends; the conductor still conducts.

## Productivity at conversational latency

Eight weeks from the Saturday-morning email to a co-author position on a Google Quantum AI arXiv submission. Four weeks more to $p = 14$ on a Mac. Twenty-two test files. Five compute environments. Fifteen $(k, D)$ instances of Max-$k$-XORSAT in Phase 1. Three depths of MaxCut at $p = 14$ in Phase 2.

My honest estimate of the same work without the instrument: about six months, and probably a different shape of project. Less ambitious in test coverage. Less aggressive in the Python-to-Julia rewrite step from [Part 7](/2026/06/18/saturday-to-coauthor-07-learning-from-the-masters.html). Almost certainly without the diagnostics module from [Part 8](/2026/06/22/saturday-to-coauthor-08-fourteen.html), which exists because writing it cost twenty minutes, which is the threshold at which "yes, let's write it" stops being a question.

The interesting reading of that speedup is not "the assistant did most of the work." It is that *research operations are now paced by thinking, not by typing*. The time from "I see what to do" to "the code does it" used to include an hour of typing, an hour of looking up library signatures, an hour of writing tests. With the instrument, that loop runs at the speed of dictation. The cost of an experimental excursion drops by an order of magnitude. The number of excursions you can afford goes up by the same factor. Some of them succeed. The work compounds.

This is the same shape of speedup that LaTeX gave to mathematical typesetting and that version control gave to code. It does not change *what* the work is. It changes how much of it a single person can do in a week.

## The new instrument, the old practice

Nobody asks "did you write this paper, or did LaTeX?" The instrument is part of the practice; the discipline that distinguishes good work from bad is the discipline in *how the instrument is used*. The same will be true of AI coding assistants once enough people have stopped treating their use as a thing to be disclosed and started treating their effective use as a craft skill.

The craft skill, on the evidence of this project, is the persistence layer (journal, learning materials, tests), the session protocol (read, set phase, work in cycles, summarise back), the three conversational patterns (translate from paper, recognise and delegate, query the ecosystem), and the verification harness (tests at $p \geq 2$, cross-evaluator congruence, external truth). That is the whole technique.

The work in this series is what it is because of those techniques, not in spite of them. The assistant did not author the result. Neither did Julia. Neither did the test framework. The result was authored by the practice in which all of these instruments were used, in which a human collaborator on another continent supplied the problem and the validation, and in which a person wrote the journal entries and chose the experiments. That practice has a name. It is *research*. The instruments are new; the practice is the same.

---

_Next: **What language taught us about mathematics**, the closing reflection on eight weeks of building this engine in Julia, with this collaborator on a continent away and this assistant on every laptop in the project._

_Code: [github.com/johnazariah/qaoa-xorsat](https://github.com/johnazariah/qaoa-xorsat). This post, like all the others in the series, was drafted in conversation with an AI assistant in my voice from my outline. The persistence layer for the series is the FACTS document; the session protocol is the same one the project used; the verification harness is the same email thread that caught a misattribution mid-draft._
