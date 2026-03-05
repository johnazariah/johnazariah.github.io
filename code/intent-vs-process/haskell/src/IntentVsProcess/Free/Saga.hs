-- | Saga interpreter — compensation that writes itself.
--
-- The saga interpreter walks the AST, executes forward steps,
-- accumulates a compensation stack, and on failure unwinds it.
--
-- In Haskell this is much simpler than C# because we can carry
-- the compensation stack as an accumulating parameter.
module IntentVsProcess.Free.Saga
  ( runWithSaga
  ) where

import IntentVsProcess.Free

import Data.Text (Text)

-- | Run a program with saga-style compensation.
-- On failure, accumulated rollback actions are "executed" in reverse.
-- Returns @(Either Text a, [Text])@ — the result plus a rollback log.
runWithSaga
  :: (OrderStepF (OrderProgram a) -> OrderProgram a)  -- ^ executor
  -> OrderProgram a
  -> (Either Text a, [Text])  -- ^ (result, rollback actions taken)
runWithSaga exec = go []
  where
    go _compensations (Pure a) = (Right a, [])
    go compensations  (Fail e) =
      -- Unwind: execute compensations in reverse order
      (Left e, reverse compensations)
    go compensations (Free step) =
      let compensations' = addCompensation step compensations
          next = exec step
      in go compensations' next

    -- Track which steps have rollback actions
    addCompensation :: OrderStepF next -> [Text] -> [Text]
    addCompensation (ChargePaymentF _ _ _)  cs = "Refund payment" : cs
    addCompensation (ReserveInventoryF _ _) cs = "Release inventory" : cs
    addCompensation _                       cs = cs
