# Why Learning to Code Properly Still Matters in the Age of AI

**Speaker:** John Azariah

**Audience:** Software-engineering colleagues already using AI, from exploratory vibe coding to structured agent workflows

**Format:** 35–40 minutes, followed by Q&A

**Status:** First full speaker draft, revised 15 September 2026. Not yet rehearsed; timings are targets.

**Working subtitle:** What our experience buys us when implementation gets cheaper.

**Materials:** [Editable PowerPoint deck](2026-05-08-the-craft-isnt-dead.pptx) · [PDF slides](2026-05-08-the-craft-isnt-dead-slides.pdf) · [Speaker cues](2026-05-08-the-craft-isnt-dead-speaker-notes.md) · [Organiser abstract](2026-05-08-the-craft-isnt-dead-abstract.md)

**Tomorrow's delivery (18 September):** Confirmed 35 minutes, with Q&A afterwards. Use the 33-minute route in the deck's speaker notes and linked speaker cues. The full script and original 37-minute running order below remain reference material.

## The talk's promise

This is not a case against AI-assisted development, or an argument that everyone must learn category theory. It answers a question our colleagues are already asking: **if AI can do so much of the implementation, what is the value of our skill and experience?**

The answer is practical. Experience helps us recognise consequential assumptions, choose useful representations, and establish what evidence would justify shipping a change. We can make that understanding durable in code, contracts, tests, and operational safeguards. AI can help with all of this; our contribution is not confined to supervising its typing.

The order workflow is the running example. Froggy makes interpretation intuitive. The later Intent vs Process work supplies the useful distinction between changing a representation and justifying a change in behaviour. The correspondence informs the talk without turning it into a research memoir.

## Abstract

AI can produce useful software in minutes. For engineers who spent years learning to build it, that raises a reasonable question: what are our skill and experience worth now?

Starting with an ordinary order-processing workflow, we'll explore how understanding changes what we can ask AI to do—and what evidence we need before trusting the result. We'll see how a better representation opens up useful possibilities, why a plausible optimisation can still be wrong, and what I had to revise in my own explanation of these ideas.

This isn't a case against AI-assisted development, or a demand that everyone learn category theory. It's a practical look at how engineering experience earns its keep when implementation gets cheaper, and what is still worth learning.

## Running order

| Slide | Time | Title | Purpose |
|---|---|---|---|
| 1 | 00:00–03:00 | Several thousand lines of responsibility | Ask their question; grant AI's usefulness |
| 2 | 03:00–06:00 | The payment succeeded. What failed? | Put a consequential ambiguity on screen |
| 3 | 06:00–09:00 | What experience notices | Make the value of engineering judgement concrete |
| 4 | 09:00–11:00 | A brief frog | Explain one program, different interpretations |
| 5 | 11:00–14:00 | The frog doesn't have a payment gateway | Return to real data dependencies |
| 6 | 14:00–17:00 | Write the decision once | Show what a useful separation buys |
| 7 | 17:00–20:00 | Two representations. What survives the journey? | Introduce the round-trip and its boundary |
| 8 | 20:00–23:00 | My explanation needed debugging too | Model learning and correction, not infallibility |
| 9 | 23:00–26:00 | May we remove the second read? | Separate a plausible optimisation from a justified one |
| 10 | 26:00–29:00 | Give the agent something worth preserving | Turn experience into a concrete agent task |
| 11 | 29:00–32:00 | Delegate according to consequence | Give structured and exploratory AI users a common method |
| 12 | 32:00–35:00 | Keep learning where the mistakes are expensive | Answer what to practise now |
| 13 | 35:00–37:00 | What your experience buys | Close on the audience's question |

**Delivery:** 37-minute target, three minutes of headroom in a 40-minute slot. For a 35-minute slot, use the cuts below to target 33 minutes. Q&A is additional.

**Slide discipline:** Titles and the specified visual only. The prose below is spoken material, not slide copy. Code is revealed in small pieces. Examples marked illustrative are teaching excerpts, not claims of a complete, executable implementation.

---

## 1. Several thousand lines of responsibility

**00:00–03:00**

**On screen:** The title, then: **What does our experience buy us now?**

### Spoken

Most of us in this room are already using AI to write software.

Sometimes it's autocomplete. Sometimes it's a collaborator. Sometimes we describe what we want, let it run, and discover we've acquired several thousand lines of responsibility.

And it's useful. This isn't a talk about why we should stop.

I've used it for exploration, for implementation, and for working through ideas I couldn't yet explain properly. Sometimes it gets me unstuck. Sometimes it produces an answer so plausible that noticing what's wrong with it takes rather more understanding than producing it did.

