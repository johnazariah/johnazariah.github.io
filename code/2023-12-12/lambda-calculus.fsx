// consider the following recursive factorial function
let rec fact =
    function
    | 0 -> 1
    | n -> n * fact (n - 1)
fact 10

// we don't want `let rec` - instead we pass in the function (let's call it something random, like 'omega') and call that instead
let f omega =
    function
    | 0 -> 1
    | n -> n * omega (n - 1)

// ok - so what function should we call f with??

// let's create something that goes boom...
let error x = failwith $"{x} goes Boom!"

// ...and try using that with f
let fact0 = f (error)
fact0 0
//fact0 1 // goes boom

let fact1 = f (fact0)
fact1 0
fact1 1
//fact1 2 // goes boom

let fact2 = f (fact1)
fact2 0
fact2 1
fact2 2
//fact2 3 // goes boom


// ok let's go back to this expression for a second
fact0 0

// this is the same as
f (error) 0

// which is the same as
(fun omega ->
    function
    | 0 -> 1
    | n -> n * omega(n - 1)
) (error) 0

// so lets wrap this expression with a function
// well, the omega lambda needs to be a valid binding for x, and we need to ensure that "error" is put in there somewhere so that we can call the omega lambda with it
(fun x -> x(error))
    (fun omega ->
        function
        | 0 -> 1
        | n -> n * omega(n - 1)
    )
    0

(fun x -> x(x(x(x(error)))))
    (fun omega ->
        function
        | 0 -> 1
        | n -> n * omega(n - 1)
    )
    3 // succeeds
    // 4 // fails


// come back from the javascript script

(fun f ->
    (fun x -> f (fun n -> (x x) (n)))
        (fun x -> f (fun n -> (x x) (n))))
    (fun p ->
        function
        | 0 -> 1
        | n ->
            n * p(n - 1))
    10


(fun f ->
    (fun x -> f (fun n -> (x x) (n)))
        (fun x -> f (fun n -> (x x) (n))))
    (fun p ->
        function
        | 0 -> 1
        | n ->
            n * p(n - 1))
    10


(fun f ->
    (fun  (x : obj -> _) -> f (fun n -> (x x) (n)))
        (fun x -> f (fun n -> (x x) (n))))
    (fun p ->
        function
        | 0 -> 1
        | n ->
            n * p(n - 1))
    10


(fun f ->
    (fun (x : obj -> _) -> f (fun n -> x x n))
        (fun x -> f (fun n -> (x :?> obj -> _) x n)))
    (fun p ->
        function
        | 0 -> 1
        | n ->
            n * p(n - 1))
    10


let Z = 
    (fun f ->
        (fun (x : obj -> _) -> f (fun n -> x x n))
            (fun x -> f (fun n -> (x :?> obj -> _) x n)))
let Z f =
    (fun (x : obj -> _) -> f (fun n -> x x n))
        (fun x -> f (fun n -> (x :?> obj -> _) x n))
