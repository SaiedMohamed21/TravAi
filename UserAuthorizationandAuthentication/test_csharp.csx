
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var body = @"{
            ""fromCity"": ""FRA"",
            ""departureDate"": ""2026-06-25T00:00:00.000Z"",
            ""returnDate"": ""2026-06-28T00:00:00.000Z"",
            ""adults"": 1,
            ""children"": 0,
            ""singleRooms"": 0,
            ""doubleRooms"": 1,
            ""excludeFlights"": false,
            ""excludeHotels"": false,
            ""excludeTours"": false,
            ""touristLanguage"": ""English"",
            ""maxBudget"": 1852,
            ""budgetType"": ""Premium"",
            ""itinerary"": [
                { ""city"": ""CAI"", ""days"": 3 }
            ]
        }";
        using var client = new HttpClient();
        var content = new StringContent(body, Encoding.UTF8, ""application/json"");
        var res = await client.PostAsync(""http://localhost:5210/api/ai/generate-plan"", content);
        Console.WriteLine(res.StatusCode);
        Console.WriteLine(await res.Content.ReadAsStringAsync());
    }
}
Program.Main().Wait();

