namespace IntentVsProcess

// ═══════════════════════════════════════════════════════════════════════
// Free Monad — Post 3: Intent You Can See (and Optimize)
// ═══════════════════════════════════════════════════════════════════════

// ─── Effect functor ───────────────────────────────────────────────────

type OrderStep =
    | CheckStock of Item list
    | CalculatePrice of Item list * Coupon option
    | ChargePayment of PaymentMethod * decimal
    | ReserveInventory of Item list
    | SendConfirmation of Customer * PriceResult
    | RefundPayment of transactionId: string
    | ReleaseInventory of reservationId: string

// ─── Free Monad ───────────────────────────────────────────────────────

type Program<'a> =
    | Done of 'a
    | Failed of string
    | Step of OrderStep * (obj -> Program<'a>)

module Program =
    let ret x = Done x
    let fail reason = Failed reason

    let rec bind (f: 'a -> Program<'b>) (m: Program<'a>) : Program<'b> =
        match m with
        | Done a -> f a
        | Failed reason -> Failed reason
        | Step(step, k) -> Step(step, fun x -> bind f (k x))

    let map f m = bind (f >> ret) m

    let lift (step: OrderStep) : Program<'a> =
        Step(step, fun result -> Done(unbox<'a> result))

    let guard condition reason =
        if condition then Done() else Failed reason

// ─── Computation expression builder ──────────────────────────────────

type OrderBuilder() =
    member _.Return(x) = Program.ret x
    member _.ReturnFrom(m) = m
    member _.Bind(m, f) = Program.bind f m
    member _.Zero() = Program.ret ()

[<AutoOpen>]
module OrderBuilderInstance =
    let order = OrderBuilder()

// ─── Smart constructors ──────────────────────────────────────────────

module Order =
    let checkStock items : Program<StockResult> = Program.lift (CheckStock items)

    let calculatePrice items coupon : Program<PriceResult> =
        Program.lift (CalculatePrice(items, coupon))

    let chargePayment meth amount : Program<ChargeResult> =
        Program.lift (ChargePayment(meth, amount))

    let reserveInventory items : Program<ReservationResult> = Program.lift (ReserveInventory items)

    let sendConfirmation cust price : Program<unit> =
        Program.lift (SendConfirmation(cust, price))

    let guard cond reason : Program<unit> = Program.guard cond reason

// ─── Programs ─────────────────────────────────────────────────────────

module FreeMonad =

    let placeOrder (req: OrderRequest) : Program<OrderResult> =
        Order.checkStock req.Items
        |> Program.bind (fun stock ->
            Program.guard stock.IsAvailable "Out of stock"
            |> Program.bind (fun () ->
                Order.calculatePrice req.Items req.Coupon
                |> Program.bind (fun price ->
                    Order.chargePayment req.PaymentMethod price.Total
                    |> Program.bind (fun charge ->
                        Program.guard charge.Succeeded "Payment failed"
                        |> Program.bind (fun () ->
                            Order.reserveInventory req.Items
                            |> Program.bind (fun _ ->
                                Order.sendConfirmation req.Customer price
                                |> Program.bind (fun () ->
                                    let txnId = charge.TransactionId |> Option.defaultValue "unknown"
                                    Program.ret (Success txnId))))))))

    let placeOrderWithCompensation (req: OrderRequest) : Program<OrderResult> = placeOrder req // same structure for now

// ─── Interpreter ──────────────────────────────────────────────────────

module Interpreter =

    let rec runSync (execute: OrderStep -> obj) (program: Program<'a>) : 'a =
        match program with
        | Done a -> a
        | Failed reason -> raise (OrderFailed reason)
        | Step(step, k) -> runSync execute (k (execute step))

    let rec runAsync (execute: OrderStep -> Async<obj>) (program: Program<'a>) : Async<'a> =
        async {
            match program with
            | Done a -> return a
            | Failed reason -> return raise (OrderFailed reason)
            | Step(step, k) ->
                let! result = execute step
                return! runAsync execute (k result)
        }

    let defaultExecutor (step: OrderStep) : obj =
        match step with
        | CheckStock _ -> box { IsAvailable = true }
        | CalculatePrice _ ->
            box
                { Total = 99.50m
                  Subtotal = 99.50m
                  Discount = 0m }
        | ChargePayment _ ->
            box
                { Succeeded = true
                  TransactionId = Some "txn-test-001" }
        | ReserveInventory _ -> box { ReservationId = Some "res-test-001" }
        | SendConfirmation _ -> box ()
        | RefundPayment _ -> box ()
        | ReleaseInventory _ -> box ()

// ─── Structural analysis ─────────────────────────────────────────────

module Structure =

    let rec flatten (program: Program<'a>) : OrderStep list =
        match program with
        | Done _ -> []
        | Failed _ -> []
        | Step(step, k) ->
            let next =
                try
                    k (Interpreter.defaultExecutor step)
                with _ ->
                    Failed "structural"

            step :: flatten next

    let countSteps program = flatten program |> List.length

    let stepNames program =
        flatten program
        |> List.map (fun s ->
            match s with
            | CheckStock _ -> "CheckStock"
            | CalculatePrice _ -> "CalculatePrice"
            | ChargePayment _ -> "ChargePayment"
            | ReserveInventory _ -> "ReserveInventory"
            | SendConfirmation _ -> "SendConfirmation"
            | RefundPayment _ -> "RefundPayment"
            | ReleaseInventory _ -> "ReleaseInventory")

    let appearsBefore stepA stepB program =
        let names = stepNames program
        let idxA = names |> List.tryFindIndex ((=) stepA)
        let idxB = names |> List.tryFindIndex ((=) stepB)

        match idxA, idxB with
        | Some a, Some b -> a < b
        | _ -> false

    let contains stepName program =
        stepNames program |> List.contains stepName

// ─── Execution plan ──────────────────────────────────────────────────

type ExecutionPlan =
    { DbCalls: int
      PaymentCalls: int
      EmailCalls: int
      EstimatedLatencyMs: float
      EstimatedCost: decimal
      Steps: string list }

module Optimizer =

    let analyze (program: Program<OrderResult>) : ExecutionPlan =
        let steps = Structure.flatten program
        let mutable db, pay, email = 0, 0, 0
        let mutable latency, cost = 0.0, 0m
        let descs = ResizeArray()

        for step in steps do
            match step with
            | CheckStock items ->
                db <- db + 1
                latency <- latency + 50.0
                descs.Add(sprintf "DB: Check stock for %d item(s)" items.Length)
            | CalculatePrice(items, _) ->
                latency <- latency + 10.0
                descs.Add(sprintf "Compute: Calculate price for %d item(s)" items.Length)
            | ChargePayment(m, amt) ->
                pay <- pay + 1
                latency <- latency + 800.0
                cost <- cost + 0.03m
                descs.Add(sprintf "API: Charge %M via %A" amt m)
            | ReserveInventory items ->
                db <- db + 1
                latency <- latency + 50.0
                descs.Add(sprintf "DB: Reserve %d item(s)" items.Length)
            | SendConfirmation(c, _) ->
                email <- email + 1
                latency <- latency + 100.0
                cost <- cost + 0.001m
                descs.Add(sprintf "Email: Confirm to %s" c.Email)
            | _ -> descs.Add(sprintf "Other: %A" step)

        { DbCalls = db
          PaymentCalls = pay
          EmailCalls = email
          EstimatedLatencyMs = latency
          EstimatedCost = cost
          Steps = Seq.toList descs }
