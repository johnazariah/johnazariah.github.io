using IntentVsProcess.Domain;
using static IntentVsProcess.Free.OrderProgramExtensions;

namespace IntentVsProcess.Free;

// ─── Post 3: Programs as LINQ queries ─────────────────────────────────
//
// Same five-step order flow from Posts 1 and 2 — but now as DATA.
// PlaceOrder(request) returns a VALUE you can walk, analyze, optimize.

public static class OrderPrograms
{
    /// <summary>
    /// The standard order flow — no compensation.
    /// Read it aloud: "From the stock check, where stock is available,
    /// from the price calculation, from the charge... select success."
    /// </summary>
    public static OrderProgram<OrderResult> PlaceOrder(OrderRequest request) =>
        from stock in Lift(new CheckStock(request.Items))
        where stock.IsAvailable
        from price in Lift(new CalculatePrice(request.Items, request.Coupon))
        from charge in Lift(new ChargePayment(request.PaymentMethod, price.Total))
        where charge.Succeeded
        from _ in Lift(new ReserveInventory(request.Items))
        from __ in Lift(new SendConfirmation(request.Customer, price))
        select OrderResult.Ok(charge.TransactionId!);

    /// <summary>
    /// The order flow WITH declarative compensation.
    /// "Charge — and if something fails later, here's how to undo it."
    /// The saga interpreter walks the AST and unwinds on failure.
    /// </summary>
    public static OrderProgram<OrderResult> PlaceOrderWithCompensation(OrderRequest request) =>
        from stock in Lift(new CheckStock(request.Items))
        where stock.IsAvailable
        from price in Lift(new CalculatePrice(request.Items, request.Coupon))
        from charge in Lift(new WithCompensation<ChargeResult>(
                            new ChargePayment(request.PaymentMethod, price.Total),
                            result => new RefundPayment(result.TransactionId!)))
        where charge.Succeeded
        from _ in Lift(new WithCompensation<ReservationResult>(
                            new ReserveInventory(request.Items),
                            result => new ReleaseInventory(result.ReservationId!)))
        from __ in Lift(new SendConfirmation(request.Customer, price))
        select OrderResult.Ok(charge.TransactionId!);

    /// <summary>
    /// The order flow with EXPLICIT PARALLELISM.
    /// CheckStock and CalculatePrice are independent — neither uses the
    /// other's result. The Parallel combinator marks this in the AST,
    /// allowing a parallel-aware interpreter to run them concurrently.
    ///
    /// Compare with PlaceOrder above: same five steps, same business logic,
    /// but the data-dependency analysis is encoded in the program structure.
    /// </summary>
    public static OrderProgram<OrderResult> PlaceOrderParallel(OrderRequest request) =>
        from stockAndPrice in Parallel(
            Lift(new CheckStock(request.Items)),
            Lift(new CalculatePrice(request.Items, request.Coupon)))
        let stock = stockAndPrice.Item1
        let price = stockAndPrice.Item2
        where stock.IsAvailable
        from charge in Lift(new ChargePayment(request.PaymentMethod, price.Total))
        where charge.Succeeded
        from _ in Lift(new ReserveInventory(request.Items))
        from __ in Lift(new SendConfirmation(request.Customer, price))
        select OrderResult.Ok(charge.TransactionId!);
}
