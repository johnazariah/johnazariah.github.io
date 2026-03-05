module IntentVsProcess.Tests

open Xunit
open IntentVsProcess

// ─── Test fixtures ────────────────────────────────────────────────────
// Defined as functions to avoid F# module initialization issues with xUnit

let private mkTestItems () =
    [ { Sku = "SKU-001"; Name = "Widget"; Quantity = 2 }
      { Sku = "SKU-002"; Name = "Gadget"; Quantity = 1 } ]

let private mkTestCustomer () =
    { Name = "Alice"; Email = "alice@example.com" }

let private mkHappyRequest () =
    { Items = mkTestItems ()
      Customer = mkTestCustomer ()
      PaymentMethod = CreditCard
      Coupon = None }

// ═══════════════════════════════════════════════════════════════════════
// Tagless Final tests (Post 2)
// ═══════════════════════════════════════════════════════════════════════

module ``Tagless Final`` =

    [<Fact>]
    let ``happy path succeeds`` () =
        let result = OrderInterpreters.runTest true 99.50m true "test-txn-001" (mkHappyRequest ())
        match result with
        | Success txn -> Assert.Equal("test-txn-001", txn)
        | Failure r   -> Assert.Fail (sprintf "Expected Success, got: %s" r)

    [<Fact>]
    let ``out of stock fails`` () =
        let result = OrderInterpreters.runTest false 99.50m true "test-txn-001" (mkHappyRequest ())
        match result with
        | Failure r -> Assert.Equal("Out of stock", r)
        | _         -> Assert.Fail "Expected Failure"

    [<Fact>]
    let ``payment failed`` () =
        let result = OrderInterpreters.runTest true 99.50m false "test-txn-001" (mkHappyRequest ())
        match result with
        | Failure r -> Assert.Equal("Payment failed", r)
        | _         -> Assert.Fail "Expected Failure"

    [<Fact>]
    let ``narrative produces readable output`` () =
        let story = OrderInterpreters.runNarrative (mkHappyRequest ())
        Assert.Contains("Check if", story)
        Assert.Contains("item(s) are in stock", story)

    [<Fact>]
    let ``dry run records operations`` () =
        let entries = OrderInterpreters.runDryRun (mkHappyRequest ())
        let ops = entries |> List.map (fun e -> e.Operation)
        Assert.Contains("CheckStock", ops)

    [<Fact>]
    let ``same program, different interpreters`` () =
        let req = mkHappyRequest ()
        let test      = OrderInterpreters.runTest true 50m true "test-txn-001" req
        let narrative  = OrderInterpreters.runNarrative req
        let audit      = OrderInterpreters.runDryRun req
        Assert.True(test.Succeeded)
        Assert.False(System.String.IsNullOrEmpty narrative)
        Assert.True(audit.Length >= 1)

// ═══════════════════════════════════════════════════════════════════════
// Free Monad tests (Post 3)
// ═══════════════════════════════════════════════════════════════════════

module ``Free Monad`` =

    [<Fact>]
    let ``happy path succeeds`` () =
        let result = FreeMonad.placeOrder (mkHappyRequest ()) |> Interpreter.runSync Interpreter.defaultExecutor
        match result with
        | Success txn -> Assert.Equal("txn-test-001", txn)
        | Failure r   -> Assert.Fail (sprintf "Expected Success, got: %s" r)

    [<Fact>]
    let ``out of stock throws`` () =
        let executor step =
            match step with
            | CheckStock _ -> box { IsAvailable = false }
            | _ -> Interpreter.defaultExecutor step
        Assert.Throws<OrderFailed>(fun () ->
            FreeMonad.placeOrder (mkHappyRequest ()) |> Interpreter.runSync executor |> ignore)
        |> ignore

    [<Fact>]
    let ``payment failed throws`` () =
        let executor step =
            match step with
            | ChargePayment _ -> box { Succeeded = false; TransactionId = None }
            | _ -> Interpreter.defaultExecutor step
        Assert.Throws<OrderFailed>(fun () ->
            FreeMonad.placeOrder (mkHappyRequest ()) |> Interpreter.runSync executor |> ignore)
        |> ignore

// ─── Structural tests ────────────────────────────────────────────────

