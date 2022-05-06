using System;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;

namespace btchistorycs
{
    public class ApiResponse
    {
        [JsonProperty("market_data")]
        public MarketData CurrentMarketData { get; set; }

        public class MarketData
        {
            [JsonProperty("current_price")]
            public Price CurrentPrice { get; set; }

            public class Price
            {
                [JsonProperty("gbp")]
                public double GBP { get; set; }
            }
        }
    }

    public class DataContext : DbContext
    {

        public DbSet<PriceEntry> PriceEntries { get; set; }

        public DataContext(DbContextOptions options) : base(options) { }

    }

    public record PriceEntry
    {
        public int ID { get; init; }
        public DateTime Timestamp { get; init; }

        public int MinuteOfDay {get;set;}

        public String Coin { get; init; }

        public String Currency { get; init; }

        public Double Price { get; init; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"-- START {DateTime.UtcNow}");

            var currentPrice = await GetCurrentPrice();

            var entry = new PriceEntry
            {
                Timestamp = DateTime.UtcNow,
                MinuteOfDay = (DateTime.UtcNow.Hour * 60) + DateTime.UtcNow.Minute,
                Coin = "BTC",
                Currency = "GBP",
                Price = currentPrice.CurrentMarketData.CurrentPrice.GBP
            };

            var conn = new SqliteConnection("DataSource=history.db");
            conn.Open(); 

            var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(conn)
            .Options;

            using var ctx = new DataContext(options);

            ctx.Database.EnsureCreated();

            ctx.Add(entry);
            ctx.SaveChanges();

            // All time

            var pricesPerMinuteAllTime = ctx.PriceEntries
                .ToList()
                .GroupBy(x => x.MinuteOfDay);

            Console.WriteLine($"[All time] Best 10 mins to buy bitcoin (UTC):");

            foreach (var price in pricesPerMinuteAllTime
                .OrderBy(x => x.Average(y => y.Price))
                .Take(10))
            {
                Console.WriteLine($"{MinuteOfDayToString(price.Key)} = Avg. {price.Average(x => x.Price)} (price {price.Min(x => x.Price)} - Max {price.Max(x => x.Price)})");
            }

            Console.WriteLine($"[All time] Worst 10 mins to buy bitcoin (UTC):");

            foreach (var price in pricesPerMinuteAllTime
                .OrderByDescending(x => x.Average(y => y.Price))
                .Take(10))
            {
                Console.WriteLine($"{MinuteOfDayToString(price.Key)} = Avg. {price.Average(x => x.Price)} (price {price.Min(x => x.Price)} - Max {price.Max(x => x.Price)})");
            }

            // Last Month

            var pricesPerMinutelastMonth = ctx.PriceEntries
                .Where(x => x.Timestamp > DateTime.UtcNow.AddMonths(-1))
                .ToList()
                .GroupBy(x => x.MinuteOfDay);

            Console.WriteLine($"[Last Month] Worst 10 mins to buy bitcoin (UTC):");

            foreach (var price in pricesPerMinutelastMonth
                .OrderByDescending(x => x.Average(y => y.Price))
                .Take(10))
            {
                Console.WriteLine($"{MinuteOfDayToString(price.Key)} = Avg. {price.Average(x => x.Price)} (price {price.Min(x => x.Price)} - Max {price.Max(x => x.Price)})");
            }

            Console.WriteLine($"[Last Month] Worst 10 mins to buy bitcoin (UTC):");

            foreach (var price in pricesPerMinutelastMonth
                .OrderBy(x => x.Average(y => y.Price))
                .Take(10))
            {
                Console.WriteLine($"{MinuteOfDayToString(price.Key)} = Avg. {price.Average(x => x.Price)} (price {price.Min(x => x.Price)} - Max {price.Max(x => x.Price)})");
            }            

            Console.WriteLine($"-- END {DateTime.UtcNow}");
        }

        static string MinuteOfDayToString(int minuteOfDay)
        {
            var mins = minuteOfDay % 60;
            var hour = (minuteOfDay - mins) / 60;

            return $"{hour}:{mins}";
        }

        static async Task<ApiResponse> GetCurrentPrice()
        {
            var client = new RestClient("https://api.coingecko.com/api/v3/coins/bitcoin");

            var request = new RestRequest()
                .AddQueryParameter("localization", "false")
                .AddQueryParameter("tickers", "false")
                .AddQueryParameter("market_data", "true")
                .AddQueryParameter("community_data", "false")
                .AddQueryParameter("developer_data", "false")
                .AddQueryParameter("sparkline", "false");

            var response = await client.GetAsync(request);
             
            return JsonConvert.DeserializeObject<ApiResponse>(response.Content);
        }
    }
}
