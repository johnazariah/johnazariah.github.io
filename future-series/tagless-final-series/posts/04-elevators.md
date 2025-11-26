# A Surprising New DSL: Elevators

[<< Previous: Goals, Threats, and Getting Stuck](./03-goals-threats.md)

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
}
```

And "compile" it to an elevator program:

```fsharp
let elevatorScript = adventure frogToElevator
```

## The Elevator Algorithm

Real elevators are complex. They don't just go up and down randomly. They use algorithms like **SCAN** (or the Elevator Algorithm) to decide which floor to visit next.

We can express this logic in our DSL too!

```fsharp
let scanAlgorithm (currentFloor: int) (requests: int list) = elevator {
    // Sort requests based on current direction
    let sortedRequests = sortRequests currentFloor requests
    
    for floor in sortedRequests do
        // Move to floor
        if floor > currentFloor then move_up
        elif floor < currentFloor then move_down
        
        // Service request
        open_doors
        close_doors
}
```

Because our DSL is just F#, we can mix standard F# logic (sorting, loops) with our domain logic (`move_up`, `open_doors`).

> **Sidebar: Changing Meaning Without Changing Code**
>
> This is the essence of **Tagless-Final**. Our program `adventure` is just a description of *intent*. It is not tied to any specific implementation.
>
> - We can interpret it as a string (Pretty Printer).
> - We can interpret it as a graph (Visualizer).
>
> And now, we can interpret it as another language entirely (Compiler). This is an **Isomorphism**. We are showing that the structure of the Frog problem is the same as the structure of the Elevator problem.

## Why?

Why on earth would we want to control an elevator with a frog game?

Well, remember our **Safety Inspector** from the last post? The one that checked for death paths?

If we can map the Elevator (complex, dangerous, hard to test) to the Frog (simple, abstract, easy to visualize), maybe we can use the Frog tools to verify the Elevator safety.

Maybe `Die "Eaten by snake"` in Frog World corresponds to `Crash "Door open while moving"` in Elevator World.

In the next post, we'll see how to use this connection to verify our elevator code.

---
[<< Previous: Goals, Threats, and Getting Stuck](./03-goals-threats.md) | [Next: Verifying the Elevator >>](./05-verifying-elevators.md)