module ``Structural Analysis`` =

    [<Fact>]
    let ``has five steps`` () =
        Assert.Equal(5, FreeMonad.placeOrder (mkHappyRequest ()) |> Structure.countSteps)

    [<Fact>]
    let ``checks stock first`` () =
        let names = FreeMonad.placeOrder (mkHappyRequest ()) |> Structure.stepNames
        Assert.Equal("CheckStock", names.[0])

    [<Fact>]
    let ``checks stock before charging`` () =
        Assert.True(
            FreeMonad.placeOrder (mkHappyRequest ())
            |> Structure.appearsBefore "CheckStock" "ChargePayment")

    [<Fact>]
    let ``always sends confirmation`` () =
        Assert.True(
            FreeMonad.placeOrder (mkHappyRequest ())
            |> Structure.contains "SendConfirmation")

    [<Fact>]
    let ``steps in correct order`` () =
        let names = FreeMonad.placeOrder (mkHappyRequest ()) |> Structure.stepNames
        Assert.Equal<string list>(
            ["CheckStock"; "CalculatePrice"; "ChargePayment"; "ReserveInventory"; "SendConfirmation"],
            names)

// ─── Bind/guard tests ────────────────────────────────────────────────

module ``Program combinators`` =

    [<Fact>]
    let ``bind chains programs`` () =
        let program =
            Program.ret 10
            |> Program.bind (fun x ->
                Program.ret 20
                |> Program.bind (fun y ->
                    Program.ret (x + y)))
        let result = Interpreter.runSync (fun _ -> failwith "no steps") program
        Assert.Equal(30, result)

    [<Fact>]
    let ``guard short-circuits on false`` () =
        let program =
            Program.ret 5
            |> Program.bind (fun x ->
                Program.guard (x > 10) "too small"
                |> Program.bind (fun () -> Program.ret x))
        Assert.Throws<OrderFailed>(fun () ->
            Interpreter.runSync (fun _ -> failwith "no steps") program |> ignore)
        |> ignore

    [<Fact>]
    let ``guard continues on true`` () =
        let program =
            Program.ret 15
            |> Program.bind (fun x ->
                Program.guard (x > 10) "too small"
                |> Program.bind (fun () -> Program.ret x))
        let result = Interpreter.runSync (fun _ -> failwith "no steps") program
        Assert.Equal(15, result)

// ─── Execution plan tests ────────────────────────────────────────────

module ``Execution Plan`` =

    [<Fact>]
    let ``counts database calls`` () =
        let plan = FreeMonad.placeOrder (mkHappyRequest ()) |> Optimizer.analyze
        Assert.Equal(2, plan.DbCalls)

    [<Fact>]
    let ``counts payment calls`` () =
        let plan = FreeMonad.placeOrder (mkHappyRequest ()) |> Optimizer.analyze
        Assert.Equal(1, plan.PaymentCalls)

    [<Fact>]
    let ``counts email calls`` () =
        let plan = FreeMonad.placeOrder (mkHappyRequest ()) |> Optimizer.analyze
        Assert.Equal(1, plan.EmailCalls)

    [<Fact>]
    let ``estimates nonzero latency`` () =
        let plan = FreeMonad.placeOrder (mkHappyRequest ()) |> Optimizer.analyze
        Assert.True(plan.EstimatedLatencyMs > 0.0)

    [<Fact>]
    let ``estimates nonzero cost`` () =
        let plan = FreeMonad.placeOrder (mkHappyRequest ()) |> Optimizer.analyze
        Assert.True(plan.EstimatedCost > 0m)

    [<Fact>]
    let ``lists all steps`` () =
        let plan = FreeMonad.placeOrder (mkHappyRequest ()) |> Optimizer.analyze
        Assert.Equal(5, plan.Steps.Length)

// ─── Monad law tests (Post 5) ────────────────────────────────────────

module ``Monad Laws`` =

    let run program = Interpreter.runSync Interpreter.defaultExecutor program

    [<Fact>]
    let ``left identity: return a >>= f  ≡  f a`` () =
        let f x = Done (sprintf "value: %d" x)
        Assert.Equal(run (Program.bind f (Done 42)), run (f 42))

    [<Fact>]
    let ``right identity: m >>= return  ≡  m`` () =
        let m = Done 42
        Assert.Equal(run (Program.bind Done m), run m)

    [<Fact>]
    let ``associativity`` () =
        let m = Done 10
        let f x = Done (x + 5)
        let g x = Done (sprintf "result: %d" x)
        let left  = Program.bind g (Program.bind f m)
        let right = Program.bind (fun x -> Program.bind g (f x)) m
        Assert.Equal(run left, run right)

    [<Fact>]
    let ``failed short-circuits bind`` () =
        let mutable wasCalled = false
        let _ = Program.bind (fun (x: int) -> wasCalled <- true; Done "nope") (Failed "boom")
        Assert.False(wasCalled)