Both experiences matter.

But there's a reasonable question underneath the excitement. If a tool can produce in minutes something that used to take us days, what was the value of learning to do it ourselves?

And if we're helping somebody learn this profession now, what should we ask them to spend their time learning?

I don't think “don't worry, humans will always be special” is a useful engineering answer. Nor do I think we can declare everything we've learned obsolete because a model can generate a service class.

Some of what we know is becoming cheaper to obtain. Remembering the exact incantation for an API is less valuable when the tool can find it, explain it, and try it. That's a real change. We should let it change how we work.

What I want to examine is the understanding that changes the result.

Not how much code you personally typed. What you noticed about the problem. What you made explicit. What you needed to see before you were willing to trust the answer.

Let's start with something thoroughly ordinary.

### Delivery

Pause after the audience question. Don't begin with a show-of-hands exercise about AI adoption; the talk assumes they already use it. No celebrity-quote montage.

**Transition:** “Here is a method that looks perfectly familiar. Let's make one thing go wrong.”

---

## 2. The payment succeeded. What failed?

**03:00–06:00**

**On screen:** An illustrative C# excerpt, initially without commentary.

```csharp
var stock = await inventory.CheckStock(request.Items);
if (!stock.IsAvailable)
    return OrderResult.Failed("Out of stock");

var price = pricing.Calculate(request.Items, request.Coupon);
var charge = await payment.Charge(request.PaymentMethod, price.Total);
if (!charge.Succeeded)
    return OrderResult.Failed("Payment failed");

await inventory.Reserve(request.Items);
await email.SendConfirmation(request.Customer, price);
return OrderResult.Success(charge.TransactionId);
```

**Reveal:** Highlight `Charge`, then `Reserve`.

### Spoken

Check stock. Work out the price. Charge the customer. Reserve the goods. Send confirmation.

It's recognisable code. We could write it; AI could write it. There are sensible abstractions here. The presence of interfaces is not the offence.

Now suppose the charge succeeds and the reservation fails.

What has happened to the order?

**[Let the room think for a few seconds.]**

The customer has paid. We may not have the goods. Returning “failed” doesn't undo the payment.

Should we refund? Retry the reservation? Create an order requiring manual attention? And if the process dies before recording which of those it chose, what happens when it starts again?

There's a related problem a line earlier. A payment request times out. Did the payment fail—or did we fail to hear that it succeeded?

Those are different situations. The customer would prefer us to notice.

We cannot settle them by making this method prettier. Moving the reservation earlier may be part of a solution, but it raises its own questions about holds, expiry, and abandoned orders. There is a business policy to choose and a distributed system to understand.

This is where experience begins earning its keep. You've seen a timeout that wasn't a failure. You've seen a retry make a bad situation twice as expensive. You know that “the test passed” is incomplete information until you know what the test exercised.

An AI assistant can notice these things too. Ask it to challenge the workflow; that is a useful thing to do.

But a generated suggestion doesn't establish what our payment provider guarantees, or what our business has promised its customers. We need evidence for that.

### Delivery

Treat audience objections as support for the example. If someone notices the stock race, acknowledge it: “Exactly. That is an assumption this method leaves us to investigate.”

Allow about 30 seconds for the room to read the method and another 20–30 seconds for one response to the failure question. Don't attempt to resolve every proposed recovery policy.

Do not claim the code would pass every review, or that the talk will implement a complete order saga.

**Transition:** “So what, precisely, did the experienced engineer contribute?”

---

## 3. What experience notices

**06:00–09:00**

**On screen:** Build a small decision record.

| What we notice | What we must establish | Where it should survive |
|---|---|---|
| A timeout can hide a successful charge | Provider retry and reconciliation behaviour | Payment adapter contract and integration checks |
| Stock can change after a lookup | Reservation and consistency policy | Workflow rules and failure-path tests |
| Confirmation delivery can fail independently | Whether the order remains successful | Explicit outcome and delivery policy |

### Spoken

Experience isn't just having a longer list of things that can go wrong. If that were the whole job, we'd be exceptionally expensive pessimists.

It helps us decide which questions matter here.

For a disposable experiment, a rough answer may be enough. For a service that moves money, ambiguity about retries deserves considerably more attention.

It also helps us decide where the answer belongs.

If I know the same logical payment attempt must reuse an idempotency key, I can mention it in a review comment. That's useful once.

If we encode it in the request model, preserve it across retries, check the adapter behaviour, and keep an integration test against the provider's guarantees, the knowledge can survive me being on holiday.

