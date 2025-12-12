---
    layout: post
    title: "Tagless Final in F# - Part 5: Verifying the Elevator"
    tags: [F#, functional-programming, tagless-final, dsl, computation-expressions]
    author: johnazariah
    summary: Using our Froggy infrastructure to detect safety violations in elevator control programs.
---

[<< Previous: A Surprising New DSL: Elevators](/2025/12/13/tagless-final-04-elevators.html)

---

# Verifying the Elevator

We have a shiny new DSL for controlling elevators. We can write scripts like:

```fsharp
let normalOperation = elevator {
    move_up
    open_doors
    close_doors
    move_down
}
```

But elevators are heavy machinery. If we make a mistake, bad things happen. We need to be absolutely sure that our code is safe.

## Defining Safety

What does "safe" mean for an elevator?

Let's define a simple rule: **The elevator must never move while the doors are open.**

If we were writing standard imperative code, we might try to enforce this with `if` statements everywhere. But we often forget one.

In our DSL world, we can define a **Safety Interpreter**.

## The Safety Interpreter

Remember the `SafetyInspector` we built for Froggy? It checked if Froggy ever died.

We can build the exact same thing for the Elevator.

```fsharp
// We define what "Safe" means for our interpreter
let safetyCheck : ElevatorInterpreter<bool> = {
    MoveUp = fun () -> false // Safe
    MoveDown = fun () -> false // Safe
    
    // But wait! We need state to know if doors are open.
    // Let's make our interpreter stateful, like the Simulator.
    // State = areDoorsOpen: bool
}
```

Actually, let's look at it differently. Let's say we have a `Crash` operation in our DSL (just like `Die` in Froggy's world).

```fsharp
type ElevatorInterpreter<'a> = {
    // ... existing ops ...
    Crash : string -> 'a
}
```

And our "Hardware" interpreter (the one that runs the real elevator) throws an exception if we try to move with doors open.

But we want to catch this *before* we run it.

## Reusing the Frog Tools

In the last post, we saw that the Elevator DSL and the Frog DSL are structurally identical.

- `Move` $\approx$ `Jump`
- `Open/Close` $\approx$ `Croak`
- `Crash` $\approx$ `Die`

Because they share the same structure (Algebra), we can reuse the **logic** of our Frog tools.

If we map our Elevator operations to Frog operations, we can use the **Frog Safety Inspector** to check our Elevator!

1. Translate `Elevator Program` $\to$ `Frog Program`.
2. Run `Frog Program` through `Safety Inspector`.
3. If Frog dies, Elevator crashes.

## Finding a Bug

Let's look at a buggy program:

```fsharp
let riskyScript = elevator {
    open_doors
    move_up   // DANGER! Doors are open!
    close_doors
}
```

If we translate this to Frog:

```fsharp
let frogScript = frog {
    croak     // Open doors
    jump      // Move up -> DANGER?
    croak     // Close doors
}
```

Wait, `Jump` isn't dangerous for a Frog. This is where our mapping needs to be smart.

We need a **Model** of the elevator's constraints.

## Building a Safety Model

Instead of a direct translation, we build a **Safety Model Interpreter** for the Elevator DSL.

This interpreter tracks state and inserts `die` when rules are violated.

```fsharp
type ElevatorState = { DoorsOpen: bool; Floor: int }

let safetyModel : ElevatorInterpreter<ElevatorState -> FrogProgram<'a>> = {
    OpenDoors = fun () -> fun state ->
        frog { 
            croak // Signal "doors opening"
        }, { state with DoorsOpen = true }
    
    CloseDoors = fun () -> fun state ->
        frog { 
            croak // Signal "doors closing"  
        }, { state with DoorsOpen = false }
    
    MoveUp = fun () -> fun state ->
        if state.DoorsOpen then
            frog { die "Moved with doors open!" }, state
        else
            frog { jump }, { state with Floor = state.Floor + 1 }
    
    MoveDown = fun () -> fun state ->
        if state.DoorsOpen then
            frog { die "Moved with doors open!" }, state
        else
            frog { eat_fly }, { state with Floor = state.Floor - 1 }
    
    // ... Bind, Return, Choose, Crash implementations ...
}
```

Now when we run `riskyScript` through `safetyModel`, it produces a Frog program that contains `die "Moved with doors open!"`.

When we then run *that* through the `safetyInspector`, it returns `true` (danger detected!).

## The Verification Pipeline

```
Elevator Program
      |
      v
  [safetyModel] -- Translates to Frog, inserting 'die' on violations
      |
      v
  Frog Program
      |
      v
  [safetyInspector] -- Checks if any path leads to 'die'
      |
      v
  bool: true = UNSAFE, false = SAFE
```

We've built a verification pipeline by composing interpreters!

## Property: Door Safety

We can now express our safety property as a test:

```fsharp
let isDoorSafe (program: ElevatorProgram<_>) =
    let initialState = { DoorsOpen = false; Floor = 0 }
    let (frogProgram, _) = program safetyModel initialState
    let isDangerous = frogProgram safetyInspector
    not isDangerous

// Test it!
assert (isDoorSafe normalOperation) // true
assert (not (isDoorSafe riskyScript)) // true - it IS dangerous
```

## Adding More Properties

We can add more rules without changing our core infrastructure:

```fsharp
// Rule: Never go below floor 0
let floorBoundsModel : ElevatorInterpreter<ElevatorState -> FrogProgram<'a>> = {
    MoveDown = fun () -> fun state ->
        if state.Floor <= 0 then
            frog { die "Tried to go below ground!" }, state
        else
            frog { eat_fly }, { state with Floor = state.Floor - 1 }
    // ... rest similar ...
}

// Rule: Never open doors while moving (need velocity state)
// Rule: Emergency stop must always be responsive
// ... and so on
```

Each rule is a new interpreter. We can combine them using `product` from earlier!

```fsharp
let fullSafetyCheck = product safetyModel floorBoundsModel
```

## What's Next?

We've seen how Tagless-Final lets us build verification tools by composing interpreters. But we've been talking about this in terms of Frogs and Elevators.

In the final post, we'll zoom out and discuss the deeper principle: **Code as Model**. We'll see how Tagless-Final closes the gap between specification and implementation.

---

*This post is part of [FsAdvent 2025](https://sergeytihon.com/fsadvent/).*

[<< Previous: A Surprising New DSL: Elevators](/2025/12/12/tagless-final-04-elevators.html) | [Next: The Power of Tagless-Final: Code as Model >>](/2025/12/12/tagless-final-06-model-verification.html)
