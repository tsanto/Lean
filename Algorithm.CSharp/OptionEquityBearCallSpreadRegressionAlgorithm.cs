/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm exercising an equity Bear Call Spread option strategy and asserting it's being detected by Lean and works as expected
    /// </summary>
    public class OptionEquityBearCallSpreadRegressionAlgorithm : OptionEquityBaseStrategyRegressionAlgorithm
    {
        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                OptionChain chain;
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    var callContracts = chain
                        .Where(contract => contract.Right == OptionRight.Call)
                        .OrderByDescending(x => x.Expiry)
                        .ThenBy(x => x.Strike)
                        .ToList();

                    var shortCall = callContracts.First();
                    var longCall = callContracts.First(contract => contract.Strike > shortCall.Strike && contract.Expiry == shortCall.Expiry);

                    var initialMargin = Portfolio.MarginRemaining;

                    MarketOrder(shortCall.Symbol, -10);

                    AssertDefaultGroup(shortCall.Symbol, -10);

                    MarketOrder(longCall.Symbol, 10);
                    var freeMarginPostTrade = Portfolio.MarginRemaining;

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.BearCallSpread.Name, 10);

                    var expectedMarginUsage = (longCall.Strike - shortCall.Strike) * Securities[longCall.Symbol].SymbolProperties.ContractMultiplier * 10;

                    if (expectedMarginUsage != Portfolio.TotalMarginUsed)
                    {
                        throw new Exception("Unexpect margin used!");
                    }

                    // we payed the ask and value using the assets price
                    var priceSpreadDifference = GetPriceSpreadDifference(longCall.Symbol, shortCall.Symbol);
                    if (initialMargin != (freeMarginPostTrade + expectedMarginUsage + _paidFees - priceSpreadDifference))
                    {
                        throw new Exception("Unexpect margin remaining!");
                    }
                }
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 475788;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$5.00"},
            {"Estimated Strategy Capacity", "$2700000.00"},
            {"Lowest Capacity Asset", "GOOCV WBGM95TAH2LI|GOOCV VP83T1ZUHROL"},
            {"Return Over Maximum Drawdown", "0"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "54d92f54e9e61dda2013ec25487c204e"}
        };
    }
}
