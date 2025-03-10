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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting warming up with a lower resolution for speed is respected
    /// </summary>
    public class WarmupDailyResolutionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private long _previousSampleCount;
        private bool _warmedUpTradeBars;
        private bool _warmedUpQuoteBars;

        protected SimpleMovingAverage Sma { get; set; }
        protected TimeSpan ExpectedDataSpan { get; set; }
        protected TimeSpan ExpectedWarmupDataSpan { get; set; }

        public override void Initialize()
        {
            SetStartDate(2013, 10, 10);
            SetEndDate(2013, 10, 11);

            AddEquity("SPY", Resolution.Hour);
            ExpectedDataSpan = Resolution.Hour.ToTimeSpan();

            SetWarmUp(TimeSpan.FromDays(3), Resolution.Daily);
            ExpectedWarmupDataSpan = Resolution.Daily.ToTimeSpan();

            Sma = SMA("SPY", 2);
        }

        public override void OnData(Slice data)
        {
            if (Sma.Samples <= _previousSampleCount)
            {
                throw new Exception("Indicator was not updated!");
            }
            _previousSampleCount = Sma.Samples;

            var tradeBars = data.Get<TradeBar>();
            tradeBars.TryGetValue("SPY", out var trade);

            var quoteBars = data.Get<QuoteBar>();
            quoteBars.TryGetValue("SPY", out var quote);

            var expectedPeriod = ExpectedDataSpan;
            if (Time <= StartDate)
            {
                expectedPeriod = ExpectedWarmupDataSpan;
                if (trade != null && trade.IsFillForward || quote != null && quote.IsFillForward)
                {
                    throw new Exception("Unexpected fill forwarded data!");
                }
            }

            // let's assert the data's time are what we expect
            if (trade != null && trade.EndTime.Ticks % expectedPeriod.Ticks != 0)
            {
                throw new Exception($"Unexpected data end time! {trade.EndTime}");
            }
            if (quote != null && quote.EndTime.Ticks % expectedPeriod.Ticks != 0)
            {
                throw new Exception($"Unexpected data end time! {quote.EndTime}");
            }

            if (trade != null)
            {
                _warmedUpTradeBars |= IsWarmingUp;
                if (trade.Period != expectedPeriod)
                {
                    throw new Exception($"Unexpected period for trade data point {trade.Period} expected {expectedPeriod}. IsWarmingUp: {IsWarmingUp}");
                }
            }
            if (quote != null)
            {
                _warmedUpQuoteBars |= IsWarmingUp;
                if (quote.Period != expectedPeriod)
                {
                    throw new Exception($"Unexpected period for quote data point {quote.Period} expected {expectedPeriod}. IsWarmingUp: {IsWarmingUp}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_warmedUpTradeBars)
            {
                throw new Exception("Did not assert data during warmup!");
            }

            if (ExpectedWarmupDataSpan == QuantConnect.Time.OneDay)
            {
                if (_warmedUpQuoteBars)
                {
                    throw new Exception("We should of not gotten any quote bar during warmup for daily resolution!");
                }
            }
            else if (!_warmedUpQuoteBars)
            {
                throw new Exception("Did not assert data during warmup!");
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
        public virtual long DataPoints => 37;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
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
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
