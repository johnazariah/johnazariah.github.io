namespace IntentVsProcess

// ═══════════════════════════════════════════════════════════════════════
// Tagless Final — Post 2: The Algebra of Intent
// ═══════════════════════════════════════════════════════════════════════
//
// In Haskell, Tagless Final is just a type class:
//   class Monad m => OrderAlgebra m where
//     checkStock :: [Item] -> m StockResult
//
// F# doesn't have type classes. Instead, we model interpretations as
// functions from OrderRequest to the interpreter's result type.
// Each "interpreter" is a module function. The program IS the function
// — same intent, different interpretations, no mocks.

module OrderInterpreters =

    /// Test interpreter — pure, deterministic, no I/O.
    let runTest
        (stockOk: bool)
        (price: decimal)
        (chargeOk: bool)
        (txnId: string)
        (req: OrderRequest)
        : OrderResult =

        // Step 1: Check stock
        if not stockOk then
            Failure "Out of stock"
        else

        // Step 2: Calculate price
        let discount =
            match req.Coupon with
            | Some c -> price * c.DiscountPercent / 100m
            | None   -> 0m
        let _total = price - discount

        // Step 3: Charge payment
        if not chargeOk then
            Failure "Payment failed"
        else

        // Step 4 & 5: Reserve + confirm
        Success txnId

    /// Narrative interpreter — produces a human-readable story.
    let runNarrative (req: OrderRequest) : string =
        let items = req.Items
        let cust = req.Customer
        let pm = req.PaymentMethod
        [ sprintf "Check if %d item(s) are in stock." items.Length
          sprintf "Calculate price for %d item(s)." items.Length
          sprintf "Charge payment via %A." pm
          sprintf "Reserve %d item(s) in inventory." items.Length
          sprintf "Send confirmation to %s." cust.Email
          "Complete (narrative-txn)" ]
        |> String.concat "\n"

    /// Dry-run interpreter — records audit entries.
    let runDryRun (req: OrderRequest) : AuditEntry list =
        let items = req.Items
        let cust = req.Customer
        let pm = req.PaymentMethod
        [ { Operation = "CheckStock"
            Details = sprintf "%d item(s)" items.Length }
          { Operation = "CalculatePrice"
            Details = sprintf "%d item(s)" items.Length }
          { Operation = "ChargePayment"
            Details = sprintf "via %A" pm }
          { Operation = "ReserveInventory"
            Details = sprintf "%d item(s)" items.Length }
          { Operation = "SendConfirmation"
            Details = sprintf "to %s" cust.Email }
          { Operation = "Done"
            Details = "Success" } ]