That's a much more valuable use of experience than repeatedly rescuing the same codebase.

Notice that none of those steps requires us to exclude AI. It can help write the adapter and the tests. It can propose the request model. It can point out that our original test doesn't exercise the timeout window.

The question is whether our working process turns those discoveries into something durable.

And we have to apply that question to ourselves. “I've always done it this way” isn't a guarantee either. Experience can teach us useful patterns; it can also make our assumptions harder to see.

What matters is whether we can explain the decision, identify the evidence behind it, and revise it when the system behaves differently.

Now there's an architectural opportunity here. If important decisions and execution details are tangled together, every change asks us to rediscover the boundary. Could we make that boundary easier for both us and the agent to work with?

**Transition:** “To make that idea visible, I need a much less expensive customer.”

---

## 4. A brief frog

**09:00–11:00**

**On screen:** The frog program from the original series, presented as a DSL sketch with its builder omitted.

```fsharp
let adventure = frog {
    jump
    croak
    jump
    eat_fly
}
```

**Reveal:** Two labelled interpretations of the same four operations:

- **Narrative:** describe each action.
- **Simulation:** update a small model of the frog's state.

### Spoken

This is a tiny program from a series I wrote. Jump, croak, jump, eat a fly.

The useful trick isn't the frog. Although I remain fond of the frog.

The program names the actions. We supply a separate interpretation of those actions.

One interpreter tells a story. “The frog jumps. The frog croaks.” Another updates a simulation: jumping changes height; eating changes hunger.

Same sequence. Different meanings.

In this design, the interpreter also supplies the way those results are combined. We aren't assuming that appending narrative text and composing state changes are the same operation.

That separation gives us options without copying the adventure into several implementations.

This is the basic intuition behind the tagless-final approach: write the program against an abstract vocabulary and let an interpreter give it meaning.

If you already use dependency injection, parts of this will feel familiar. Good. We are extending a useful idea, not pretending everyone else's code has no structure.

There is a limit to what this example establishes. The frog's actions are deliberately simple. An order workflow needs the price from one operation to construct the payment in another.

That dependency is where the next interesting question lives.

### Delivery

No interpreter-record implementation on this slide. The audience needs the separation before it needs the encoding.

**Transition:** “The frog doesn't have a payment gateway. Let's put the data back.”

---

## 5. The frog doesn't have a payment gateway

**11:00–14:00**

**On screen:** Three labelled regions, revealed along the order workflow.

```text
Domain operation       Pure data transformation       Domain operation
CalculatePrice    →     price.Total / make request  →  ChargePayment

                 Sequencing connects the computations
```

**Caption:** The result becomes available through the chosen sequencing mechanism; the diagram is not a direct unwrap operation.

### Spoken

Calculating a price gives us a price result. Charging a payment needs a payment request.

Between those two operations, something reads the total and constructs the request. That is ordinary data transformation. It isn't a new external operation just because it sits between two of them.

There is another piece of work as well. The payment depends on the result of pricing. If pricing is asynchronous, we cannot pretend the price is already sitting there.

We need a way to say: perform this computation, and use its result to determine the next computation.

You already know that idea through `await`, or through a query expression that feeds one result into the next step. In the model we're discussing, the general sequencing operation is called bind.

That name is useful because it lets us find the relevant theory and reuse implementations. It isn't a password we need to pronounce before our code becomes respectable.

The distinction is what matters. Charging is domain vocabulary. Selecting the total is a pure transformation. Connecting computations is sequencing machinery.

Once we separate those responsibilities, we can ask a better question than “can this service be mocked?”

Can the same workflow be given a deterministic test interpretation? An interpretation that records a trace? One that constructs an explicit program representation rather than immediately calling the outside world?

Each of those interpretations needs a coherent account of the results the workflow consumes. A dry run can't simply return “payment omitted” if the next decision depends on a transaction result. It must model that result or explicitly stop at the point it cannot justify continuing.

This is design work. AI can help us do it, but the vocabulary and result types have to express the problem we actually have.

**Transition:** “What do we get for doing that work?”

---

## 6. Write the decision once

**14:00–17:00**

**On screen:** A short excerpt from the research example, with `K<F, A>` glossed as “an A produced in interpretation F”.

```csharp
public static K<F, string> PlaceOrder<F>(string items)
    where F : OrderAlgebra<F> =>
    from stock  in F.CheckStock(items)
    from price  in F.CalculatePrice(items)
    from charge in F.ChargePayment(price)
    select charge;
```

**Caption:** A deliberately small composition example. It does not implement stock rejection, real pricing, or payment recovery.

