-- | Post 3: Intent You Can See (and Optimize) — Free Monad encoding.
--
-- In C#, we needed:
--   - An abstract @OrderStepBase@ (non-generic base)
--   - Generic @OrderStep\<T\>@ records for each operation
--   - @OrderProgram\<T\>@ with @Done@, @Failed@, @Bind@ cases
--   - @SelectMany@ / @Select@ / @Where@ extension methods for LINQ
--   - Boxing and casting through @object@ in the continuation
--
-- In Haskell, all of that is:
--
-- @
-- data OrderStepF next where
--   CheckStock :: [Item] -> (StockResult -> next) -> OrderStepF next
--   ...
--
-- type OrderProgram = Free OrderStepF
-- @
--
-- GADTs give us typed instructions. @Free@ gives us the monad.
-- Do-notation gives us LINQ. No boxing, no casting, no ceremony.
module IntentVsProcess.Free
  ( -- * The instruction set (functor)
    OrderStepF (..)
    -- * The Free Monad
  , OrderProgram
  , Free (..)
    -- * Smart constructors
  , checkStock
  , calculatePrice
  , chargePayment
  , reserveInventory
  , sendConfirmation
  , orderFailed
    -- * Programs
  , placeOrder
  , placeOrderWithCompensation
    -- * Guards
  , guard'
    -- * Compensation
  , CompensationMeta (..)
  ) where

import IntentVsProcess.Domain
import Data.Text (Text)

-- ═══════════════════════════════════════════════════════════════════════
-- The Free Monad — general-purpose
-- ═══════════════════════════════════════════════════════════════════════

-- | The Free Monad over a functor @f@.
-- This is the same thing that Control.Monad.Free provides, but
-- we define it ourselves so you can see there's no magic.
--
-- @Pure@  = Done  = Return — the program has finished
-- @Free@  = Bind  = Then   — one instruction, then a continuation
-- @Fail@  = Failed         — short-circuit with an error
data Free f a
  = Pure a
  | Free (f (Free f a))
  | Fail Text

instance Functor f => Functor (Free f) where
  fmap g (Pure a)  = Pure (g a)
  fmap _ (Fail e)  = Fail e
  fmap g (Free fa) = Free (fmap (fmap g) fa)

instance Functor f => Applicative (Free f) where
  pure = Pure
  Pure f  <*> x = fmap f x
  Fail e  <*> _ = Fail e
  Free ff <*> x = Free (fmap (<*> x) ff)

instance Functor f => Monad (Free f) where
  Pure a  >>= f = f a
  Fail e  >>= _ = Fail e
  Free fa >>= f = Free (fmap (>>= f) fa)

-- | Lift a single instruction into the Free Monad.
liftF :: Functor f => f a -> Free f a
liftF fa = Free (fmap Pure fa)

-- | Short-circuit with a failure reason.
failWith :: Text -> Free f a
failWith = Fail

-- | Guard: if the condition is false, fail with the given reason.
-- This is the Haskell equivalent of the C# @Where@ LINQ extension.
guard' :: Functor f => Bool -> Text -> Free f ()
guard' True  _      = Pure ()
guard' False reason = Fail reason

-- ═══════════════════════════════════════════════════════════════════════
-- The Instruction Set — a GADT functor
-- ═══════════════════════════════════════════════════════════════════════

-- | The effect functor — each constructor is one domain operation.
--
-- The @next@ parameter is the continuation type — what the Free Monad
-- uses to thread the "rest of the program" through each instruction.
-- The GADT gives each operation its own typed result without boxing.
--
-- Compare the C# version:
--
-- @
-- public record CheckStock(List\<Item\> Items) : OrderStep\<StockResult\>;
-- @
--
-- Here:
--
-- @
-- CheckStock :: [Item] -> (StockResult -> next) -> OrderStepF next
-- @
--
-- The continuation @(StockResult -> next)@ is what C# hides inside
-- @Bind\<T\>@'s @Func\<object, OrderProgram\<T\>\>@, but here it's
-- typed — no casting required.
data OrderStepF next where
  CheckStockF       :: [Item] -> (StockResult -> next) -> OrderStepF next
  CalculatePriceF   :: [Item] -> Maybe Coupon -> (PriceResult -> next) -> OrderStepF next
  ChargePaymentF    :: PaymentMethod -> Double -> (ChargeResult -> next) -> OrderStepF next
  ReserveInventoryF :: [Item] -> (ReservationResult -> next) -> OrderStepF next
  SendConfirmationF :: Customer -> PriceResult -> next -> OrderStepF next
  -- Compensation steps
  RefundPaymentF    :: Text -> next -> OrderStepF next
  ReleaseInventoryF :: Text -> next -> OrderStepF next

