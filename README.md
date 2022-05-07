# btchistory
Record minute by minute Bitcoin price history in GBP

Prices are obtained from the CoinGecko API

Price history is held persistently in an SqLite database file on disk

This provides (almost definitely not) useful data, to aid with timing DCA strategies

This was written purely as a hobby project to learn F#

**Usage:**

Record the current BTCGBP price in the database (Cron task per minute/hour/day is recommended):

`dotnet run record`

View statistics about the previous 10 days of data held in the database:

`dotnet run stats`

View statistics about the previous 365 days of data held in the database:

`dotnet run stats 365`

**Example output**

```
$ dotnet run record

Recorded -> cecf10d6-9f8b-4cc8-a1b7-aa200b5c7617 - 07/05/2022 11:55:44 - 29240
```

```
$ dotnet run stats

Running stats for the previous 10 days

Best hours to buy on a Saturday:
11-12 -> Avg. 29240 (Min. 29240 - Max 29240)

Best hours of the day to buy across full week
11-12 = Avg. 29240 (Min. 29240 - Max. 29240)

Best minutes of the day to buy across full week
11:55 = Avg. 29240 (Min. 29240 - Max. 29240)
```

**Todo**

- Tests
- Replace .NET/C# style linq `GroupBy` with F# list `groupBy`
- Refactor to be more functional and pure
- Learn F#...