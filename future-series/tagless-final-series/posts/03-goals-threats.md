# Goals, Threats, and Getting Stuck

[<< Previous: Maps, Branches, and Choices](./02-maps-branches-choices.md)

Froggy is happily exploring the multiverse. He can jump, croak, and choose his own path. But life isn't just about wandering aimlessly. Sometimes you want to reach the top of the tree. And sometimes, there are snakes.

Today, we're adding **Goals** and **Threats**.

## Winning and Losing

Let's expand our language to include end states.

```fsharp
frog {
    jump
    choose [
        frog { jump; win }             // Reached the top!
        frog { croak; die "Eaten by snake" } // Oh no.
    ]
}
```

## Updating the Interpreter

We add `Win` and `Die` to our record.

```fsharp
type FrogInterpreter<'a> = {
    Jump : unit -> 'a
    Croak : unit -> 'a
    EatFly : unit -> 'a
    Choose : 'a list -> 'a
    
    // New terminals
    Win : unit -> 'a
    Die : string -> 'a
    
    Bind : 'a -> (unit -> 'a) -> 'a 
    Return : unit -> 'a
}
```

## Interpreter 5: The Safety Inspector

We can build an interpreter specifically designed to find bugs... I mean, dangers.

Let's say we want to know: **Is it possible for Froggy to die?**

We can define an interpreter where `'a` is `bool`.

- `Die` returns `true` (danger found!).
- `Win` returns `false` (safe).
- `Choose` returns `List.exists` (if *any* branch dies, the whole thing is dangerous).

```fsharp
let safetyInspector : FrogInterpreter<bool> = {
    Jump = fun () -> false
    Croak = fun () -> false
    EatFly = fun () -> false
    Win = fun () -> false
    Die = fun _ -> true // Found a death!
    
    Return = fun () -> false
    Bind = fun prev next -> 
        if prev then true // If already dead, stay dead (Short-circuit!)
        else next()       // Otherwise continue checking
        
    Choose = fun options -> options |> List.exists id
}
```

Notice the short-circuiting in `Bind`. If `prev` is true (meaning a death was found in the previous step), we don't even bother running `next()`. We just propagate the danger signal. This makes our inspector very efficient.

Now we can run our game through the `safetyInspector`.

```fsharp
let isDangerous = game safetyInspector
if isDangerous then printfn "Warning: This game contains death!"
```

## Interpreter 6: The Coroner (Trace Analysis)

Knowing *that* Froggy dies is useful. Knowing *how* he dies is better.

We can build an interpreter that returns a list of "Death Traces". A trace is just a list of strings describing the steps taken.

Our result type `'a` will be `string list list`. A list of paths, where each path is a list of strings.

```fsharp
let coroner : FrogInterpreter<string list list> = {
    // Basic actions just append to all current paths
    Jump = fun () -> [["Jumped"]]
    Croak = fun () -> [["Croaked"]]
    EatFly = fun () -> [["Ate Fly"]]
    
    // Win returns an empty list (no death here)
    Win = fun () -> []
    
    // Die returns a single path containing the reason
    Die = fun reason -> [[sprintf "Died: %s" reason]]
    
    Return = fun () -> [[]]
    
    Bind = fun prev next -> 
        // This is where we combine paths.
        // If 'prev' has paths, we extend them with 'next'.
        let prevPaths = prev
        // Note: This implementation is simplified. 
        // In reality, we need to run 'next' and prepend 'prev'.
        // But for now, let's just say we concatenate strings.
        ...
        
    Choose = fun options -> options |> List.concat
}
```

Implementing `Bind` correctly for trace analysis is a fun exercise (hint: it involves a cross-product of lists). But the result is worth it.

When we run this, we get a report:

```text
Potential Deaths:
1. Jump -> Croak -> Died: Eaten by snake
2. Jump -> Jump -> Slip -> Died: Fell off branch
```

This is incredibly useful for debugging level design. "Oh, I didn't realize the player could jump *there* and get stuck."

## Finding the Winning Path

We can also flip this logic. Instead of looking for death, we can look for **Victory**.

We can build a `StrategyInterpreter` that finds the *shortest path* to a `Win` state.

```fsharp
// Result is 'int option' (Some steps or None if impossible)
let strategyGuide : FrogInterpreter<int option> = {
    Jump = fun () -> Some 1
    Croak = fun () -> Some 1
    EatFly = fun () -> Some 1
    
    Win = fun () -> Some 0
    Die = fun _ -> None // Dead end
    
    Bind = fun prev next -> 
        match prev with
        | None -> None
        | Some steps -> 
            match next() with
            | None -> None
            | Some moreSteps -> Some (steps + moreSteps)
            
    Choose = fun options -> 
        // Find the minimum of all valid options
        options 
        |> List.choose id 
        |> List.sort 
        |> List.tryHead
}
```

If we run this on our game, it tells us: "You can win in 5 steps."

Combine this with the Trace Analyzer, and you can print out the exact walkthrough for the player!

## Try It Yourself

Here is a challenge for you:

1. Implement the `Bind` function for the `Coroner` interpreter properly.
2. Add a `PowerUp` instruction to the DSL.
3. Update the `StrategyInterpreter` to prefer paths that pick up power-ups.

> **Sidebar: Interpreters Disagree Because They Give Different Meanings**
>
> Notice that our `safetyInspector` and our `simulator` (from Post 1) have very different views of the world.
>
> - The **Simulator** cares about *state* (hunger, height).
> - The **Safety Inspector** ignores state completely! It doesn't care if Froggy is hungry when he dies. It only cares about the `Die` event.
>
> This is a powerful concept. We can write interpreters that abstract away everything except the specific property we are interested in. This is called **Abstract Interpretation**. We are interpreting the program in a domain that is simpler than the "real" domain, but still preserves the properties we care about (like safety).

## The Plot Thickens

We have a language for moving, choosing, winning, and losing. We have interpreters that can run the game, map the game, and check for safety.

It feels like we've built a nice little ecosystem for our game.

But... what if I told you this wasn't about frogs at all?

In the next post, we're going to take a hard left turn. We're going to leave the forest and enter a skyscraper. 🏢

---
[<< Previous: Maps, Branches, and Choices](./02-maps-branches-choices.md) | [Next: A Surprising New DSL: Elevators >>](./04-elevators.md)