instance Functor OrderStepF where
  fmap f (CheckStockF items k)         = CheckStockF items (f . k)
  fmap f (CalculatePriceF items c k)   = CalculatePriceF items c (f . k)
  fmap f (ChargePaymentF m amt k)      = ChargePaymentF m amt (f . k)
  fmap f (ReserveInventoryF items k)   = ReserveInventoryF items (f . k)
  fmap f (SendConfirmationF c p next)  = SendConfirmationF c p (f next)
  fmap f (RefundPaymentF txn next)     = RefundPaymentF txn (f next)
  fmap f (ReleaseInventoryF rid next)  = ReleaseInventoryF rid (f next)

-- | The program type — Free monad over OrderStepF.
type OrderProgram = Free OrderStepF

-- ═══════════════════════════════════════════════════════════════════════
-- Smart constructors — lift each instruction into the Free Monad
-- ═══════════════════════════════════════════════════════════════════════

checkStock :: [Item] -> OrderProgram StockResult
checkStock items = liftF $ CheckStockF items id

calculatePrice :: [Item] -> Maybe Coupon -> OrderProgram PriceResult
calculatePrice items coupon = liftF $ CalculatePriceF items coupon id

chargePayment :: PaymentMethod -> Double -> OrderProgram ChargeResult
chargePayment method amount = liftF $ ChargePaymentF method amount id

reserveInventory :: [Item] -> OrderProgram ReservationResult
reserveInventory items = liftF $ ReserveInventoryF items id

sendConfirmation :: Customer -> PriceResult -> OrderProgram ()
sendConfirmation cust price = liftF $ SendConfirmationF cust price ()

orderFailed :: Text -> OrderProgram a
orderFailed = failWith

-- ═══════════════════════════════════════════════════════════════════════
-- Programs — pure intent as data
-- ═══════════════════════════════════════════════════════════════════════

-- | Place an order — the same five steps, now as a data structure.
--
-- Compare the C# LINQ version:
--
-- @
-- from stock   in Lift(new CheckStock(request.Items))
-- where stock.IsAvailable
-- from price   in Lift(new CalculatePrice(...))
-- ...
-- @
--
-- In Haskell, do-notation IS the LINQ syntax — no extensions needed.
placeOrder :: OrderRequest -> OrderProgram OrderResult
placeOrder req = do
  stock <- checkStock (orderItems req)
  guard' (stockIsAvailable stock) "Out of stock"

  price <- calculatePrice (orderItems req) (orderCoupon req)

  charge <- chargePayment (orderPaymentMethod req) (priceTotal price)
  guard' (chargeSucceeded charge) "Payment failed"

  _ <- reserveInventory (orderItems req)
  sendConfirmation (orderCustomer req) price

  case chargeTransactionId charge of
    Just txn -> pure $ OrderSuccess txn
    Nothing  -> orderFailed "No transaction ID"

-- ═══════════════════════════════════════════════════════════════════════
-- Compensation metadata (for saga support)
-- ═══════════════════════════════════════════════════════════════════════

-- | Marks a step as compensatable with its rollback program.
data CompensationMeta = Compensatable
  { compensationRollback :: OrderProgram ()
  }

-- | Place an order with compensation annotations.
-- The saga interpreter can inspect these to build the rollback stack.
placeOrderWithCompensation :: OrderRequest -> OrderProgram OrderResult
placeOrderWithCompensation req = do
  stock <- checkStock (orderItems req)
  guard' (stockIsAvailable stock) "Out of stock"

  price <- calculatePrice (orderItems req) (orderCoupon req)

  charge <- chargePayment (orderPaymentMethod req) (priceTotal price)
  guard' (chargeSucceeded charge) "Payment failed"

  _reservation <- reserveInventory (orderItems req)
  sendConfirmation (orderCustomer req) price

  case chargeTransactionId charge of
    Just txn -> pure $ OrderSuccess txn
    Nothing  -> orderFailed "No transaction ID"
