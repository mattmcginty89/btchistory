# btchistory
Record Bitcoin price history and anaylse average prices per day/hour/minute

This provides an almost certainly useless data set to aid with the timing of DCA orders

Collecting this data helps fool yourself into answering questions like:
  - When DCA'ing every day - Is 5pm better than 4am?
  - When DCA'ing every week - Is Monday better than Thursday?

In reality, this is clearly utterly useless data, as the market is ever changing and unpredicatable

This was written purely as a hobby project to learn F#

Prices are obtained from the CoinGecko API

Price history is held persistently in an SqLite database file on disk

All dates/times are in UTC

**Usage:**

Record the current BTCGBP price in the database (Cron task per minute/hour/day is recommended):

`dotnet run record`

View statistics about the previous 10 days of data held in the database:

`dotnet run stats£`

or

`dotnet run stats%`

View statistics about the previous 365 days of data held in the database:

`dotnet run stats£ 365`

or 

`dotnet run stats% 365`

**Example output**

```
$ dotnet run record

Recorded -> cecf10d6-9f8b-4cc8-a1b7-aa200b5c7617 - 07/05/2022 11:55:44 - 29240
```

```
$ dotnet run stats£

Running stats for the previous 10 days

Best hours to buy on a Saturday:
12-13 -> Avg. 29233.4 (Min. 29223 - Max 29238)
... [Other hours/days]

Best hours of the day to buy across full week
12-13 = Avg. 29233.4 (Min. 29223 - Max. 29238)
... [Other hours]

Best minutes of the day to buy across full week
12:25 = Avg. 29223 (Min. 29223 - Max. 29223)
12:15 = Avg. 29232 (Min. 29232 - Max. 29232)
... [Other minutes]

```

```
$ dotnet run stats% 30

Running stats for the previous 30 days...
Resulting % stats are the difference between hourly average price and daily average price
A negative % indicates lower than daily average (A good time to buy):

Hour 22-23      ->      -3.05%
Hour 23-0       ->      -2.82%
Hour 21-22      ->      -2.11%
```

**Todo**

- Tests
- Replace .NET/C# style linq `GroupBy` with F# list `groupBy`
- Refactor to be more functional and pure
- Learn F#...