**Reveal:** The sample runs the same workflow with `Id` and `Trace`. One returns a value; the other returns a value together with operation-log entries.

### Spoken

This is the small C# version of the idea. The full plumbing is in the example code, not on the slide.

`F` tells us which interpretation we're using. The query reads as a sequence of operations, with the price flowing into the charge.

The sample has one interpretation that returns ordinary values, and another that also records which operations occurred. We don't rewrite the workflow to obtain the trace.

It's intentionally not a production order system. In particular, this little version doesn't reject unavailable stock. The presence of a method called `CheckStock` is not proof that the program does anything sensible with its result.

That is worth noticing. A vocabulary can make intent easier to express without automatically making the expressed intent correct.

What we've gained is a place to put the workflow and a controlled way to vary its meaning.

That can reduce the amount of infrastructure a test needs to exercise a decision. It can make an execution trace easier to obtain. It gives us a smaller, more explicit interface against which to ask an agent to implement a particular interpretation.

There is a cost. Someone must design that interface, decide what the results mean, and maintain the interpretations. For a ten-line disposable script, that may be a poor bargain.

For a workflow with important policies and several reasons to interpret it differently, it may be an excellent one.

That's an architectural judgement. “Always use this abstraction” would just be another rule to memorise.

### Delivery

Default to a prepared walkthrough, not terminal typing. The executable sample is identified in the preparation notes. It must be run and its output captured before being presented as a demonstrated run. Don't display a production label over a toy interpreter.

Reserve 45–60 seconds to follow `price` into `ChargePayment`, then compare the value-only and traced interpretations. Point to the unchanged workflow while switching the output. This is explanation time, not extra implementation detail.

**Transition:** “One of those interpretations can build a program as data. But is it really the same program?”

---

## 7. Two representations. What survives the journey?

**17:00–20:00**

**On screen:**

```text
Explicit program syntax
        ⇅
Program expressed uniformly through interpreters
```

**Reveal:** **Preserving the program is not the same as proving the program right.**

### Spoken

There are two ways we've been describing the workflow.

One is an explicit program representation: instructions and the structure connecting them.

The other is a program expressed through an interpreter's operations.

It's tempting to draw a double-headed arrow between them, announce they're equivalent, and move on to the exciting optimisations.

But the arrow is a claim. What exactly survives the journey?

The later work behind this talk asks that question carefully. Under the required conditions, we can interpret the explicit syntax through the abstract interface, and recover its program representation by choosing an interpreter that builds syntax.

The other direction needs an important restriction: the abstract program must behave uniformly across interpretations. It must not secretly recognise the interpreter and choose unrelated behaviour for that particular case.

A generic type parameter in C# or TypeScript doesn't, by itself, establish that entire condition.

There is another qualification when the language gets richer. We may recover an equivalent normalised program rather than the exact original source. Preserving meaning and preserving spelling are different claims.

This round-trip is useful because it explains how convenient program construction and an explicit representation can belong to the same design.

It does not show that every future branch is available for inspection before results exist. Host-language continuations can still hide structure. And it certainly does not show that the workflow meets the business requirement.

If we faithfully translate a program that charges twice, we have a second representation of a program that charges twice.

The theorem did its job. We still have ours.

### Delivery

Explain the conditions in ordinary language. No theorem proof or category-theory taxonomy in the main deck. Do not identify tagless final with a final coalgebra, or invoke Lambek's Lemma to justify this round-trip.

Spend about 30 seconds walking each direction of the diagram. Ask what a syntax-building interpreter would return, then reveal the answer. Don't claim the C# sample on the previous slide implements this conversion.

**Transition:** “I didn't make all those distinctions carefully enough when I first wrote about this.”

---

## 8. My explanation needed debugging too

**20:00–23:00**

**On screen:** **What exactly have we established?**

**Reveal:** “Compiles”, “passes these tests”, “preserves this property”, and “fits this domain” as separate claims—not a ladder of automatic implications.

### Spoken

I published a series about separating intent from process. Then Mitchell Wand started asking questions about it.

I've written publicly about the first part of that exchange. The later work made the explanation considerably more precise.

It also made it less comfortable to wave at some mathematics and say “the structure guarantees it”.

Which structure? Under which assumptions? Guarantees what?

I had put too much weight on statements that needed more work. Domain operations and the machinery connecting them weren't being distinguished carefully enough. Some claims about what we could inspect or optimise were broader than the representation justified.

Working through those questions didn't destroy the useful architectural idea. It gave it boundaries I could explain.

