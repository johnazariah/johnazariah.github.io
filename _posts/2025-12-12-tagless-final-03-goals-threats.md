---
    layout: post
    title: "Tagless Final in F# - Part 3: Goals, Threats, and Getting Stuck"
    tags: [F#, functional-programming, tagless-final, dsl, computation-expressions]
    author: johnazariah
    summary: Adding win conditions and failure states to our DSL, plus building safety inspectors and coroners.
---

[<< Previous: Maps, Branches, and Choices](/2025/12/13/tagless-final-02-maps-branches-choices.html)

---

# Goals, Threats, and Getting Stuck

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
        prev |> List.collect (fun path ->
            let nextPaths = next()
            nextPaths |> List.map (fun nextPath -> path @ nextPath)
        )
    
    Choose = fun options -> options |> List.concat
}
```

Now we can get a full autopsy report!

```fsharp
let deathPaths = dangerousGame coroner
deathPaths |> List.iter (fun path ->
    printfn "Death path: %s" (String.concat " -> " path)
)
```

**Output:**

```text
Death path: Jumped -> Croaked -> Died: Eaten by snake
```

## Combining Interpreters

What if we want to run the safety check *and* collect the traces at the same time?

We can build a **Product Interpreter** that runs two interpreters in parallel.

```fsharp
let product (i1: FrogInterpreter<'a>) (i2: FrogInterpreter<'b>) : FrogInterpreter<'a * 'b> = {
    Jump = fun () -> (i1.Jump(), i2.Jump())
    Croak = fun () -> (i1.Croak(), i2.Croak())
    EatFly = fun () -> (i1.EatFly(), i2.EatFly())
    Win = fun () -> (i1.Win(), i2.Win())
    Die = fun reason -> (i1.Die reason, i2.Die reason)
    Return = fun () -> (i1.Return(), i2.Return())
    Bind = fun (prev1, prev2) next ->
        let (next1, next2) = next()
        (i1.Bind prev1 (fun () -> next1), i2.Bind prev2 (fun () -> next2))
    Choose = fun options ->
        let (opts1, opts2) = options |> List.unzip
        (i1.Choose opts1, i2.Choose opts2)
}

// Use it!
let combined = product safetyInspector coroner
let (isDangerous, deathPaths) = game combined
```

## The Algebra Grows

Look at how our interpreter record has evolved:

| Version | Operations | Purpose |
|---------|------------|---------|
| Part 1 | `Jump`, `Croak`, `EatFly` | Basic actions |
| Part 2 | + `Choose` | Nondeterminism |
| Part 3 | + `Win`, `Die` | Terminal states |

Each addition to the algebra opens up new possibilities for interpretation. And because F# is strongly typed, the compiler ensures we handle every case in every interpreter.

## What's Next?

We've built a surprisingly powerful toolkit for a frog game. But is this just for frogs?

In the next post, we'll leave the tree behind and visit a skyscraper. We'll see that the same patterns apply to a completely different domain: **Elevator Control Systems**.

---

*This post is part of [FsAdvent 2025](https://sergeytihon.com/fsadvent/).*

[<< Previous: Maps, Branches, and Choices](/2025/12/12/tagless-final-02-maps-branches-choices.html) | [Next: A Surprising New DSL: Elevators >>](/2025/12/12/tagless-final-04-elevators.html)
