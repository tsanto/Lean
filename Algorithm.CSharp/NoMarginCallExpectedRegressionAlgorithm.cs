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
*/

using System;
using System.Collections.Generic;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Margin model regression algorithm testing <see cref="PatternDayTradingMarginModel"/> and
    /// margin calls NOT being triggered when the market is about to close, GH issue 4064.
    /// Brother too <see cref="MarginCallClosedMarketRegressionAlgorithm"/>
    /// </summary>
    public class NoMarginCallExpectedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _marginCall;
        private Symbol _spy;
        private decimal _closedMarketLeverage;
        private decimal _openMarketLeverage;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            var security = AddEquity("SPY", Resolution.Minute);
            _spy = security.Symbol;

            _closedMarketLeverage = 2;
            _openMarketLeverage = 5;
            security.BuyingPowerModel = new PatternDayTradingMarginModel(_closedMarketLeverage, _openMarketLeverage);

            Schedule.On(
                DateRules.EveryDay(_spy),
                // 15 minutes before market close, because PatternDayTradingMarginModel starts using closed
                // market leverage 10 minutes before market closes.
                TimeRules.BeforeMarketClose(_spy, 15),
                () => {
                    // before market close we reduce our position to closed market leverage
                    SetHoldings(_spy, _closedMarketLeverage);
                }
            );

            Schedule.On(
                DateRules.EveryDay(_spy),
                TimeRules.AfterMarketOpen(_spy, 1), // 1 min so that price is set
                () => {
                    // at market open we increase our position to open market leverage
                    SetHoldings(_spy, _openMarketLeverage);
                }
            );
        }

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            _marginCall++;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_marginCall != 0)
            {
                throw new Exception($"We expected NO margin call to happen, {_marginCall} occurred");
            }
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
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "10"},
            {"Average Win", "2.45%"},
            {"Average Loss", "-1.97%"},
            {"Compounding Annual Return", "9644.133%"},
            {"Drawdown", "9.800%"},
            {"Expectancy", "0.346"},
            {"Net Profit", "6.030%"},
            {"Sharpe Ratio", "42.882"},
            {"Probabilistic Sharpe Ratio", "63.958%"},
            {"Loss Rate", "40%"},
            {"Win Rate", "60%"},
            {"Profit-Loss Ratio", "1.24"},
            {"Alpha", "28.369"},
            {"Beta", "3.698"},
            {"Annual Standard Deviation", "0.833"},
            {"Annual Variance", "0.693"},
            {"Information Ratio", "54.961"},
            {"Tracking Error", "0.614"},
            {"Treynor Ratio", "9.654"},
            {"Total Fees", "$109.28"},
            {"Estimated Strategy Capacity", "$8400000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Return Over Maximum Drawdown", "108.753"},
            {"Portfolio Turnover", "7.21"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "d43d7155325d65ad1eaeb740f46fa63c"}
        };
    }
}
