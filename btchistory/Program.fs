open BtcContext
open Prices
open Util

/// <summary>
///     Application entry point.
/// </summary>
/// <remarks>
///     Runs various operation based on program arguments.
///     If zero or invalid program arguments are provided, a help message is printed.
///     Valid program arguments are:
///         record (Record the current Bitcoin price)
///         stats£ (Get Bitcoin price stats in GBP for the last 10 days)
///         stats£ N (Get Bitcoin price stats in GBP for the last N days)
///         stats% (Get Bitcoin price stats in % for the last 10 days)
///         stats% N (Get Bitcoin price stats in % for the last N days)
/// </remarks>
/// <returns>Exit Code.</returns>
[<EntryPoint>]
let main args =
    match args with
    | x when x.Length = 1 && x[0] = "record" -> printfn $"%s{Prices.record BtcContext.init}"
    | x when x.Length = 1 && x[0] = "stats£" -> Prices.printStatsGbp BtcContext.init 10
    | x when x.Length = 2 && x[0] = "stats£" -> Util.parseAsInt x[1] 10 |> Prices.printStatsGbp BtcContext.init
    | x when x.Length = 1 && x[0] = "stats%" -> Prices.printStatsPercent BtcContext.init 10
    | x when x.Length = 2 && x[0] = "stats%" -> Util.parseAsInt x[1] 10 |> Prices.printStatsPercent BtcContext.init
    | _ -> printfn "%s" "Invalid args: Valid args are 'record', 'stats£', 'stats£ N, 'stats%' 'stats% N'"

    0
