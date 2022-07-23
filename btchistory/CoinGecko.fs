namespace CoinGecko

open FSharp.Data

/// <summary>
///     Module containing methods for interacting with the CoinGecko API.
/// </summary>
module CoinGecko =

    /// <summary>
    ///     URL to the CoinGecko API Bitcoin price endpoint.
    /// </summary>
    [<Literal>]
    let url =
        "https://api.coingecko.com/api/v3/coins/bitcoin?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false&"

    /// <summary>
    ///     Api Response model from the CoinGecko Bitcoin price endpoint.
    /// </summary>
    /// <seealso cref="url"/>
    type Response = JsonProvider<url>
