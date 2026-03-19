using IntentVsProcess.Domain;

namespace IntentVsProcess.TaglessFinal;

// ─── Post 2: Programs written against the algebra ─────────────────────
//
// These functions say NOTHING about how anything happens.
// No await. No sync/async decision. No retry policy. No logging.
// Pure intent: validate, price, charge, reserve, notify, done.

public static class OrderPrograms
{
    /// <summary>
    /// PlaceOrder as a generic function over any algebra.
    /// The same function drives production, test, narrative, and dry-run interpreters.
    /// </summary>
    public static TResult PlaceOrder<TResult>(
        IOrderAlgebra<TResult> alg,
        OrderRequest request) =>
        alg.Then<StockResult>(
            alg.CheckStock(request.Items),
            stock => alg.Guard(
                () => stock.IsAvailable,
                () => alg.Then<PriceResult>(
                    alg.CalculatePrice(request.Items, request.Coupon),
                    price => alg.Then<ChargeResult>(
                        alg.ChargePayment(request.PaymentMethod, price.Total),
                        charge => alg.Guard(
                            () => charge.Succeeded,
                            () => alg.Then<ReservationResult>(
                                alg.ReserveInventory(request.Items),
                                _ => alg.Then<Unit>(
                                    alg.SendConfirmation(request.Customer, price),
                                    __ => alg.Done(OrderResult.Ok(charge.TransactionId!))
                                )
                            ),
                            "Payment failed"
                        )
                    )
                ),
                "Out of stock"
            )
        );

    /// <summary>
    /// PlaceOrder with explicit parallelism.
    /// CheckStock and CalculatePrice don't depend on each other —
    /// use Both to mark them as independent. A parallel-aware interpreter
    /// (or the ToFreeMonad interpreter) can exploit this.
    /// </summary>
    public static TResult PlaceOrderParallel<TResult>(
        IOrderAlgebra<TResult> alg,
        OrderRequest request) =>
        alg.Both<StockResult, PriceResult>(
            alg.CheckStock(request.Items),
            alg.CalculatePrice(request.Items, request.Coupon),
            (stock, price) => alg.Guard(
                () => stock.IsAvailable,
                () => alg.Then<ChargeResult>(
                    alg.ChargePayment(request.PaymentMethod, price.Total),
                    charge => alg.Guard(
                        () => charge.Succeeded,
                        () => alg.Then<ReservationResult>(
                            alg.ReserveInventory(request.Items),
                            _ => alg.Then<Unit>(
                                alg.SendConfirmation(request.Customer, price),
                                __ => alg.Done(OrderResult.Ok(charge.TransactionId!))
                            )
                        ),
                        "Payment failed"
                    )
                ),
                "Out of stock"
            )
        );
}