That's an important part of the answer to today's question. Learning to code properly isn't acquiring a certificate that says you now recognise all bad code. It includes learning how to discover that your own explanation is incomplete.

And AI doesn't make that process irrelevant. It gives us more material to interrogate, often much faster.

If it generates an implementation and a convincing explanation, we should be able to separate the claims. The compiler has checked certain properties. The tests have exercised certain cases. The explanation may make a much stronger claim than either of those supports.

We can ask AI to help challenge that claim. We can seek an independent review. But we still need to recognise what an adequate answer would look like.

This is not about distrusting everything. That would make the tools unusable. It's about placing confidence at the level the evidence earns.

I don't need a proof of a prototype's entire behaviour before showing it to a colleague. I do need a much better argument before letting a retry mechanism move a customer's money again.

### Delivery

Use only the general, already-public fact of the correspondence and John's own account of revising his explanation. No private email quotations, unpublished proof/code screenshots, claimed endorsement, or disclosure of Mitch's working methods without permission.

**Transition:** “Let's put that distinction to work on an optimisation an agent might reasonably suggest.”

---

## 9. May we remove the second read?

**23:00–26:00**

**On screen:** A proposed transformation, not a claim about the existing sample.

```text
Before                         After
first = ReadStock(item)         first = ReadStock(item)
...                            ...
second = ReadStock(item)        second = first
use(first, second)              use(first, second)
```

**Reveal:** Two possible service contracts.

| Contract | What happens to the proposal? |
|---|---|
| Live stock can change between calls | The readings may differ; the rewrite can change behaviour |
| A fixed snapshot supplies both readings | Reuse may be justified, subject to the specified observable behaviour |

### Spoken

Two calls, same arguments. Remove the duplicate and reuse the result.

Looks sensible. It may even pass our tests.

Suppose the test interpreter always returns twelve. Both versions produce the same answer. The test has established agreement in that situation.

Now suppose another order reserves stock between the two live reads. The first result is twelve; the second is eleven.

The proposed optimisation has changed what the program can observe.

There may also be other observable behaviour: an audit event, a failure, a quota being consumed. We need to decide which of those belongs to the contract we're preserving.

If instead we explicitly read from one immutable snapshot for the duration of this workflow, we have a different argument. Reusing a result may now be justified.

The operation name didn't tell us that. The service contract did.

This is why “AI can write the optimisation” and “the optimisation is valid here” are different claims.

An agent can discover this concern. It can generate the changing-stock example. It can help find the service documentation. We should use those capabilities.

Our experience helps us ask for that evidence in the first place—and notice when the answer quietly assumes a snapshot we don't have.

The same principle applies to payments. An idempotency key isn't a charm against duplicate charges. Its meaning depends on the provider's behaviour, including the scope and lifetime of that guarantee.

Understanding gives us something more useful than a general instruction to “be careful”. It gives us a precise condition to preserve.

### Delivery

Reveal the changing result before the snapshot contract. Give the audience time to distinguish “doesn't depend on the earlier value” from “safe to reorder or reuse”.

Use 30–45 seconds for “What would make this rewrite safe?” Take one answer before revealing the two contracts. The discussion is about which assumption changes, not finding a clever cache implementation.

**Transition:** “Now we can give the agent a much better task than ‘optimise this service’.”

---

## 10. Give the agent something worth preserving

**26:00–29:00**

**On screen:** A small work brief, progressively revealed.

```text
Goal
Reduce repeated stock lookups within one order evaluation.

Boundary
Reuse is permitted only within an explicit immutable snapshot.
No reuse across snapshots; no payment-path changes.

Evidence
Show the live-read counterexample.
Check same-snapshot reuse and cross-snapshot separation.
Compare the specified outcomes and relevant operation traces.

Unknowns
Identify any required service guarantee the repository cannot establish.
```

### Spoken

This is not a secret prompting technique. It's a small engineering task with its assumptions exposed.

We've said what improvement we want. We've bounded where it may apply. We've asked for evidence that distinguishes the valid case from the invalid one.

We haven't handed the agent the answer to every implementation question. There is still useful work to delegate: finding the right boundary, proposing a representation for snapshot identity, implementing the change, and constructing the checks.

It may discover that the repository has no actual snapshot abstraction. Good. That's important information, not an invitation to name a mutable object `ImmutableSnapshot` and continue.

It may propose a better approach. We should consider it.

But the acceptance conditions don't disappear because the implementation is eloquent.

The valuable move is to carry the important conditions out of the conversation and into the system. A snapshot identifier in the API. A test that crosses the boundary. A short decision record explaining why live reads aren't cached here.

