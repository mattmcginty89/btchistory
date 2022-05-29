﻿open System.Collections.Generic
open EntityFrameworkCore.FSharp.DbContextHelpers
open FSharp.Data
open Microsoft.Data.Sqlite
open Microsoft.EntityFrameworkCore
open System
open System.ComponentModel.DataAnnotations
open System.Linq

[<Literal>]
let apiUrl = "https://api.coingecko.com/api/v3/coins/bitcoin?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false&"
let cr = Environment.NewLine

type ApiResponse = JsonProvider<apiUrl>

[<CLIMutable>]
type PriceEntry = {
    [<Key>]
    Id : Guid
    Timestamp : DateTime
    Price : double
}

type BtcContext (options: DbContextOptions<BtcContext>) =
    inherit DbContext(options: DbContextOptions<BtcContext>)

    [<DefaultValue>]
    val mutable private _priceEntries : DbSet<PriceEntry>
    
    member this.PriceEntries with get() = this._priceEntries and set v = this._priceEntries <- v
    
    override __.OnModelCreating(modelBuilder) =
        modelBuilder.Entity<PriceEntry>().HasIndex( (fun b -> b.Timestamp :> Object ) ) |> ignore

let initCtx =
    let conn = new SqliteConnection("DataSource=history.db")
    conn.Open()
    
    let ctx = new BtcContext(DbContextOptionsBuilder<BtcContext>().UseSqlite(conn).Options)
    ctx.Database.EnsureCreated() |> ignore    
    ctx

let recordPrice ctx = 
    let testEnt  = {
        Id = Guid.NewGuid()
        Timestamp = DateTime.UtcNow
        Price = ApiResponse.Load(apiUrl).MarketData.CurrentPrice.Gbp
    }

    testEnt |> addEntity ctx
    saveChanges ctx
    $"Recorded -> {testEnt.Id} - {testEnt.Timestamp} - {testEnt.Price}"
    
let getPrices (ctx: BtcContext) days =
    query {
        for p in ctx.PriceEntries do
        where (p.Timestamp > DateTime.UtcNow.AddDays(-days))
        select p
    }
    |> toListAsync
    |> Async.RunSynchronously
    
let getNextHour hour =
    match hour with
    | 23 -> 0
    | _ -> hour + 1
    
let formatMinuteOfDay minuteOfDay =
    let mins = minuteOfDay % 60
    let hours = (minuteOfDay - mins) / 60
    
    $"{hours}:{mins}"
    
let printBestHoursPerDayStats (stats: IGrouping<DayOfWeek,PriceEntry>) =
    printfn $"{cr}Best hours to buy on a {stats.Key}:"

    stats.GroupBy(fun x -> x.Timestamp.Hour)
        .OrderBy(fun x -> x.Average(fun y -> y.Price))
        .Select(fun x -> $"{x.Key}-{getNextHour x.Key} -> Avg. {x.Average(fun x -> x.Price)} (Min. {x.Min(fun x -> x.Price)} - Max {x.Max(fun x -> x.Price)})")
        .ToList()
        .ForEach(fun x -> printfn $"{x}")
    
let getPerHourStatLines (prices: PriceEntry list) =
    prices.GroupBy(fun x -> x.Timestamp.Hour).ToList()
        .OrderBy(fun x -> x.Average(fun y -> y.Price))
        .Select(fun x -> $"{x.Key}-{getNextHour x.Key} = Avg. {x.Average(fun y -> y.Price)} (Min. {x.Min(fun y -> y.Price)} - Max. {x.Max(fun y -> y.Price)})")
        
let getPerMinStatLines (prices: PriceEntry list) =
    prices.GroupBy(fun x -> (x.Timestamp.Hour * 60) + x.Timestamp.Minute).ToList()
        .OrderBy(fun x -> x.Average(fun y -> y.Price))
        .Select(fun x -> $"{formatMinuteOfDay x.Key} = Avg. {x.Average(fun y -> y.Price)} (Min. {x.Min(fun y -> y.Price)} - Max. {x.Max(fun y -> y.Price)})")
        
let printStatsGbp ctx (days: int) =
    printfn $"Running GBP stats for the previous {days} days"
    
    let prices = getPrices ctx days
    
    // Print best times to buy per day    
    prices.GroupBy(fun x -> x.Timestamp.DayOfWeek).ToList().ForEach(fun x -> printBestHoursPerDayStats x)
    
    // Print best general hour to buy across full week
    printfn $"{cr}Best hours of the day to buy across full week"        
    (getPerHourStatLines prices).ToList().ForEach(fun x -> printfn $"{x}")
        
    // Print best general hour to buy across full week
    printfn $"{cr}Best minutes of the day to buy across full week"
    (getPerMinStatLines prices).ToList().ForEach(fun x -> printfn $"{x}")

let printStatsPercent ctx (days: int) =
    printfn $"Running stats for the previous {days} days..."
    printfn "Resulting %% stats are the difference between hourly average price and daily average price"
    printfn "A negative %% indicates lower than daily average (A good time to buy):\n"
    
    let prices = getPrices ctx days
    let hourlyAvgDict = List<KeyValuePair<int, double>>()

    // Loop through each day & get average price for entire day
    // Then rank each hour as +-% of the daily average and record in a dict
    for day in prices.GroupBy(fun x -> x.Timestamp.Date) do
        let dayAvg = day.Average(fun x -> x.Price)
        
        for hour in day.GroupBy(fun x -> x.Timestamp.Hour) do
            let hourAvg = hour.Average(fun x -> x.Price)
            
            // Work out this hours % performance vs the daily avg price and record it
            let hourDiff = hourAvg - dayAvg            
            let hourPerc = 100.0 / (dayAvg / hourDiff)
            hourlyAvgDict.Add(KeyValuePair<int, double>(hour.Key, hourPerc))
        
    // Dict now looks like this [hour, avgPriceDiff%]: [1, 0.123], [1, -0.432], [1, 0.111], ... , [2, 0.432], [2, -0.234] ... etc
    // Group by the hour & order by average price for that hour (Lower = lower avg % diff vs daily average)
    hourlyAvgDict.GroupBy(fun x -> x.Key)
       .OrderBy(fun x -> x.Average(fun y -> y.Value))
       .Select(fun x -> $"Hour {x.Key}-{getNextHour x.Key}\t->\t{x.Sum(fun y -> y.Value / Convert.ToDouble(x.Select(fun y -> y.Value).Count())):N2}%%")
       .ToList().ForEach(fun x -> printfn $"{x}")
    
let parseArgAsInt (arg: string) fallback : int =
    let mutable res = fallback
    
    if Int32.TryParse(arg, &res) then
        res
    else
        fallback
    
[<EntryPoint>]
let main args =
    match args with
    | x when x.Length = 1 && x[0] = "record" -> printfn $"%s{recordPrice initCtx}"
    | x when x.Length = 1 && x[0] = "stats£" -> printStatsGbp initCtx 10
    | x when x.Length = 2 && x[0] = "stats£" -> parseArgAsInt x[1] 10 |> printStatsGbp initCtx
    | x when x.Length = 1 && x[0] = "stats%" -> printStatsPercent initCtx 10
    | x when x.Length = 2 && x[0] = "stats%" -> parseArgAsInt x[1] 10 |> printStatsPercent initCtx
    | _ -> printfn "%s" "Invalid args provided: Valid args are 'record', 'stats', 'stats N'"
    0