module MonadLawTest (tests) where

import Test.Tasty
import Test.Tasty.HUnit

import IntentVsProcess.Domain
import IntentVsProcess.Free
import IntentVsProcess.Free.Interpreter

import Data.Text (Text)

-- Helper: run a program to get a result for comparison
run :: OrderProgram a -> Either Text a
run = runPure

-- ─── Tests ─────────────────────────────────────────────────────────

tests :: TestTree
tests = testGroup "Monad Laws (Post 5)"
  [ testCase "left identity: pure a >>= f  ≡  f a" $ do
      let f :: Int -> OrderProgram String
          f x = Pure ("value: " <> show x)
      run (Pure 42 >>= f) @?= run (f 42)

  , testCase "right identity: m >>= pure  ≡  m" $ do
      let m :: OrderProgram Int
          m = Pure 42
      run (m >>= Pure) @?= run m

  , testCase "associativity" $ do
      let m :: OrderProgram Int
          m = Pure 10
          f x = Pure (x + 5)
          g x = Pure ("result: " <> show x)
      run ((m >>= f) >>= g) @?= run (m >>= (\x -> f x >>= g))

  , testCase "associativity with steps" $ do
      let m = checkStock [Item "SKU-001" "Widget" 1]
          f _ = calculatePrice [Item "SKU-001" "Widget" 1] Nothing
          g price = Pure ("total: " <> show (priceTotal price))
      run ((m >>= f) >>= g) @?= run (m >>= (\x -> f x >>= g))

  , testCase "Fail short-circuits >>=" $ do
      let failed :: OrderProgram Int
          failed = Fail "boom"
          -- If bind evaluates the function, this would blow up
          result = failed >>= (\_ -> error "should not be called")
      case result of
        Fail e  -> e @?= "boom"
        _       -> assertFailure "expected Fail"

  , testCase "Fail short-circuits fmap" $ do
      let failed :: OrderProgram Int
          failed = Fail "boom"
          result = fmap (* 2) failed
      case result of
        Fail e  -> e @?= "boom"
        _       -> assertFailure "expected Fail"
  ]
