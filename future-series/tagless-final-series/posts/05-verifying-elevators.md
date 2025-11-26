# Verifying the Elevator

[<< Previous: A Surprising New DSL: Elevators](./04-elevators.md)

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

1.  Translate `Elevator Program` $\to$ `Frog Program`.
2.  Run `Frog Program` through `Safety Inspector`.
3.  If Frog dies, Elevator crashes.

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

## Building the Model

We define a new interpreter that models the *rules* of the elevator.

We need to track the state of the doors. And we need to return a list of errors if we find any.

```fsharp
type ElevatorState = { DoorsOpen : bool }

// The result type is a function that takes a state and returns:
// 1. A list of possible next states (if successful)
// 2. A list of errors (if crashed)
type VerificationResult = ElevatorState -> (ElevatorState list * string list)

let safetyModel : ElevatorInterpreter<VerificationResult> = {
    OpenDoors = fun () -> fun s -> 
        // Opening doors is always safe, and results in open doors
        [ { s with DoorsOpen = true } ], []
        
    CloseDoors = fun () -> fun s -> 
        // Closing doors is always safe
        [ { s with DoorsOpen = false } ], []
    
    MoveUp = fun () -> fun s -> 
        if s.DoorsOpen then 
            // CRASH! No next state, just an error.
            [], ["CRASH: Moved up with doors open!"]
        else 
            // Safe to move
            [ s ], []

    MoveDown = fun () -> fun s -> 
        if s.DoorsOpen then 
            [], ["CRASH: Moved down with doors open!"]
        else 
            [ s ], []
            
    // Bind needs to handle the branching logic
    Bind = fun prev next -> fun s ->
        let (states, errors) = prev s
        
        // If we already have errors, we keep them.
        // If we have valid states, we continue running 'next' on them.
        let nextResults = states |> List.map (fun s' -> next() s')
        
        let newStates = nextResults |> List.collect fst
        let newErrors = nextResults |> List.collect snd
        
        (newStates, errors @ newErrors)
        
    Return = fun () -> fun s -> [s], []
    
    Choose = fun options -> fun s ->
        // Run all options and combine results
        let results = options |> List.map (fun opt -> opt s)
        (results |> List.collect fst, results |> List.collect snd)
        
    Crash = fun reason -> fun s -> [], [reason]
}
```

This looks a lot like our Frog Simulator, doesn't it? It tracks state. But instead of calculating hunger, it calculates **validity**.

## Running the Verification

Now we run `riskyScript` through `safetyModel`.

```fsharp
let (finalStates, errors) = riskyScript safetyModel { DoorsOpen = false }
```

**Result:** `errors` contains `["CRASH: Moved up with doors open!"]`

We have mathematically proven that `riskyScript` is unsafe, without ever running a real elevator.

## It's Not Just Testing

This isn't just a unit test. Because we have `choose` (nondeterminism), we can verify **every possible sequence of events**.

If we have a complex controller that reacts to buttons:

```fsharp
let controller = elevator {
    choose [
        elevator { button_pressed; open_doors }
        elevator { button_pressed; move_up }
    ]
}
```

Our interpreter will explore *both* branches. It will find that the second branch might be unsafe if the doors were already open.

We are exploring the entire state space of the program.

### Liveness Properties

We can also check for **Liveness**: "Does the elevator eventually arrive?"

This is harder. We need to detect loops. If the elevator enters a state where it just opens and closes the doors forever, it satisfies the safety property (it never moves with doors open), but it fails the liveness property (it never arrives).

To check this, we would use a **Graph Interpreter** (like in Post 2) to build the full state graph, and then run an algorithm to find "Strongly Connected Components" that don't contain a "Success" state.

## What Have We Done?

We've taken a domain (Elevators), defined a language for it, and built a special interpreter that checks for safety properties.

This seems powerful. Is there a name for this?

In the next post, we'll step back and look at the bigger picture. We'll talk about **Model Verification** and why the technique we just used (Tagless-Final) is a superpower for building reliable software.

---
[<< Previous: A Surprising New DSL: Elevators](./04-elevators.md) | [Next: The Power of Tagless-Final: Code as Model >>](./06-model-verification.md)
