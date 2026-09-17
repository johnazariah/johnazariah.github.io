# Speaker cues — Why Learning to Code Properly Still Matters in the Age of AI

For Friday 18 September 2026. Target: 33 minutes within a 35-minute talk, with Q&A afterwards. Timings are rehearsal targets, not measured durations. Slides 14–18 are Q&A backups.

These cues are embedded in the PowerPoint speaker notes. The full script remains in `2026-05-08-the-craft-isnt-dead-talk.md`.

## 1. Several thousand lines of responsibility

TIME: 00:00–03:00 | 3 minutes

- Most of us already use AI. Grant its usefulness; this isn't a case for stopping.
- Sometimes we describe an idea and acquire “several thousand lines of responsibility”.
- Ask: if implementation takes minutes, what are our skill and experience worth?
- No promise that humans are uniquely special or that expertise guarantees job security.
- Focus on understanding that changes the result: what we notice, make explicit, and need to see before trusting it.

DELIVERY: Pause after the central question. No adoption poll or celebrity quotes.

TRANSITION: “Here is a method that looks perfectly familiar. Let's make one thing go wrong.”

## 2. The payment succeeded. What failed?

TIME: 03:00–06:00 | 3 minutes

- Walk through stock → price → charge → reserve → confirmation.
- Point to Charge, then Reserve. The charge succeeds; reservation fails. What happened to the order?
- The customer has paid. Returning “failed” doesn't undo that.
- Refund, retry, manual attention? What survives a process crash?
- A payment timeout may mean we failed to hear about success—not that payment failed.
- Moving reservation earlier raises holds and expiry questions; it isn't a universal fix.
- AI can identify these risks too. A suggestion doesn't establish the provider's guarantees.

DELIVERY: Allow about 30 seconds to read the code; take one brief response. Acknowledge the stock race. Don't design a complete saga.

TRANSITION: “So what, precisely, did the experienced engineer contribute?”

## 3. What experience notices

TIME: 06:00–08:00 | 2 minutes

- Experience helps select consequential questions, not just list everything that could fail.
- Keep the payment-key example: preserve one logical attempt's key across retries.
- The key works only under the provider's actual guarantees.
- Move that knowledge into the request model, adapter contract, and integration checks—not just a review comment.
- Now the knowledge survives your holiday and the next generated change.
- AI can help implement and challenge it. “I've always done it this way” isn't evidence either.

DELIVERY: Don't read every table cell. This is a shortened section; retain the concrete example.

TRANSITION: “To make that idea visible, I need a much less expensive customer.”

## 4. A brief frog

TIME: 08:00–10:00 | 2 minutes

- Jump, croak, jump, eat a fly. One sequence, different interpretations.
- Narrative describes the actions; simulation changes the frog's state.
- The interpreter also supplies how results combine: text accumulation isn't state composition.
- We don't copy the adventure to obtain another interpretation.
- This is the intuition behind tagless-final. DI is a familiar starting point, not an enemy.
- The frog is deliberately simple. A payment needs data from an earlier operation.

DELIVERY: DSL sketch; builder omitted. No implementation detour.

TRANSITION: “The frog doesn't have a payment gateway. Let's put the data back.”

## 5. The frog doesn't have a payment gateway

TIME: 10:00–13:00 | 3 minutes

- Pricing produces a price result; charging consumes a payment request.
- Selecting the total and building that request are pure data transformations, not external operations.
- Sequencing makes an effectful result available to the next computation.
- Connect to familiar await/query syntax. “Bind” names the general sequencing operation here; it isn't a membership test.
- Keep domain vocabulary, pure transformations, and sequencing distinct.
- A dry run still needs coherent results for later decisions. It must model them or stop explicitly.

DELIVERY: Follow the diagram left to right. Don't imply we can directly unwrap an asynchronous result.

TRANSITION: “What do we get for doing that work?”

## 6. Write the decision once

TIME: 13:00–16:00 | 3 minutes

- Read K<F, A> as “an A produced in interpretation F”.
- Follow price into ChargePayment. The workflow stays unchanged.
- Id returns a value; Trace also records operations.
- Important limitation: this tiny example doesn't reject unavailable stock. Calling CheckStock is not the same as using its result correctly.
- Benefits: isolated decisions, traces, and a bounded interface for another interpreter.
- Costs: design and maintain the vocabulary, result meanings, and interpretations.
- A disposable script may not need this abstraction. Choose it for a concrete benefit.

