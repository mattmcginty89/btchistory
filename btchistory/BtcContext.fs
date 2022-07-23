namespace BtcContext

open System
open Microsoft.Data.Sqlite
open Microsoft.EntityFrameworkCore
open System.ComponentModel.DataAnnotations

/// <summary>
///     Model representing a single row in the PriceEntry database table.
/// </summary>
[<CLIMutable>]
type PriceEntry =
    { [<Key>]
      Id: Guid
      Timestamp: DateTime
      Price: decimal }

/// <summary>
///     EFCore Database context.
/// </summary>
/// <remarks>
///     Contains a single table, PriceEntry, for recording Bitcoin price history.
/// </remarks>
type BtcContext(options: DbContextOptions<BtcContext>) =
    inherit DbContext(options: DbContextOptions<BtcContext>)

    [<DefaultValue>]
    val mutable private _priceEntries: DbSet<PriceEntry>

    member this.PriceEntries
        with get () = this._priceEntries
        and set v = this._priceEntries <- v

    override __.OnModelCreating(modelBuilder) =
        modelBuilder
            .Entity<PriceEntry>()
            .HasIndex((fun b -> b.Timestamp :> Object))
        |> ignore

/// <summary>
///     Module containing methods for creating Bitcoin database contexts.
/// </summary>
module BtcContext =

    /// <summary>
    ///     Initialise a new EFCore database context.
    /// </summary>
    /// <seealso cref="BtcContext"/>
    /// <returns>BtcContext EFCore database context.</returns>
    let init =
        let conn = new SqliteConnection("DataSource=history.db")
        conn.Open()

        let ctx =
            new BtcContext(
                DbContextOptionsBuilder<BtcContext>().UseSqlite(
                    conn
                )
                    .Options
            )

        ctx.Database.EnsureCreated() |> ignore
        ctx
