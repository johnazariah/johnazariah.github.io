module Main (main) where

import Test.Tasty

import qualified TaglessFinalTest
import qualified FreeMonadTest
import qualified MonadLawTest
import qualified OptimizerTest

main :: IO ()
main = defaultMain $ testGroup "Intent vs Process"
  [ TaglessFinalTest.tests
  , FreeMonadTest.tests
  , MonadLawTest.tests
  , OptimizerTest.tests
  ]
