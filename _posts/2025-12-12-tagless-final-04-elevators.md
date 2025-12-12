---
    layout: post
    title: "Tagless Final in F# - Part 4: A Surprising New DSL: Elevators"
    tags: [F#, functional-programming, tagless-final, dsl, computation-expressions]
    author: johnazariah
    summary: Discovering that frogs and elevators share the same algebraic structure - and building a translator between them.
---

[<< Previous: Goals, Threats, and Getting Stuck](/2025/12/13/tagless-final-03-goals-threats.html)

---

Okay, put the frogs away. We have a new contract. A corporate client wants us to write control software for a skyscraper elevator system.

Serious business. No jumping, no croaking.

## The Elevator DSL

We need to be able to move the cabin, operate the doors, and handle emergency stops.

```fsharp
type ElevatorInterpreter<'a> = {
    MoveUp : unit -> 'a
    MoveDown : unit -> 'a
    OpenDoors : unit -> 'a
    CloseDoors : unit -> 'a
    
    // We also need choice, just like the frog!
    // Maybe the elevator can choose to serve floor 5 or floor 7 first.
    Choose : 'a list -> 'a
    
    // And we need to know if we crashed.
    Crash : string -> 'a
    
    Bind : 'a -> (unit -> 'a) -> 'a 
    Return : unit -> 'a
}
```

We can write a builder for this, just like we did for Froggy.

```fsharp
let serviceRequest = elevator {
    move_up
    open_doors
    close_doors
    move_down
}
```

This looks familiar, doesn't it?

## Déjà Vu

Let's look at our `FrogInterpreter` again.

```fsharp
type FrogInterpreter<'a> = {
    Jump : unit -> 'a     
    Croak : unit -> 'a    
    EatFly : unit -> 'a   
    Choose : 'a list -> 'a
    Die : string -> 'a
    ...
}
```

They are... suspiciously similar. In fact, if we squint, they are **structurally identical**.

| Frog World | Elevator World | Concept |
| :--- | :--- | :--- |
| `Jump` | `MoveUp` / `MoveDown` | State Transition |
| `Croak` | `OpenDoors` | Action in Place |
| `EatFly` | `CloseDoors` | Action in Place |
| `Choose` | `Choose` | Nondeterminism |
| `Die` | `Crash` | Failure State |

This isn't a coincidence. Both domains are describing **Sequential Processes with Branching and Failure**.

## The Translator Interpreter

What if we could run our Frog code *on* the Elevator? Or, more usefully, run our Elevator code on the Frog infrastructure?

We can build a `FrogInterpreter` where the result type `'a` is actually an `ElevatorProgram`. This is a **Compiler**. It translates one language into another.

```fsharp
// This interpreter turns Frog commands into Elevator commands
let frogToElevator : FrogInterpreter<ElevatorProgram<'a>> = {
    Jump = fun () -> elevator { move_up }
    Croak = fun () -> elevator { open_doors; close_doors } // Croaking opens and closes doors?
    EatFly = fun () -> elevator { move_down }
    
    Return = fun () -> elevator { return () }
    Bind = fun prev next -> 
        elevator {
            do! prev
            do! next()
        }
    
    // Mapping failure is easy
    Die = fun reason -> elevator { crash ("Frog died: " + reason) }
    
    // Mapping choice is easy
    Choose = fun options -> elevator { choose options }
}
```

Now, we can take our existing `adventure` script:

```fsharp
let adventure = frog {
    jump
    croak
    jump
    eat_fly
}
```

And run it through our translator:

```fsharp
let elevatorProgram = adventure frogToElevator
```

The `elevatorProgram` is now a valid elevator control sequence! It will:
1. Move up
2. Open doors, close doors
3. Move up
4. Move down

## Why This Matters

This isn't just a party trick. It has real applications:

### 1. Testing Infrastructure Reuse

If you've built extensive test infrastructure for one DSL (randomized testing, property checkers, visualizers), you can reuse it for another DSL by building a translator.

### 2. Gradual Migration

Migrating from one system to another? Build a translator. Run both systems in parallel. Compare outputs.

### 3. Simulation

Want to test your elevator controller before deploying it to real hardware? Translate elevator commands to a simulation DSL that models physics.

### 4. Cross-Domain Verification

This is the big one. If we've proven that our Frog tools can detect danger, and we can translate Elevator programs to Frog programs, then we can use the Frog tools to verify Elevator programs.

## The Algebra is the Contract

The key insight is that the **shape** of the interpreter record defines a **contract**.

Any domain that fits the shape:
- Some actions
- Nondeterministic choice
- Failure states
- Sequential composition

...can be expressed in our DSL framework.

And once expressed, all our interpreters—storytellers, simulators, safety checkers, graph builders—work automatically.

## What's Next?

We've built a bridge between frogs and elevators. But we still haven't answered the big question: **Can we verify that our elevator is safe?**

In the next post, we'll use our Frog infrastructure to detect bugs in elevator programs—before they crash.

---

*This post is part of [FsAdvent 2025](https://sergeytihon.com/fsadvent/).*

[<< Previous: Goals, Threats, and Getting Stuck](/2025/12/12/tagless-final-03-goals-threats.html) | [Next: Verifying the Elevator >>](/2025/12/12/tagless-final-05-verifying-elevators.html)
