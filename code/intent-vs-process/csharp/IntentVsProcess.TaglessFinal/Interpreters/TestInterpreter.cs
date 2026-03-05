using IntentVsProcess.Domain;

namespace IntentVsProcess.TaglessFinal.Interpreters;

// ─── Post 2: Test interpreter ─────────────────────────────────────────
// Pure, in-memory, deterministic. No I/O, no external dependencies.
// Not a mock — a real implementation of the algebra.
// If you add a new operation to the algebra, the compiler forces you
// to implement it here. No silent mock drift.

public class TestInterpreter(
    bool stockAvailable = true,
    decimal price = 99.50m,
    bool chargeSucceeds = true,
    string transactionId = "test-txn-123") : IOrderAlgebra<Eval>
{
    private readonly bool _stockAvailable = stockAvailable;
    private readonly decimal _price = price;
    private readonly bool _chargeSucceeds = chargeSucceeds;
    private readonly string _transactionId = transactionId;

    public Eval CheckStock(List<Item> items) =>
        Eval.Of(new StockResult(_stockAvailable));

    public Eval CalculatePrice(List<Item> items, Coupon? coupon)
    {
        var discount = coupon != null ? _price * coupon.DiscountPercent / 100m : 0m;
        return Eval.Of(new PriceResult(_price - discount, _price, discount));
    }

    public Eval ChargePayment(PaymentMethod method, decimal amount) =>
        Eval.Of(new ChargeResult(_chargeSucceeds, _chargeSucceeds ? _transactionId : null));

    public Eval ReserveInventory(List<Item> items) =>
        Eval.Of(new ReservationResult("res-test-001"));

    public Eval SendConfirmation(Customer customer, PriceResult price) =>
        Eval.Of(Unit.Value);

    public Eval Then<T>(Eval first, Func<T, Eval> next) =>
        first switch
        {
            Eval.Fail => first,     // propagate failure
            _ => next(first.Unwrap<T>())
        };

    public Eval Done(OrderResult result) => Eval.Of(result);

    public Eval Guard(Func<bool> predicate, Func<Eval> onSuccess, string failureReason) =>
        predicate() ? onSuccess() : Eval.Failed(failureReason);
}
