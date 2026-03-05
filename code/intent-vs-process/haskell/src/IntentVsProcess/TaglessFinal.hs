-- | Post 2: The Algebra of Intent — Tagless Final encoding.
--
-- In Haskell, Tagless Final is just a type class.
-- No IKind<TBrand,T> hacks. No CPS. No ceremony.
-- A type class IS an algebra. An instance IS an interpreter.
-- This is the native habitat of the pattern.
--
-- Compare the C# version:
--
-- @
-- public interface IOrderAlgebra\<TResult\> {
--     TResult CheckStock(List\<Item\> items);
--     TResult Then\<T\>(TResult first, Func\<T, TResult\> next);
--     ...
-- }
-- @
--
-- Here, all of that is just:
--
-- @
-- class Monad m => OrderAlgebra m where
--   checkStock :: [Item] -> m StockResult
--   ...
-- @
--
-- The @m@ is the interpreter. @IO@, @Identity@, @Writer [AuditEntry]@ —
-- whatever you want. The HKT problem that C# needs brands and wrappers
-- to solve? Haskell's kind system handles it natively: @m@ has kind @* -> *@.
module IntentVsProcess.TaglessFinal
  ( -- * The algebra (type class)
    OrderAlgebra (..)
    -- * Programs written against the algebra
  , placeOrder
    -- * Interpreters
  , TestConfig (..)
  , runTest
  , runNarrative
  , runDryRun
  ) where

import IntentVsProcess.Domain

import Control.Monad          (when)
import Control.Monad.Writer   (Writer, tell, runWriter)
import Data.Text              (Text)
import qualified Data.Text as T

-- ═══════════════════════════════════════════════════════════════════════
-- The Algebra
-- ═══════════════════════════════════════════════════════════════════════

-- | The order algebra — the vocabulary of intent.
--
-- @m@ is the effect: @IO@ for production, @Identity@ for tests,
-- @Writer [Text]@ for narrative, @Writer [AuditEntry]@ for dry-run.
--
-- Each method returns @m result@ — a computation in the interpreter's
-- monad that produces a typed result. No boxing. No casting. No brands.
--
-- The @Monad m@ constraint gives us @>>=@ (sequencing) and @pure@
-- (completion) for free — we don't need @Then@ and @Done@ in the
-- algebra because Haskell's do-notation provides them.
class Monad m => OrderAlgebra m where
  checkStock       :: [Item] -> m StockResult
  calculatePrice   :: [Item] -> Maybe Coupon -> m PriceResult
  chargePayment    :: PaymentMethod -> Double -> m ChargeResult
  reserveInventory :: [Item] -> m ReservationResult
  sendConfirmation :: Customer -> PriceResult -> m ()
  -- | Short-circuit with a failure message. Not every monad needs this
  -- (IO can throw), but we include it for pure interpreters.
  orderFailed      :: Text -> m a

-- ═══════════════════════════════════════════════════════════════════════
-- The Program
-- ═══════════════════════════════════════════════════════════════════════

-- | Place an order — pure intent, no how.
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
-- In Haskell this is just do-notation — no LINQ extensions needed,
-- no SelectMany overloads, no computation expression builders.
-- The monad constraint on the type class provides everything.
placeOrder :: OrderAlgebra m => OrderRequest -> m OrderResult
placeOrder req = do
  stock <- checkStock (orderItems req)
  when (not $ stockIsAvailable stock) $
    orderFailed "Out of stock"

  price <- calculatePrice (orderItems req) (orderCoupon req)

  charge <- chargePayment (orderPaymentMethod req) (priceTotal price)
  when (not $ chargeSucceeded charge) $
    orderFailed "Payment failed"

  _ <- reserveInventory (orderItems req)
  sendConfirmation (orderCustomer req) price

  case chargeTransactionId charge of
    Just txn -> pure $ OrderSuccess txn
    Nothing  -> orderFailed "No transaction ID"

-- ═══════════════════════════════════════════════════════════════════════
-- Interpreter 1: Test — pure, deterministic, in-memory
-- ═══════════════════════════════════════════════════════════════════════

-- | Test configuration — same role as C#'s TestInterpreter constructor args.
data TestConfig = TestConfig
  { testStockAvailable :: !Bool
  , testPrice          :: !Double
  , testChargeSucceeds :: !Bool
  , testTransactionId  :: !Text
  } deriving stock (Eq, Show)

