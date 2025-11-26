# Froggy Tree House: A Tiny DSL for a Tiny Game

Welcome to **Froggy Tree House**! 🐸

This is a series about building a game. Well, not just a game. It's about how we *talk* to computers, how we define meaning, and how we can say one thing but mean two (or three, or four) different things.

But mostly, it's about a frog named Froggy.

Froggy lives in a tree. He likes to jump. He likes to croak. Sometimes, if he's lucky, he catches a fly. We want to write a program to control Froggy.

## The "Ugly" Way

If we were writing standard F#, we might model Froggy's actions as a list of commands.

```fsharp
type Action = 
    | Jump 
    | Croak 
    | EatFly

let myProgram = [ Jump; Croak; Jump; EatFly ]
```

This is okay, but it's a bit... static. What if we want to do things based on the state of the world? What if we want to chain actions together more naturally?

We could write functions:

```fsharp
let runFroggy (frog: Frog) =
    let frog2 = frog.Jump()
    let frog3 = frog2.Croak()
    frog3.EatFly()
```

That's a bit clunky. We have to thread that `frog` state through everything. If we forget to pass `frog2` to the next function and pass `frog` instead, we've introduced a bug where time didn't move forward.

Imagine if we had 50 lines of this. One typo, and Froggy teleports back in time. We want something that handles that plumbing for us.

## The "Cute" Way: A Froggy DSL

What we really want is to write code that looks like a story. We want a **Domain Specific Language** (DSL) just for Froggy.

Wouldn't it be nice if we could write this?

```fsharp
let adventure = frog {
    jump
    croak
    jump
    eat_fly
}
```

This looks clean. It looks like a script. But how do we make F# understand it?

## Making It Work (The Magic)

To make that `frog { ... }` syntax work, we need a **Computation Expression**. But before we build the builder, we need to decide what our "instructions" actually *are*.

In this series, we're going to use a technique where an **Interpreter** is just a record of functions.

```fsharp
type FrogInterpreter<'a> = {
    Jump : unit -> 'a
    Croak : unit -> 'a
    EatFly : unit -> 'a
    // We need a way to glue instructions together
    Bind : 'a -> (unit -> 'a) -> 'a 
    Return : unit -> 'a
}
```

Wait, don't panic at the types! All this says is: "If you want to be a Frog Interpreter, you need to know how to handle a Jump, a Croak, and Eating a Fly."

The generic type `'a` represents the **result** of our program.
- If we are printing a story, `'a` might be `string`.
- If we are simulating a game, `'a` might be `FrogState -> FrogState`.
- If we are drawing a picture, `'a` might be `Image`.

The `Bind` function is the glue. It says: "Give me the result of the previous instruction (`'a`), and a function that generates the next instruction (`unit -> 'a`), and I will combine them into a new result (`'a`)."

Now, our `frog` builder just uses this interpreter.

```fsharp
// A FrogProgram is just a function that takes an interpreter and returns a result
type FrogProgram<'a> = FrogInterpreter<'a> -> 'a

type FrogBuilder() =
    // 'Yield' is called when we have a simple value (like 'return' in C#)
    member _.Yield(()) = fun (i: FrogInterpreter<'a>) -> i.Return ()
    
    // Custom operations allow us to add keywords like 'jump' and 'croak'
    [<CustomOperation("jump")>]
    member _.Jump(state: FrogProgram<'a>) = 
        fun (i: FrogInterpreter<'a>) -> i.Bind (state i) (fun () -> i.Jump())

    [<CustomOperation("croak")>]
    member _.Croak(state: FrogProgram<'a>) = 
        fun (i: FrogInterpreter<'a>) -> i.Bind (state i) (fun () -> i.Croak())

    [<CustomOperation("eat_fly")>]
    member _.EatFly(state: FrogProgram<'a>) = 
        fun (i: FrogInterpreter<'a>) -> i.Bind (state i) (fun () -> i.EatFly())

let frog = FrogBuilder()
```

> Note: This is a simplified view. In a real tagless-final encoding, we might do this slightly differently, but let's stick to the idea that a program is just a function waiting for an interpreter.

## Interpreter 1: The Storyteller

Now that we have our `adventure` defined, it doesn't actually *do* anything. It's just a description. To run it, we need an interpreter.

Let's build a **Pretty Printer**. This interpreter doesn't simulate physics; it just tells a story.

```fsharp
let storyTeller : FrogInterpreter<string> = {
    Jump = fun () -> "Froggy jumps up!"
    Croak = fun () -> "Ribbit!"
    EatFly = fun () -> "Yum, a fly!"
    Return = fun () -> ""
    Bind = fun prev next -> 
        let n = next()
        if prev = "" then n else prev + "\n" + n
}

// Run it!
let result = adventure storyTeller
printfn "%s" result
```

**Output:**

```text
Froggy jumps up!
Ribbit!
Froggy jumps up!
Yum, a fly!
```

## Interpreter 2: The Simulator

That was fun, but is Froggy actually getting anywhere? Let's build a **Simulator** that tracks Froggy's height and hunger.

```fsharp
type FrogState = { Height: int; Hunger: int }

// Our 'a is now a function: FrogState -> FrogState
let simulator : FrogInterpreter<FrogState -> FrogState> = {
    Jump = fun () -> fun s -> { s with Height = s.Height + 1; Hunger = s.Hunger + 1 }
    Croak = fun () -> fun s -> { s with Hunger = s.Hunger + 1 } // Croaking takes energy
    EatFly = fun () -> fun s -> { s with Hunger = 0 }
    Return = fun () -> id
    Bind = fun prev next -> fun s -> 
        let s' = prev s
        let nextAction = next()
        nextAction s'
}

// Run it!
let finalState = adventure simulator { Height = 0; Hunger = 0 }
printfn "Final Height: %d, Hunger: %d" finalState.Height finalState.Hunger
```

**Output:**

```text
Final Height: 2, Hunger: 0
```

Wait, what if Froggy runs out of energy? We could add logic to `Jump` to check `s.Hunger`.

```fsharp
    Jump = fun () -> fun s -> 
        if s.Hunger > 10 then 
            printfn "Froggy is too tired to jump!"
            s 
        else 
            { s with Height = s.Height + 1; Hunger = s.Hunger + 1 }
```

This is the beauty of it: The `adventure` code doesn't know about hunger limits. The *interpreter* enforces the rules of physics.

> **Sidebar: Interpreters Are Just Records of Functions**
>
> Notice something cool? The `adventure` code didn't change.
>
> ```fsharp
> let adventure = frog {
>     jump
>     croak
>     jump
>     eat_fly
> }
> ```
>
> This single piece of code has **two different meanings** depending on which interpreter we give it.
>
> - To the `storyTeller`, it's a string generator.
> - To the `simulator`, it's a state transition function.
>
> This is the power of separating the **description** of the program from its **execution**. In fancy math terms, the `FrogInterpreter` type defines an **Algebra**, and our `storyTeller` and `simulator` are two different **Instances** of that algebra. The `adventure` is a program written against that algebra.

## What's Next?

Right now, Froggy just follows a script. But the world is scary and unpredictable! What if Froggy wants to make choices? What if there are multiple paths up the tree?

In the next post, we'll introduce **Nondeterminism** and let Froggy explore the multiverse. 🌌

---
[Next: Maps, Branches, and Choices >>](./02-maps-branches-choices.md)
