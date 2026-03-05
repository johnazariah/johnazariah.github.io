using IntentVsProcess.Domain;

namespace IntentVsProcess.HKT;

// ─── The "proper" Tagless Final algebra using HKTs ────────────────────
//
// Compare with IOrderAlgebra<TResult> in the TaglessFinal project:
//   - That version uses a single TResult and runtime casts in Then<T>
//   - This version is fully type-safe — each operation returns IKind<TBrand, T>
//     where T is the operation's specific result type
//
// The trade-off: more boilerplate for type safety.
// In languages with native HKTs (Haskell, Scala), this is free.

/// <summary>
/// The order algebra parameterized by a type constructor brand.
/// Each operation returns IKind&lt;TBrand, T&gt; — the brand wrapping
/// the operation's specific result type.
/// </summary>
public interface IOrderAlgebraHKT<TBrand>
{
    // ── Domain operations (type-safe: each returns its specific result) ──

    IKind<TBrand, StockResult> CheckStock(List<Item> items);
    IKind<TBrand, PriceResult> CalculatePrice(List<Item> items, Coupon? coupon);
    IKind<TBrand, ChargeResult> ChargePayment(PaymentMethod method, decimal amount);
    IKind<TBrand, ReservationResult> ReserveInventory(List<Item> items);
    IKind<TBrand, Unit> SendConfirmation(Customer customer, PriceResult price);

    // ── Monadic operations ──

    /// <summary>Lift a pure value into the effect.</summary>
    IKind<TBrand, T> Pure<T>(T value);

    /// <summary>
    /// Sequence: run ma, extract its A, feed to the function that produces the next effect.
    /// This is the type-safe version of Then — no casts needed.
    /// </summary>
    IKind<TBrand, B> Bind<A, B>(IKind<TBrand, A> ma, Func<A, IKind<TBrand, B>> f);

    /// <summary>Guard with access to the typed value.</summary>
    IKind<TBrand, T> Guard<T>(
        IKind<TBrand, T> value,
        Func<T, bool> predicate,
        string failureReason);
}

// ─── Example: synchronous (identity) interpreter using HKTs ───────────

/// <summary>
/// Test interpreter using the HKT algebra.
/// Compare with TestInterpreter in the TaglessFinal project:
/// no casts, no Eval wrapper, fully type-safe.
/// </summary>
public class TestInterpreterHKT(
    bool stockAvailable = true,
    decimal price = 99.50m,
    bool chargeSucceeds = true,
    string transactionId = "test-txn-hkt") : IOrderAlgebraHKT<IdBrand>
{
    private readonly bool _stockAvailable = stockAvailable;
    private readonly decimal _price = price;
    private readonly bool _chargeSucceeds = chargeSucceeds;
    private readonly string _transactionId = transactionId;

    public IKind<IdBrand, StockResult> CheckStock(List<Item> items) =>
        new IdKind<StockResult>(new StockResult(_stockAvailable));

    public IKind<IdBrand, PriceResult> CalculatePrice(List<Item> items, Coupon? coupon)
    {
        var discount = coupon != null ? _price * coupon.DiscountPercent / 100m : 0m;
        return new IdKind<PriceResult>(new PriceResult(_price - discount, _price, discount));
    }

    public IKind<IdBrand, ChargeResult> ChargePayment(PaymentMethod method, decimal amount) =>
        new IdKind<ChargeResult>(
            new ChargeResult(_chargeSucceeds, _chargeSucceeds ? _transactionId : null));

    public IKind<IdBrand, ReservationResult> ReserveInventory(List<Item> items) =>
        new IdKind<ReservationResult>(new ReservationResult("res-hkt-001"));

    public IKind<IdBrand, Unit> SendConfirmation(Customer customer, PriceResult price) =>
        new IdKind<Unit>(Unit.Value);

    public IKind<IdBrand, T> Pure<T>(T value) =>
        new IdKind<T>(value);

    // Fully type-safe: no casts anywhere
    public IKind<IdBrand, B> Bind<A, B>(IKind<IdBrand, A> ma, Func<A, IKind<IdBrand, B>> f) =>
        f(ma.Unwrap());

    public IKind<IdBrand, T> Guard<T>(
        IKind<IdBrand, T> value,
        Func<T, bool> predicate,
        string failureReason)
    {
        var v = value.Unwrap();
        return predicate(v)
            ? value
            : throw new OrderFailedException(failureReason);
    }
}

// ─── Programs using the HKT algebra ──────────────────────────────────

public static class OrderProgramsHKT
{
    /// <summary>
    /// PlaceOrder using the type-safe HKT algebra.
    /// Compare with OrderPrograms.PlaceOrder in the TaglessFinal project.
    /// Same logic, same nesting, but no runtime casts.
    /// </summary>
    public static IKind<TBrand, OrderResult> PlaceOrder<TBrand>(
        IOrderAlgebraHKT<TBrand> alg,
        OrderRequest request) =>
        alg.Bind(
            alg.CheckStock(request.Items),
            stock =>
            {
                var guarded = alg.Guard(
                    alg.Pure(stock),
                    s => s.IsAvailable,
                    "Out of stock");
                return alg.Bind(guarded, _ =>
                    alg.Bind(
                        alg.CalculatePrice(request.Items, request.Coupon),
                        price => alg.Bind(
                            alg.ChargePayment(request.PaymentMethod, price.Total),
                            charge =>
                            {
                                var chargeGuard = alg.Guard(
                                    alg.Pure(charge),
                                    c => c.Succeeded,
                                    "Payment failed");
                                return alg.Bind(chargeGuard, _ =>
                                    alg.Bind(
                                        alg.ReserveInventory(request.Items),
                                        __ => alg.Bind(
                                            alg.SendConfirmation(request.Customer, price),
                                            ___ => alg.Pure(
                                                OrderResult.Ok(charge.TransactionId!))
                                        )
                                    )
                                );
                            }
                        )
                    )
                );
            }
        );
}
