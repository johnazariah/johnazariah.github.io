-- | Interpreters for the Free Monad — giving meaning to the AST.
--
-- The interpreter is a fold (catamorphism) over the program tree.
-- Same concept as the C# @OrderInterpreter.RunSync@.
module IntentVsProcess.Free.Interpreter
  ( -- * Running programs
    runPure
  , runPureWith
    -- * Default executor
  , defaultExecutor
  , runPureDefault
  , Executor
  ) where

import IntentVsProcess.Domain
import IntentVsProcess.Free

import Data.Text (Text)

-- | An executor maps each instruction to its result in some monad.
-- In C# this was @Func<OrderStepBase, object>@ — here it's typed.
type Executor m = forall a. OrderStepF a -> m a

-- | Default test executor — deterministic happy-path results.
defaultExecutor :: Applicative m => Executor m
defaultExecutor step = case step of
  CheckStockF _ k       -> pure $ k (StockResult True)
  CalculatePriceF _ _ k -> pure $ k (PriceResult 99.50 99.50 0)
  ChargePaymentF _ _ k  -> pure $ k (ChargeResult True (Just "txn-test-001"))
  ReserveInventoryF _ k -> pure $ k (ReservationResult (Just "res-test-001"))
  SendConfirmationF _ _ next -> pure next
  RefundPaymentF _ next      -> pure next
  ReleaseInventoryF _ next   -> pure next

-- | Run a program purely with the default executor.
-- Returns @Either Text a@ — Left for failures, Right for success.
runPure :: OrderProgram a -> Either Text a
runPure (Pure a)   = Right a
runPure (Fail e)   = Left e
runPure (Free step) = case runPureDefault step of
  nextProgram -> runPure nextProgram

-- | Apply the default executor to get the next program.
runPureDefault :: OrderStepF (OrderProgram a) -> OrderProgram a
runPureDefault step = case step of
  CheckStockF _ k       -> k (StockResult True)
  CalculatePriceF _ _ k -> k (PriceResult 99.50 99.50 0)
  ChargePaymentF _ _ k  -> k (ChargeResult True (Just "txn-test-001"))
  ReserveInventoryF _ k -> k (ReservationResult (Just "res-test-001"))
  SendConfirmationF _ _ next -> next
  RefundPaymentF _ next      -> next
  ReleaseInventoryF _ next   -> next

-- | A step executor: given a step, produce the next program.
type StepExecutor a = OrderStepF (OrderProgram a) -> OrderProgram a

-- | Run with a custom executor function that handles each step.
runPureWith :: StepExecutor a -> OrderProgram a -> Either Text a
runPureWith _    (Pure a)    = Right a
runPureWith _    (Fail e)    = Left e
runPureWith exec (Free step) = runPureWith exec (exec step)
