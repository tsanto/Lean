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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of future warmup
    /// </summary>
    public class WarmupFutureRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        // S&P 500 EMini futures
        private const string RootSP500 = Futures.Indices.SP500EMini;
        public Symbol SP500 = QuantConnect.Symbol.Create(RootSP500, SecurityType.Future, Market.CME);

        protected List<DateTime> ContinuousWarmupTimes { get; } = new();
        protected List<DateTime> ChainWarmupTimes { get; } = new();

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);

            var futureSP500 = AddFuture(RootSP500);
            futureSP500.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));

            SetWarmUp(1, Resolution.Daily);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if(IsWarmingUp && slice.ContainsKey(SP500))
            {
                if (Securities[SP500].AskPrice == 0)
                {
                    throw new Exception("Continuous contract price is not set!");
                }
                ContinuousWarmupTimes.Add(Time);
            }

            foreach (var chain in slice.FutureChains)
            {
                // find the front contract expiring no earlier than in 90 days
                var contract = (
                    from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                    where futuresContract.Expiry > Time.Date.AddDays(90)
                    select futuresContract
                ).FirstOrDefault();

                // if found, trade it
                if (contract != null)
                {
                    if (IsWarmingUp)
                    {
                        if (contract.AskPrice == 0)
                        {
                            throw new Exception("Contract price is not set!");
                        }
                        ChainWarmupTimes.Add(Time);
                    }
                    else if (!Portfolio.Invested && IsMarketOpen(contract.Symbol))
                    {
                        MarketOrder(contract.Symbol, 1);
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            AssertDataTime(new DateTime(2013, 10, 07, 20, 0, 0), new DateTime(2013, 10, 08, 20, 0, 0), ChainWarmupTimes);
            AssertDataTime(new DateTime(2013, 10, 07, 20, 0, 0), new DateTime(2013, 10, 08, 20, 0, 0), ContinuousWarmupTimes);
        }

        protected void AssertDataTime(DateTime start, DateTime end, List<DateTime> times)
        {
            var count = 0;
            do
            {
                if (Securities[SP500].Exchange.Hours.IsOpen(start.AddMinutes(-1), false))
                {
                    if (times[count] != start)
                    {
                        throw new Exception($"Unexpected time {times[count]} expected {start}");
                    }
                    // if the market is closed there will be no data, so stop moving the index counter
                    count++;
                }
                if (Settings.WarmupResolution.HasValue)
                {
                    start = start.Add(Settings.WarmupResolution.Value.ToTimeSpan());
                }
                else
                {
                    start = start.AddMinutes(1);
                }
            }
            while (start < end);
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 21679;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "130.234%"},
            {"Drawdown", "1.400%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.621%"},
            {"Sharpe Ratio", "48"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-3.382"},
            {"Beta", "0.742"},
            {"Annual Standard Deviation", "0.18"},
            {"Annual Variance", "0.032"},
            {"Information Ratio", "-120.79"},
            {"Tracking Error", "0.063"},
            {"Treynor Ratio", "11.65"},
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$120000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Return Over Maximum Drawdown", "-62.461"},
            {"Portfolio Turnover", "0.28"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "8f7633f23a8b3e6fa396cdb0bbff970a"}
        };
    }
}
