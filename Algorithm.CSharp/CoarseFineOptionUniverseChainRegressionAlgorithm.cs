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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to chain a coarse and fine universe selection with an option chain universe selection model
    /// that will add and remove an <see cref="OptionChainUniverse"/> for each symbol selected on fine
    /// </summary>
    public class CoarseFineOptionUniverseChainRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        // initialize our changes to nothing
        private SecurityChanges _changes = SecurityChanges.None;
        private int _optionCount;
        private Symbol _lastEquityAdded;
        private Symbol _aapl;
        private Symbol _twx;

        public override void Initialize()
        {
            _twx = QuantConnect.Symbol.Create("TWX", SecurityType.Equity, Market.USA);
            _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2014, 06, 04);
            SetEndDate(2014, 06, 06);

            var selectionUniverse = AddUniverse(enumerable => new[] { Time.Date <= new DateTime(2014, 6, 5) ? _twx : _aapl },
                enumerable => new[] { Time.Date <= new DateTime(2014, 6, 5) ? _twx : _aapl });

            AddUniverseOptions(selectionUniverse, universe =>
            {
                if (universe.Underlying == null)
                {
                    throw new Exception("Underlying data point is null! This shouldn't happen, each OptionChainUniverse handles and should provide this");
                }
                return universe.IncludeWeeklys()
                    .FrontMonth()
                    .Contracts(universe.Take(5));
            });
        }

        public override void OnData(Slice data)
        {
            // if we have no changes, do nothing
            if (_changes == SecurityChanges.None ||
                _changes.AddedSecurities.Any(security => security.Price == 0))
            {
                return;
            }

            // liquidate removed securities
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            foreach (var security in _changes.AddedSecurities)
            {
                if (!security.Symbol.HasUnderlying)
                {
                    _lastEquityAdded = security.Symbol;
                }
                else
                {
                    // options added should all match prev added security
                    if (security.Symbol.Underlying != _lastEquityAdded)
                    {
                        throw new Exception($"Unexpected symbol added {security.Symbol}");
                    }

                    _optionCount++;
                }

                SetHoldings(security.Symbol, 0.05m);

                var config = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(security.Symbol).ToList();

                if (!config.Any())
                {
                    throw new Exception($"Was expecting configurations for {security.Symbol}");
                }
                if (config.Any(dataConfig => dataConfig.DataNormalizationMode != DataNormalizationMode.Raw))
                {
                    throw new Exception($"Was expecting DataNormalizationMode.Raw configurations for {security.Symbol}");
                }
            }
            _changes = SecurityChanges.None;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes += changes;
        }

        public override void OnEndOfAlgorithm()
        {
            var config = SubscriptionManager.Subscriptions.ToList();
            if (config.Any(dataConfig => dataConfig.Symbol == _twx || dataConfig.Symbol.Underlying == _twx))
            {
                throw new Exception($"Was NOT expecting any configurations for {_twx} or it's options, since coarse/fine should have deselected it");
            }

            if (_optionCount == 0)
            {
                throw new Exception("Option universe chain did not add any option!");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 998462;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.12"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "2a6319d0d474f976e653dd1ebc42caac"}
        };
    }
}