Then the next agent—and the next engineer—has a better starting point.

This is where experience can multiply the value of AI. You aren't merely reviewing the generated lines after the fact. You're improving the conditions under which the work is generated and checked.

The same habit works for authentication, database migrations, background jobs, performance work, and user interfaces. The particular risks differ. The questions remain concrete: what can change, what must remain true, and what evidence would reveal the difference?

### Delivery

This brief is a proposed teaching example, not a report of an actual agent run. A future demonstration must show the real diff and evidence, including any unsuccessful attempts.

Give the room about 30 seconds to read the completed brief. Ask for one check that would catch crossing a snapshot boundary; allow another 20 seconds before continuing.

**Transition:** “How much of that process do we need every time?”

---

## 11. Delegate according to consequence

**29:00–32:00**

**On screen:** **Choose autonomy by consequence and recoverability—not by how impressive the output looks.**

### Spoken

We don't need the same process for everything.

For an experiment with synthetic data and nothing connected to production, exploring by feel may be exactly right. The purpose is to find out whether an idea is worth pursuing. Rewriting it tomorrow may be cheaper than designing it properly today.

But “temporary” doesn't tell us the risk. A one-off migration can damage a production database exactly once.

What matters is what the code can affect, what failure would cost, and whether we can observe and recover from it.

As those consequences grow, we need firmer boundaries. A narrower task. A protected environment. Clear acceptance conditions. Appropriate review. A deployment and recovery plan that works for the actual change, rather than a comforting instruction to “roll it back”.

Structured agent workflows help make those controls repeatable. They don't eliminate the need to choose the right ones.

Nor should we assume that more ceremony means more assurance. Five agents agreeing with the same mistaken assumption may give us five versions of the same mistake.

A different test, a service contract, an operational observation, or a genuinely independent line of reasoning may tell us more.

This is a familiar engineering judgement with a new tool in the loop. We already distinguish a sketch from a load-bearing design. We already know that an internal experiment and a public payment endpoint deserve different scrutiny.

AI changes how quickly we can produce either one. We still need to know which one we're building.

There is no need to turn “vibe coding” and “structured development” into rival identities. We can use exploration deliberately, and recognise when its results are crossing into a setting that needs stronger evidence.

**Transition:** “So if you're deciding where to invest your own learning, where does that leave us?”

---

## 12. Keep learning where the mistakes are expensive

**32:00–35:00**

**On screen:** A question: **What can I now recognise, express, or check that I couldn't before?**

### Spoken

I wouldn't tell you to memorise more syntax as a defence against AI.

I would tell you to keep learning how programs behave.

Learn to read unfamiliar code closely enough to follow the state and effects through it. Understand what a transaction does and doesn't cover. Learn the difference between a computation being described, started, retried, and observed.

Learn enough about types and abstractions to see when they rule out a mistake—and when they merely give a mistake an impressive name.

Practise designing checks that could prove your current belief wrong. If both the implementation and the tests came from the same interpretation of an ambiguous requirement, try changing the assumptions rather than just asking for more tests.

And keep learning the domain. How prices are fixed. What counts as an accepted order. What a user means by “cancel”. Which actions can actually be reversed.

AI can be a very good partner in that learning. Ask for competing designs. Ask for a counterexample. Ask it to explain a library implementation, then read the implementation and see whether the explanation holds.

Don't deprive yourself of the feedback that builds judgement. Predict what the code will do before running it. Trace a failure before accepting the patch. Occasionally implement the small version yourself so you can see where the difficulty really lives.

For experienced colleagues, our job includes making that learning possible for others. If juniors only receive generated code to approve and never get to investigate consequences, we haven't created an accelerated apprenticeship. We've removed part of the apprenticeship.

None of this means everyone should build a DSL. The algebraic approach is one example of knowledge becoming useful structure. Sometimes a well-chosen type, a pure function, and a boundary test are the right amount of design.

The question isn't whether the solution looks sophisticated. It's whether the understanding improves what we can accomplish.

**Transition:** “Let's return to the question we started with.”

---

## 13. What your experience buys

**35:00–37:00**

**On screen:** **AI makes implementation cheaper. Understanding changes what we can trust.**

### Spoken

If AI can write the method, what was the value of learning to write it ourselves?

Part of the answer is that learning gave us a way to reason about what the method does. We can see where a result comes from, where an effect occurs, what a failure leaves behind, and what evidence might establish that a change is safe.

That understanding isn't confined to code we personally type.

We can use it to choose a better representation. To give an agent a useful contract. To recognise a missing assumption. To turn a production failure into a constraint the next change must respect.

