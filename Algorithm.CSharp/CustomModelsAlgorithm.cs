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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of using custom fee, slippage, fill, and buying power models for modelling transactions in backtesting.
    /// QuantConnect allows you to model all orders as deeply and accurately as you need.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="transaction fees and slippage" />
    /// <meta name="tag" content="custom buying power models" />
    /// <meta name="tag" content="custom transaction models" />
    /// <meta name="tag" content="custom slippage models" />
    /// <meta name="tag" content="custom fee models" />
    public class CustomModelsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _security;
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 01);
            SetEndDate(2013, 10, 31);
            _security = AddEquity("SPY", Resolution.Hour);
            _spy = _security.Symbol;

            // set our models
            _security.SetFeeModel(new CustomFeeModel(this));
            _security.SetFillModel(new CustomFillModel(this));
            _security.SetSlippageModel(new CustomSlippageModel(this));
            _security.SetBuyingPowerModel(new CustomBuyingPowerModel(this));
        }

        public void OnData(TradeBars data)
        {
            var openOrders = Transactions.GetOpenOrders(_spy);
            if (openOrders.Count != 0) return;

            if (Time.Day > 10 && _security.Holdings.Quantity <= 0)
            {
                var quantity = CalculateOrderQuantity(_spy, .5m);
                Log($"MarketOrder: {quantity}");
                MarketOrder(_spy, quantity, asynchronous: true); // async needed for partial fill market orders
            }
            else if (Time.Day > 20 && _security.Holdings.Quantity >= 0)
            {
                var quantity = CalculateOrderQuantity(_spy, -.5m);
                Log($"MarketOrder: {quantity}");
                MarketOrder(_spy, quantity, asynchronous: true); // async needed for partial fill market orders
            }
        }

        public class CustomFillModel : ImmediateFillModel
        {
            private readonly QCAlgorithm _algorithm;
            private readonly Random _random = new Random(387510346); // seed it for reproducibility
            private readonly Dictionary<long, decimal> _absoluteRemainingByOrderId = new Dictionary<long, decimal>();

            public CustomFillModel(QCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                // this model randomly fills market orders

                decimal absoluteRemaining;
                if (!_absoluteRemainingByOrderId.TryGetValue(order.Id, out absoluteRemaining))
                {
                    absoluteRemaining = order.AbsoluteQuantity;
                    _absoluteRemainingByOrderId.Add(order.Id, order.AbsoluteQuantity);
                }

                var fill = base.MarketFill(asset, order);
                var absoluteFillQuantity = (int) (Math.Min(absoluteRemaining, _random.Next(0, 2*(int)order.AbsoluteQuantity)));
                fill.FillQuantity = Math.Sign(order.Quantity) * absoluteFillQuantity;

                if (absoluteRemaining == absoluteFillQuantity)
                {
                    fill.Status = OrderStatus.Filled;
                    _absoluteRemainingByOrderId.Remove(order.Id);
                }
                else
                {
                    absoluteRemaining = absoluteRemaining - absoluteFillQuantity;
                    _absoluteRemainingByOrderId[order.Id] = absoluteRemaining;
                    fill.Status = OrderStatus.PartiallyFilled;
                }

                _algorithm.Log($"CustomFillModel: {fill}");

                return fill;
            }
        }

        public class CustomFeeModel : FeeModel
        {
            private readonly QCAlgorithm _algorithm;

            public CustomFeeModel(QCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public override OrderFee GetOrderFee(OrderFeeParameters parameters)
            {
                // custom fee math
                var fee = Math.Max(
                    1m,
                    parameters.Security.Price*parameters.Order.AbsoluteQuantity*0.00001m);

                _algorithm.Log($"CustomFeeModel: {fee}");
                return new OrderFee(new CashAmount(fee, "USD"));
            }
        }

        public class CustomSlippageModel : ISlippageModel
        {
            private readonly QCAlgorithm _algorithm;

            public CustomSlippageModel(QCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public decimal GetSlippageApproximation(Security asset, Order order)
            {
                // custom slippage math
                var slippage = asset.Price*0.0001m*(decimal) Math.Log10(2*(double) order.AbsoluteQuantity);

                _algorithm.Log($"CustomSlippageModel: {slippage}");
                return slippage;
            }
        }

        public class CustomBuyingPowerModel : BuyingPowerModel
        {
            private readonly QCAlgorithm _algorithm;

            public CustomBuyingPowerModel(QCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public override HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
                HasSufficientBuyingPowerForOrderParameters parameters)
            {
                // custom behavior: this model will assume that there is always enough buying power
                var hasSufficientBuyingPowerForOrderResult = new HasSufficientBuyingPowerForOrderResult(true);
                _algorithm.Log($"CustomBuyingPowerModel: {hasSufficientBuyingPowerForOrderResult.IsSufficient}");

                return hasSufficientBuyingPowerForOrderResult;
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
        public long DataPoints => 330;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "62"},
            {"Average Win", "0.11%"},
            {"Average Loss", "-0.06%"},
            {"Compounding Annual Return", "-7.236%"},
            {"Drawdown", "2.400%"},
            {"Expectancy", "-0.187"},
            {"Net Profit", "-0.629%"},
            {"Sharpe Ratio", "-1.281"},
            {"Probabilistic Sharpe Ratio", "21.874%"},
            {"Loss Rate", "70%"},
            {"Win Rate", "30%"},
            {"Profit-Loss Ratio", "1.73"},
            {"Alpha", "-0.096"},
            {"Beta", "0.122"},
            {"Annual Standard Deviation", "0.04"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-4.126"},
            {"Tracking Error", "0.102"},
            {"Treynor Ratio", "-0.417"},
            {"Total Fees", "$62.25"},
            {"Estimated Strategy Capacity", "$52000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Return Over Maximum Drawdown", "-3.337"},
            {"Portfolio Turnover", "2.562"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "1118fb362bfe261323a6b496d50bddde"}
        };
    }
}