-- | A pure "Either Text a" monad — Left is failure, Right is success.
-- No IO. No mocks. No Moq. Just values.
newtype TestM a = TestM { runTestM :: TestConfig -> Either Text a }

instance Functor TestM where
  fmap f (TestM g) = TestM $ \cfg -> fmap f (g cfg)

instance Applicative TestM where
  pure x = TestM $ \_ -> Right x
  TestM f <*> TestM x = TestM $ \cfg -> f cfg <*> x cfg

instance Monad TestM where
  TestM m >>= f = TestM $ \cfg ->
    case m cfg of
      Left err -> Left err
      Right a  -> runTestM (f a) cfg

instance OrderAlgebra TestM where
  checkStock _ = TestM $ \cfg ->
    Right $ StockResult (testStockAvailable cfg)

  calculatePrice _ coupon = TestM $ \cfg ->
    let base = testPrice cfg
        disc = case coupon of
                 Just c  -> base * couponDiscountPercent c / 100
                 Nothing -> 0
    in Right $ PriceResult (base - disc) base disc

  chargePayment _ _ = TestM $ \cfg ->
    let txn = if testChargeSucceeds cfg
              then Just (testTransactionId cfg)
              else Nothing
    in Right $ ChargeResult (testChargeSucceeds cfg) txn

  reserveInventory _ = TestM $ \_ ->
    Right $ ReservationResult (Just "res-test-001")

  sendConfirmation _ _ = TestM $ \_ -> Right ()

  orderFailed reason = TestM $ \_ -> Left reason

-- | Run with the test interpreter. Returns @Either Text OrderResult@.
runTest :: TestConfig -> (forall m. OrderAlgebra m => m a) -> Either Text a
runTest cfg prog = runTestM prog cfg

-- ═══════════════════════════════════════════════════════════════════════
-- Interpreter 2: Narrative — produces a human-readable story
-- ═══════════════════════════════════════════════════════════════════════

instance OrderAlgebra (Writer [Text]) where
  checkStock items = do
    tell ["Check if " <> T.pack (show (length items)) <> " item(s) are in stock."]
    pure $ StockResult True  -- narrative always proceeds

  calculatePrice items _ = do
    tell ["Calculate price for " <> T.pack (show (length items)) <> " item(s)."]
    pure $ PriceResult 0 0 0

  chargePayment method _ = do
    tell ["Charge payment via " <> T.pack (show method) <> "."]
    pure $ ChargeResult True (Just "narrative-txn")

  reserveInventory items = do
    tell ["Reserve " <> T.pack (show (length items)) <> " item(s) in inventory."]
    pure $ ReservationResult (Just "narrative-res")

  sendConfirmation cust _ = do
    tell ["Send confirmation email to " <> customerEmail cust <> "."]

  orderFailed reason = do
    tell ["FAILED: " <> reason]
    pure undefined  -- narrative never actually fails

-- | Run with the narrative interpreter. Returns the story as [Text].
runNarrative :: (forall m. OrderAlgebra m => m a) -> (a, [Text])
runNarrative prog = runWriter prog

-- ═══════════════════════════════════════════════════════════════════════
-- Interpreter 3: Dry-run — records an audit trail
-- ═══════════════════════════════════════════════════════════════════════

instance OrderAlgebra (Writer [AuditEntry]) where
  checkStock items = do
    tell [AuditEntry "CheckStock" (T.pack (show (length items)) <> " item(s)")]
    pure $ StockResult True

  calculatePrice items _ = do
    tell [AuditEntry "CalculatePrice" (T.pack (show (length items)) <> " item(s)")]
    pure $ PriceResult 0 0 0

  chargePayment method _ = do
    tell [AuditEntry "ChargePayment" ("via " <> T.pack (show method))]
    pure $ ChargeResult True (Just "audit-txn")

  reserveInventory items = do
    tell [AuditEntry "ReserveInventory" (T.pack (show (length items)) <> " item(s)")]
    pure $ ReservationResult (Just "audit-res")

  sendConfirmation cust _ = do
    tell [AuditEntry "SendConfirmation" ("to " <> customerEmail cust)]

  orderFailed reason = do
    tell [AuditEntry "Failed" reason]
    pure undefined

-- | Run with the dry-run interpreter. Returns audit entries.
runDryRun :: (forall m. OrderAlgebra m => m a) -> (a, [AuditEntry])
runDryRun prog = runWriter prog