DELIVERY: Prepared code walkthrough, not a live execution claim. The sample does not implement the free/tagless round-trip or production payment recovery.

TRANSITION: “One of those interpretations can build a program as data. But is it really the same program?”

## 7. Two representations. What survives the journey?

TIME: 16:00–18:00 | 2 minutes

- Explicit syntax describes the program as data; a tagless program uses an interpreter's operations.
- Interpret syntax to go one way; choose a syntax-building interpretation to recover a representation.
- Condition: the program must behave uniformly across interpretations, not secretly recognise a particular interpreter.
- A generic C#/TypeScript type parameter alone doesn't establish that.
- Recovery is up to the stated equality: equivalent meaning need not mean identical original syntax.
- Faithfully translate a program that charges twice and you still have a program that charges twice.

DELIVERY: Walk both arrows, briefly. No proof. Leave normalisation machinery and hidden-continuation details to Q&A. Don't identify tagless-final with a final coalgebra.

TRANSITION: “I didn't make all those distinctions carefully enough when I first wrote about this.”

## 8. My explanation needed debugging too

TIME: 18:00–20:00 | 2 minutes

- I wrote about intent and process; Mitchell Wand asked questions that made the account more precise.
- I had put too much weight on “the structure guarantees it”.
- Which structure? Under which assumptions? Guarantees what?
- Correcting the explanation preserved the useful idea while narrowing claims about inspection and optimisation.
- Separate “compiles”, “passes these tests”, “preserves this property”, and “fits the domain”.
- AI can help challenge an explanation. Confidence still needs to match the evidence.

DELIVERY: Own the correction. Keep the correspondence story short. No private quotations, novelty claim, or claimed endorsement.

TRANSITION: “Let's put that distinction to work on an optimisation an agent might reasonably suggest.”

## 9. May we remove the second read?

TIME: 20:00–23:00 | 3 minutes

- Two calls, same arguments: reuse the first result. A plausible proposal.
- A stub always returns twelve; both versions agree in that case.
- Live stock changes: twelve, then eleven. Reuse changes what the program observes.
- Other observations may matter too: failure, audit events, quotas.
- One immutable snapshot gives a different argument—but only under the stated observable-behaviour contract.
- The service contract supplies the condition, not the operation's name or the monad laws.
- AI can propose the counterexample too. Use it; don't accept an assumed snapshot we don't have.

DELIVERY: Ask “What would make this safe?” Take one answer, then contrast the contracts. Illustrative transformation, not a completed optimisation demo.

TRANSITION: “Now we can give the agent a much better task than ‘optimise this service’.”

## 10. Give the agent something worth preserving

TIME: 23:00–26:00 | 3 minutes

- Goal: reduce repeated stock lookups within one order evaluation.
- Boundary: one explicit immutable snapshot; no cross-snapshot reuse or payment-path changes.
- Evidence: live-read counterexample, same-snapshot reuse, cross-snapshot separation, relevant outcomes and traces.
- Unknowns: identify guarantees the repository cannot establish.
- If no snapshot abstraction exists, report it. Renaming mutable data doesn't create one.
- Leave implementation choices open, but keep the acceptance conditions.
- Put discoveries into APIs, tests, and a decision record so the next engineer or agent inherits them.

DELIVERY: Give the room time to read the brief. At most one brief audience response. This is a proposed task, not a report of an agent run.

TRANSITION: “How much of that process do we need every time?”

## 11. Delegate according to consequence

TIME: 26:00–28:00 | 2 minutes

- Exploration with synthetic data and no production access can be the right approach.
- “Temporary” isn't a risk classification: a one-off migration can damage production exactly once.
- Choose autonomy by what the code can affect, the cost of failure, and our ability to observe and recover.
- Higher consequences need narrower boundaries and stronger evidence—not simply more ceremony.
- Several agents sharing one mistaken assumption don't supply independent assurance.
- Vibe coding and structured development needn't be rival identities. Recognise when an experiment becomes consequential.

DELIVERY: Keep the migration example; shorten the general process discussion.

