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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// </summary>
    public class OrderSubmissionDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Dictionary<string, OrderSubmissionData> _orderSubmissionData = new Dictionary<string, OrderSubmissionData>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            
            AddEquity("SPY");
            AddForex("EURUSD", Resolution.Hour);

            Schedule.On(DateRules.EveryDay(), TimeRules.Noon, () =>
            {
                Liquidate();
                foreach (var ticker in new[] {"SPY", "EURUSD"})
                {
                    PlaceTrade(ticker);
                }
            });
        }
        private void PlaceTrade(string ticker)
        {
            var ticket = MarketOrder(ticker, 1000);
            var order = Transactions.GetOrderById(ticket.OrderId);
            var data = order.OrderSubmissionData;
            if (data == null || data.AskPrice == 0 || data.BidPrice == 0 || data.LastPrice == 0)
            {
                throw new Exception("Invalid Order Submission data detected");
            }

            if (_orderSubmissionData.ContainsKey(ticker))
            {
                var previous = _orderSubmissionData[ticker];
                if (previous.AskPrice == data.AskPrice || previous.BidPrice == data.BidPrice || previous.LastPrice == data.LastPrice)
                {
                    throw new Exception("Order Submission data didn't change");
                }
            }
            _orderSubmissionData[ticker] = data;
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
        public long DataPoints => 10708;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 60;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "18"},
            {"Average Win", "0.83%"},
            {"Average Loss", "-0.90%"},
            {"Compounding Annual Return", "273.871%"},
            {"Drawdown", "3.200%"},
            {"Expectancy", "0.203"},
            {"Net Profit", "1.716%"},
            {"Sharpe Ratio", "11.414"},
            {"Probabilistic Sharpe Ratio", "67.016%"},
            {"Loss Rate", "38%"},
            {"Win Rate", "62%"},
            {"Profit-Loss Ratio", "0.93"},
            {"Alpha", "0.816"},
            {"Beta", "1.464"},
            {"Annual Standard Deviation", "0.326"},
            {"Annual Variance", "0.106"},
            {"Information Ratio", "16.804"},
            {"Tracking Error", "0.103"},
            {"Treynor Ratio", "2.54"},
            {"Total Fees", "$45.00"},
            {"Estimated Strategy Capacity", "$20000000.00"},
            {"Lowest Capacity Asset", "EURUSD 8G"},
            {"Return Over Maximum Drawdown", "48.904"},
            {"Portfolio Turnover", "2.58"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "bf0434a44121c3e61963c60ef9e15ee5"}
        };
    }
}
