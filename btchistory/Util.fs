namespace Util

open System

/// <summary>
///     Module containing useful utility methods.
/// </summary>
module Util =

    /// <summary>
    ///     Get the next occuring hour based on a given hour.
    /// </summary>
    /// <remarks>
    ///     In most cases this is simply "hour+1".
    ///     The exception being 11pm, which rolls back over to hour 0.
    /// </remarks>
    /// <param name="hour">Hour to find the next hour for.</param>
    /// <returns>Hour as an integer between 0 and 23.</returns>
    let getNextHour hour =
        match hour with
        | 23 -> 0
        | _ -> hour + 1

    /// <summary>
    ///     Safely parse a given string as an 32-bit integer.
    /// </summary>
    /// <remarks>
    ///     If parsing fails due to invalid input format, the fallback value is returned.
    /// </remarks>
    /// <param name="input">String value to parse as an integer.</param>
    /// <param name="fallback">Fallback value to return in cases where parsing fails.</param>
    /// <returns>Input string as an integer when input is valid, otherwise the fallback value.</returns>
    let parseAsInt (input: string) fallback : int =
        let mutable res = fallback

        if Int32.TryParse(input, &res) then
            res
        else
            fallback
