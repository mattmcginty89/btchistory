namespace Prices

open System
open System.Collections.Generic
open System.Linq
open EntityFrameworkCore.FSharp.DbContextHelpers
open BtcContext
open CoinGecko
open Util

/// <summary>
///     Module containing prices related methods to record and retrieve Bitcoin prices.
/// </summary>
module Prices =

    /// <summary>
    ///     Get historical Bitcoin prices from a given database context.
    /// </summary>
    /// <param name="ctx">Database context to get prices from.</param>
    /// <param name="days">Number of days worth of historical data to retrieve.</param>
    /// <seealso cref="PriceEntry"/>
    /// <returns>Collection of price entries.</returns>
    let getPrices (ctx: BtcContext) (days: int) =
        query {
            for p in ctx.PriceEntries do
                where (p.Timestamp > DateTime.UtcNow.AddDays(-days))
                select p
        }
        |> toListAsync
        |> Async.RunSynchronously

    /// <summary>
    ///     Format a value of total minutes as hours and minutes.
    /// </summary>
    /// <param name="minuteOfDay">Total minutes value to format.</param>
    /// <example>
    ///     Input: 86
    ///     Output: 1:26
    /// </example>
    /// <returns>Formatted string representing hours and minutes.</returns>
    let formatMinuteOfDay minuteOfDay =
        let mins = minuteOfDay % 60
        let hours = (minuteOfDay - mins) / 60

        $"{hours}:{mins}"

    /// <summary>
    ///     Print the best hours per day to buy Bitcoin based on provided price history statistics.
    /// </summary>
    /// <remarks>
    ///     "Best" in this case is when the prices is lowest on average>
    /// </remarks>
    /// <param name="stats">Price history statistics as price entrys grouped by day.</param>
    let printBestHoursPerDayStats (stats: IGrouping<DayOfWeek, PriceEntry>) =
        printfn $"{Environment.NewLine}Best hours to buy on a {stats.Key}:"

        stats
            .GroupBy(fun x -> x.Timestamp.Hour)
            .OrderBy(fun x -> x.Average(fun y -> y.Price))
            .Select(fun x ->
                $"{x.Key}-{Util.getNextHour x.Key} -> Avg. {x.Average(fun x -> x.Price)} (Min. {x.Min(fun x -> x.Price)} - Max {x.Max(fun x -> x.Price)})")
            .ToList()
            .ForEach(fun x -> printfn $"{x}")

    /// <summary>
    ///     Get formatted lines of statistical data representing average Bitcoin prices per hour base on provided price history
    /// </summary>
    /// <param name="prices">Price history data.</param>
    /// <returns>Collection of formatted strings as lines of average price data.</returns>
    let getPerHourStatLines (prices: PriceEntry list) =
        prices
            .GroupBy(fun x -> x.Timestamp.Hour)
            .ToList()
            .OrderBy(fun x -> x.Average(fun y -> y.Price))
            .Select(fun x ->
                $"{x.Key}-{Util.getNextHour x.Key} = Avg. {x.Average(fun y -> y.Price)} (Min. {x.Min(fun y -> y.Price)} - Max. {x.Max(fun y -> y.Price)})")

    /// <summary>
    ///     Get formatted lines of statistical data representing average Bitcoin prices per minute base on provided price history
    /// </summary>
    /// <param name="prices">Price history data.</param>
    /// <returns>Collection of formatted strings as lines of average price data.</returns>
    let getPerMinStatLines (prices: PriceEntry list) =
        prices
            .GroupBy(fun x -> (x.Timestamp.Hour * 60) + x.Timestamp.Minute)
            .ToList()
            .OrderBy(fun x -> x.Average(fun y -> y.Price))
            .Select(fun x ->
                $"{formatMinuteOfDay x.Key} = Avg. {x.Average(fun y -> y.Price)} (Min. {x.Min(fun y -> y.Price)} - Max. {x.Max(fun y -> y.Price)})")

    /// <summary>
    ///     Print Bitcoin price statistics in GBP based on data helf in a given database context.
    /// </summary>
    /// <param name="ctx">Database context to get prices from.</param>
    /// <param name="days">Number of days worth of historical data to retrieve.</param>
    let printStatsGbp ctx (days: int) =
        printfn $"Running GBP stats for the previous {days} days"

        let prices = getPrices ctx days

        // Print best times to buy per day
        prices
            .GroupBy(fun x -> x.Timestamp.DayOfWeek)
            .ToList()
            .ForEach(fun x -> printBestHoursPerDayStats x)

        // Print best general hour to buy across full week
        printfn $"{Environment.NewLine}Best hours of the day to buy across full week"

        (getPerHourStatLines prices)
            .ToList()
            .ForEach(fun x -> printfn $"{x}")

        // Print best general hour to buy across full week
        printfn $"{Environment.NewLine}Best minutes of the day to buy across full week"

        (getPerMinStatLines prices)
            .ToList()
            .ForEach(fun x -> printfn $"{x}")

    /// <summary>
    ///     Print Bitcoin price statistics in % based on data helf in a given database context.
    /// </summary>
    /// <param name="ctx">Database context to get prices from.</param>
    /// <param name="days">Number of days worth of historical data to retrieve.</param>
    let printStatsPercent ctx (days: int) =
        printfn $"Running stats for the previous {days} days..."
        printfn "Resulting %% stats are the difference between hourly average price and daily average price"
        printfn "A negative %% indicates lower than daily average (A good time to buy):\n"

        let prices = getPrices ctx days
        let hourlyAvgDict = List<KeyValuePair<int, decimal>>()

        // Loop through each day & get average price for entire day
        // Then rank each hour as +-% of the daily average and record in a dict
        for day in prices.GroupBy(fun x -> x.Timestamp.Date) do
            let dayAvg = day.Average(fun x -> x.Price)

            for hour in day.GroupBy(fun x -> x.Timestamp.Hour) do
                let hourAvg = hour.Average(fun x -> x.Price)

                // Work out this hours % performance vs the daily avg price and record it
                let hourDiff = hourAvg - dayAvg
                let hourPerc = 100.0M / (dayAvg / hourDiff)
                hourlyAvgDict.Add(KeyValuePair<int, decimal>(hour.Key, hourPerc))

        // Dict now looks like this [hour, avgPriceDiff%]: [1, 0.123], [1, -0.432], [1, 0.111], ... , [2, 0.432], [2, -0.234] ... etc
        // Group by the hour & order by average price for that hour (Lower = lower avg % diff vs daily average)
        hourlyAvgDict
            .GroupBy(fun x -> x.Key)
            .OrderBy(fun x -> x.Average(fun y -> y.Value))
            .Select(fun x ->
                $"Hour {x.Key}-{Util.getNextHour x.Key}\t->\t{x.Sum (fun y ->
                                                                  y.Value
                                                                  / Convert.ToDecimal(x.Select(fun y -> y.Value).Count())):N2}%%")
            .ToList()
            .ForEach(fun x -> printfn $"{x}")

    /// <summary>
    ///     Record the current Bitcoin price within a provided database context.
    /// </summary>
    /// <param name="ctx">Database context to record price within.</param>
    /// <example>
    ///     Example return value:
    ///     Recorded -> cecf10d6-9f8b-4cc8-a1b7-aa200b5c7617 - 07/05/2022 11:55:44 - 29240
    /// </example>
    /// <returns>Information message regarding newly recorded price.</returns>
    let record ctx =
        let testEnt =
            { Id = Guid.NewGuid()
              Timestamp = DateTime.UtcNow
              Price =
                CoinGecko
                    .Response
                    .Load(
                        CoinGecko.url
                    )
                    .MarketData
                    .CurrentPrice
                    .Gbp }

        testEnt |> addEntity ctx
        saveChanges ctx
        $"Recorded -> {testEnt.Id} - {testEnt.Timestamp} - {testEnt.Price}"