TRANSITION: “So if you're deciding where to invest your own learning, where does that leave us?”

## 12. Keep learning where the mistakes are expensive

TIME: 28:00–31:00 | 3 minutes

- Learn how programs behave: follow state, effects, transactions, retries, and failures.
- Understand what a type or test rules out—and what it doesn't.
- Seek a counterexample to your belief rather than just more tests confirming it.
- Learn the domain: accepted orders, cancellation, prices, genuinely reversible actions.
- Use AI to explain and compare; follow its claims into the implementation.
- Preserve feedback: predict before running, trace before accepting a patch, sometimes build the small version yourself.
- Give juniors bounded ownership and consequences to investigate, not just generated code to approve.
- Nobody needs a DSL for everything. A pure function, useful type, and boundary test may be enough.

DELIVERY: Start the closing slide at minute 31. If behind, cut elaboration here rather than rushing the close.

TRANSITION: “Let's return to the question we started with.”

## 13. What your experience buys

TIME: 31:00–33:00 | 2 minutes; two minutes of slot headroom

- Return to the opening: if AI writes the method, what did learning buy us?
- The ability to reason about results, effects, failures, and evidence applies to code we didn't type.
- Use it to choose representations, set agent contracts, recognise assumptions, and preserve lessons from failures.
- Some old work will disappear. Don't defend it merely because it took years to learn; don't promise job insulation.
- My own explanation needed correction. Expertise includes remaining correctable.
- Use AI, explore with it, and learn with it. Keep developing the understanding needed to judge the result.

CLOSE: “The code may arrive in seconds. Understanding still has to be earned.”

DELIVERY: Stop after that line. Leave the slide on screen for Q&A; don't add another summary.

## 14. Is this just dependency injection?

Q&A BACKUP — outside the timed talk

- Yes, there is a family resemblance: abstract operations and substitute implementations.
- The extra structure here abstracts how computations compose, not just which service implements each call.
- Different carriers can support traces or syntax-producing interpretations.
- That flexibility costs design and maintenance effort.
- Ordinary DI and pure functions may be sufficient. Mathematical vocabulary should explain a useful distinction, not decide who belongs.

## 15. What the round-trip does—and does not—say

Q&A BACKUP — outside the timed talk

- Requires a specified language, models, equality, and uniformity across interpretations.
- Relates a program's representations; it does not establish business correctness or arbitrary rewrite safety.
- The request is an open context, not just one sample input.
- Equality is at the stated equivalence-class level; normal-form recovery needs the appropriate additional assumptions.
- Host-language generics alone don't enforce the whole condition.
- Host-language continuations can hide future structure. A free-monad encoding does not automatically expose every path.
- The small C# example is a composition demonstration, not an implementation proof of this theorem.

## 16. When generation outruns review

Q&A BACKUP — outside the timed talk

- AI can find architectural risks and counterexamples too. That's a reason to use it.
- Don't make an unbounded code dump the unit of trust. Ask for smaller changes with clear interfaces.
- Automate the properties checks can establish; focus review on consequential assumptions.
- “Payments are idempotent” in a specification doesn't make the provider honour it.
- Agreement among agents is weaker than genuinely independent evidence if they share an assumption.
- If generation exceeds our ability to establish confidence, generate less at once.

## 17. Help colleagues develop judgement

Q&A BACKUP — outside the timed talk

- Predict behaviour before running code; investigate the failure before accepting the patch.
- Ask AI for competing designs and examine their assumptions.
- Give juniors manageable ownership, real feedback, and room to learn from consequences.
- Don't replace apprenticeship with an approval queue.
- Security, operations, and performance need their own evidence: trusted inputs, permissions, load assumptions, failure signals.
- This workflow illustrates a habit of reasoning; it isn't a substitute for those disciplines.

## 18. Further reading

Q&A BACKUP — outside the timed talk

- Froggy introduces multiple interpretations; the algebra article gives the original C# framing.
- The Wand follow-up records early corrections, not the entire later technical account.
- Earlier public material contains broader claims than this talk. Don't present it as blanket authority for every current claim.
- No novelty claim or blanket endorsement by Mitch. His questions and review sharpened my understanding.
- Private correspondence, proof notes, and contributed code are not public attachments.
- Point people to the linked public reading; distinguish it from unpublished working material.