Some of our old work will disappear. We shouldn't defend it merely because we spent a long time learning it. Nor can I promise that expertise insulates anyone from changes in jobs or organisations.

But we've seen concrete uses for engineering skill today. Its value comes from applying it, testing it against reality, and improving it.

My own explanation needed that treatment. Learning properly doesn't mean reaching a point where you no longer need to be corrected.

So use the tools. Explore with them. Let them take work off your hands. Let them help you learn things you couldn't yet do alone.

But keep developing the understanding that lets you decide what the result means.

**The code may arrive in seconds. Understanding still has to be earned.**

### Delivery

Stop. Don't add a second closing slogan. Leave the takeaway on screen for Q&A.

---

## Q&A preparation — not part of the timed talk

### “But AI can identify those problems too.”

Yes. That is a reason to use it, not an objection to the talk. The claim is not that humans have exclusive access to counterexamples or architecture. It's that understanding helps us evaluate competing answers and connect them to the actual domain and available evidence. Ask the agent to challenge you as well as implement for you.

### “Is this just dependency injection with extra steps?”

There is a family resemblance. The useful distinction in this example is that the workflow is abstract not just over individual services, but over how computations and their results compose. That makes different carriers and a syntax-producing interpretation possible. Whether that additional abstraction earns its cost depends on the application. Ordinary DI and pure functions may be sufficient.

### “Do I need to learn monads or category theory to keep my job?”

No such guarantee or requirement is being claimed. Learn the underlying behaviours: data flow, effects, sequencing, failure, and what your checks establish. Mathematical vocabulary becomes useful when it lets you recognise a shared structure, reuse a construction, or state a property more precisely. It should explain the example, not serve as a membership test.

### “Does the round-trip prove the generated program is correct?”

No. It establishes a representation relationship under stated assumptions. It doesn't establish that the source program meets the business requirement, that an arbitrary C# generic function meets the uniformity condition, or that a proposed optimiser preserves behaviour. Those are separate obligations.

### “Can I inspect every path if I use a free monad?”

Not in general. A continuation represented by a host-language function can hide what comes next until a value is supplied. Static inspection requires the relevant structure to be represented explicitly, or an appropriate abstraction/model with understood limitations. Exhaustive analysis is always a claim about a specified model, not arbitrary production behaviour.

### “Wouldn't a good specification solve this?”

A specification helps when it expresses the relevant decisions and we have a way to check compliance. Writing “payments are idempotent” doesn't make the provider implement that contract. The specification, implementation, and evidence have to agree. Keeping that agreement through change is part of the engineering work.

### “How do I review code faster than I can read all of it?”

Don't make an unbounded pile of code the unit of trust. Break work into changes with clear boundaries; use automated checks for the properties they can establish; direct human review towards high-consequence decisions and interfaces. If the amount generated exceeds the team's capacity to establish confidence, generation speed has outrun the delivery process. Generating less at once is a legitimate response.

### “What should a junior do differently?”

Use AI to explain, compare, and challenge—but preserve opportunities to predict behaviour, debug failures, and implement manageable pieces. Ask “why does this work?” and “when would it fail?” Follow the answer into code and experiments. Seniors should provide bounded ownership and feedback, not require juniors to approve generated systems they have no opportunity to understand.

### “What about security, operations, and performance?”

The same reasoning applies, with domain-specific evidence. For example: which inputs are trusted, who can perform the action, what load assumption supports the design, and what signals reveal failure? This talk is one architectural example, not a substitute for those disciplines.

### “Is this research new, or endorsed by Mitchell Wand?”

The talk makes neither claim. The public series and subsequent working notes explore established ideas through a concrete workflow. Mitch's questions and review substantially sharpened the account. The later notes remain working material; acknowledgement of correspondence is not blanket endorsement of every claim.

---

## Preparation notes — not for projection or public distribution

### What exists, and what still needs making

This file is the full speaker draft and slide specification. The accompanying PowerPoint contains 13 main slides and five appendix slides, with editable text and shapes. Its speaker notes contain concise talking points, transitions, delivery reminders, and timings for the 33-minute route; appendix notes contain Q&A cues. It has no animations: use pauses and pointing in place of the reveal cues above. No measured rehearsal timings have been recorded here.

The 17 September layout correction sets explicit paragraph spacing and fixed text-frame bounds. The PDF slides were exported by Microsoft PowerPoint; all 18 slides were checked for complete text within their frames. Re-export the PDF after applying a theme or editing the deck.

