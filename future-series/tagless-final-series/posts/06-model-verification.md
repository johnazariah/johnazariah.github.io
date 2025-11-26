# The Power of Tagless-Final: Code as Model

[<< Previous: Verifying the Elevator](./05-verifying-elevators.md)

Over the last five posts, we've built a game, explored a multiverse, and verified the safety of an elevator. We did it all using F# Computation Expressions and a pattern called **Tagless-Final**.

Today, I want to talk about *why* this worked so well.

## The "Gap" in Software Verification

In traditional software engineering, there is often a gap between the **Code** and the **Model**.

Imagine you are building a trading system.

1.  **The Architect** draws a state machine diagram on a whiteboard. "The system must never buy if the balance is negative."
2.  **The Developer** writes C# code. They add an `if (balance > 0)` check.
3.  **The Bug**: Six months later, another developer adds a "Margin Trading" feature. They bypass the check because "margin accounts can go negative".
4.  **The Crash**: The system buys stock with money it doesn't have, but for a non-margin account.

The whiteboard model was correct. The code *was* correct. But they drifted apart.

The problem is that these two are separate artifacts. You might prove that your TLA+ spec is safe, but does your C# code actually implement that spec correctly? If you change the code, do you update the spec? Usually, the answer is no. The model becomes outdated, and the verification becomes useless.

## Closing the Gap

Tagless-Final offers a different approach.

**Your Code IS the Model.**

When we wrote:

```fsharp
let controller = elevator {
    move_up
    open_doors
}
```

We didn't write "code" in the traditional sense. We wrote a **description of intent**.

Because this description is abstract (it's just a function waiting for an interpreter), we can use it as both:

1.  **The Implementation**: By passing a `ProductionInterpreter`, we generate the actual commands to move the hardware.
2.  **The Model**: By passing a `VerificationInterpreter` (like our `safetyModel`), we explore the state space and check for bugs.

There is no gap. There is no second file to keep in sync. If you change the controller logic, you are changing the thing that gets verified *and* the thing that runs.

## "Free" Verification

This gives us something magical: **Verification comes for free.**

Well, not entirely free. You have to write the interpreters. But once you have them, every new feature you write in your DSL is automatically verifiable.

- You add a new "Emergency Stop" sequence? The `SafetyInterpreter` checks it immediately.
- You add a complex "VIP Mode"? The `GraphInterpreter` maps it out for you.

## Why Tagless-Final Specifically?

You might ask: "Can't I do this with standard interfaces?"

Yes, to an extent. You could define an `IElevator` interface.

```csharp
interface IElevator {
    void MoveUp();
    void OpenDoors();
}
```

But Tagless-Final (using functions and generic return types) gives us superpowers that interfaces don't:

### 1. Type Safety & Exhaustiveness
The compiler ensures our interpreters handle every instruction. If you add `EmergencyStop` to the algebra, your code won't compile until you update *all* interpreters. You can't forget to update the Safety Model.

### 2. Return Type Flexibility
With an interface, `MoveUp` usually returns `void` or `Task`.
With Tagless-Final, `MoveUp` returns `'a`.
- For the Simulator, `'a` is `State -> State`.
- For the Graph Builder, `'a` is `NodeId -> Graph`.
- For the Pretty Printer, `'a` is `string`.

We aren't locked into a specific execution model. We can interpret the *same code* as a value, a function, or a data structure.

### 3. Composition
We can combine interpreters. We can create a `TeeInterpreter` that runs two interpreters side-by-side.

```fsharp
let loggingSafetyInterpreter = 
    combine interpreters logger safetyChecker
```

This allows us to build complex verification pipelines from simple building blocks.

### 4. Property-Based Testing vs. Model Checking

You might be familiar with **Property-Based Testing** (like FsCheck or QuickCheck). That generates *random* inputs to try and break your code.

What we did here is **Model Checking**. We didn't use random inputs. We used `choose` to explore *all* inputs.

*   **PBT**: "I tried 100 random paths and none of them crashed." (Probabilistic)
*   **Model Checking**: "I explored all 10,000 possible paths and none of them crashed." (Exhaustive)

Tagless-Final allows us to switch between these two strategies just by swapping the interpreter!

## When Should You Use This?

This technique is powerful, but it's not free. It requires a mindset shift and some boilerplate.

**Use Tagless-Final when:**
*   **Correctness is critical**: Financial systems, medical devices, hardware control.
*   **The domain is complex but bounded**: Workflows, state machines, rule engines.
*   **You need multiple "views" of the same logic**: Simulation, visualization, execution, auditing.

**Maybe avoid it when:**
*   **You are building a CRUD app**: If your logic is just "read from DB, show on screen", this is overkill.
*   **Performance is the only metric**: The abstraction layer adds a tiny overhead (though F# inlining often removes it).
*   **Your team hates functional programming**: This pattern relies heavily on FP concepts.

## Further Reading

If you want to dive deeper, here are some terms to Google:

*   **"Tagless Final"**: The core pattern we used.
*   **"Free Monad"**: A related pattern (often seen in Haskell/Scala) that solves similar problems but with data structures instead of functions. (I have a [post on this too](https://johnazariah.github.io/2018/12/04/tale-of-two-languages.html)!)
*   **"Abstract Interpretation"**: The theory behind our "Safety Inspector".
*   **"Model Checking"**: The field of verifying properties of state machines.

## The Takeaway

We started with a silly frog game. We ended up with a technique for building high-assurance software.

The next time you are designing a complex system—whether it's a financial engine, a workflow orchestrator, or a robot controller—consider building a DSL.

Define your **Algebra** (the operations).
Write your **Programs** (the logic).
Build your **Interpreters** (the meaning).

You might find that the best way to write correct software is to stop writing "code" and start writing "models".

Thanks for reading! 🐸🚀

---
[<< Start Over](./01-froggy-tree-house.md)
