-- | Post 1: The "before" code — intent and process fused together.
--
-- This is the traditional approach where the business logic directly
-- calls effectful operations. The what and the how are inseparable.
module IntentVsProcess.Traditional
  ( placeOrder
  ) where

import IntentVsProcess.Domain

-- | The traditional approach: business logic fused with effects.
--
-- In C#, this would be an async method with awaits, try/catch, and
-- early returns. In Haskell, the IO monad makes the coupling visible:
-- every line is IO, mixing business intent with infrastructure.
placeOrder
  :: (  [Item] -> IO StockResult          )  -- ^ checkStock
  -> (  [Item] -> Maybe Coupon -> PriceResult )  -- ^ calculatePrice (pure)
  -> (  PaymentMethod -> Double -> IO ChargeResult )  -- ^ chargePayment
  -> (  [Item] -> IO ReservationResult    )  -- ^ reserveInventory
  -> (  Customer -> PriceResult -> IO ()  )  -- ^ sendConfirmation
  -> OrderRequest
  -> IO OrderResult
placeOrder checkStock calcPrice charge reserve notify req = do
  stock <- checkStock (orderItems req)
  if not (stockIsAvailable stock)
    then pure $ OrderFailure "Out of stock"
    else do
      let price = calcPrice (orderItems req) (orderCoupon req)
      result <- charge (orderPaymentMethod req) (priceTotal price)
      if not (chargeSucceeded result)
        then pure $ OrderFailure "Payment failed"
        else do
          _ <- reserve (orderItems req)
          notify (orderCustomer req) price
          case chargeTransactionId result of
            Just txn -> pure $ OrderSuccess txn
            Nothing  -> pure $ OrderFailure "No transaction ID"