- **Existing small C# example:** `examples/monadic-algebra/Program.cs` in the separate Intent vs Process research project. It demonstrates `Id` and `Trace`, not production ordering, a complete test suite, or a free/tagless round-trip.
- **Existing deferred-Task example:** `examples/task-moggi/task-moggi.ts` in that project. Useful for a backup discussion of construction versus execution; not needed in the main delivery.
- **Proposed optimisation walkthrough:** slides 9–10 are illustrative. No agent run, benchmark, screenshot, or test result should be invented to make them look like a completed experiment.
- **Still to prepare:** apply the preferred deck theme; run/capture the small C# example if demonstrating its output; rehearse the walkthroughs and full talk; replace target timings with observed timings.

The default delivery is a prepared walkthrough. A live demo is optional, not a dependency of the argument. Before adding one, require a complete local run, readable output, and an equivalent static fallback. No live payment services, production data, or network-dependent generation.

### If a small agent demonstration is added

Use the bounded snapshot task from slide 10. Implement a deterministic live-read counterexample and a snapshot-scoped case. Show the actual patch and checks, including any correction needed. Distinguish checking returned values from checking relevant traces and boundary crossings.

Do not expand this into an order-processing framework. The demonstration earns its place only if it makes the value of a concrete engineering decision easier to see.

### Rehearsal and cuts

The 37-minute schedule is a delivery hypothesis, not a measured duration. Record one full read with slide changes and deliberate pauses; time the technical middle separately.

The draft contains roughly 3,500 spoken words, plus transitions. A straight read will be shorter than the target: about 26–31 minutes depending on pace. The remaining time is for code reading, diagram walkthroughs, reveals, and the bounded audience exchanges marked above. Rehearse those activities rather than stretching the prose to fill a clock. If they don't earn their time, tighten the slot or deepen a concrete example; don't add another theory section.

For a 35-minute slot, target 33 minutes:

- Reduce slide 3 by one minute: keep the payment-key example, shorten the commentary on experience.
- Reduce slide 7 by one minute: explain uniformity and meaning-versus-spelling; leave the richer-language qualification to Q&A.
- Reduce slide 8 by one minute: retain the admission and the distinction between evidence and explanation.
- Reduce slide 11 by one minute: retain the migration example and the consequence/recoverability rule.

Don't steal the closing time, speed-read the code, or drop the conditions that make a claim true.

### Source and attribution boundaries

**Public starting points**

- [The Craft Isn't Dead](https://johnazariah.github.io/2026/05/08/the-craft-isnt-dead.html) — the original provocation, not the final technical authority. Its older blanket guarantees are deliberately not repeated here.
- [The Algebra of Intent](https://johnazariah.github.io/2026/03/05/02-the-algebra-of-intent.html) — the original C# framing.
- [What Mitchell Wand Taught Me About Intent](https://johnazariah.github.io/2026/05/28/what-mitchell-wand-taught-me-about-intent.html) — public acknowledgement of the early correspondence; itself an intermediate account.
- [Froggy Tree House](https://johnazariah.github.io/2025/12/12/tagless-final-01-froggy-tree-house.html) — the original frog example.

**Private preparation sources**

The separate `intent-vs-process` research project contains `manuscript/main.tex`, `PRIMER.md`, the two example directories, and the correspondence archive. Use the July round-trip-centred manuscript as the more precise working account, not as a peer-reviewed result or an automatic certification of the host-language implementation.

The active “Intent vs process notes” worktree also contains a sent-status update absent from the research project's main checkout. John's screenshot confirms that on 30 July he notified Mitch of the updated Overleaf draft and Mitch acknowledged downloading it. This establishes receipt, not completed review or endorsement.

John's original Intent vs Process framing predates Mitch's later proof notes and review. Attribute those later contributions precisely. Do not reproduce private emails, unpublished proof material, or Mitch's supplied code in slides or public attachments without agreeing the use. The main script needs none of those disclosures.

The abandoned agent-generated sequel drafts are not sources for this talk. No new series or publication is implied by this draft.

### Claims to keep out

- “AI cannot reason about architecture or discover invariants.”
- “Vibe coding never converges” or “each generation is independent.”
- “Every implementation of an interface is automatically lawful.”
- “Verification is free” or “the compiler proves the business logic.”
- “An AST exposes every possible future branch.”
- “Initial algebra and final coalgebra are isomorphic by Lambek's Lemma.”
- “Independent operations are therefore safe to run in parallel.”
- “Tests passing proves the general theorem.”
- “An idempotency key alone guarantees safe payment retries.”
- “Expertise guarantees job security.”

The talk should remain forceful because its examples are specific, not because its claims are absolute.
