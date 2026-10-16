
using System.Net.Http.Json;
using System.Text.Json;


using System.Net.Http.Json;
using System.Text.Json;
 
namespace RaceDay.Tests
{
    public static class TestData
    {
        // Always in the future, so enrolment tests keep passing as time goes by
        public static string FutureDate() =>
            DateTime.UtcNow.AddDays(60).ToString("yyyy-MM-dd");

        public static object EventBody(string eventType = "RUN") => new
        {
            eventName = "Test City Run 10K",
            eventDate = FutureDate(),
            location = "Johannesburg, Gauteng",
            description = "A test event.",
            distance = 10,
            eventType
        };

        public static object ResultBody(int registrationId, string status = "OFFICIAL") => new
        {
            registrationID = registrationId,
            finishTime = "00:52:34",
            chipTime = "00:52:20",
            overallPosition = 18,
            categoryPosition = 12,
            resultStatus = status
        };

        public static async Task<int> CreateEvent(HttpClient organiser)
        {
            var response = await organiser.PostAsJsonAsync("/api/events", EventBody());
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("eventID").GetInt32();
        }

        public static async Task<int> CreateCategory(
            HttpClient organiser, int eventId, int minAge = 18, int? maxAge = null)
        {
            var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
            {
                categoryName = "10K Open Run",
                distance = 10,
                minAge,
                maxAge
            });
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("categoryID").GetInt32();
        }

        public static async Task<int> Enrol(HttpClient participant, int categoryId)
        {
            var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId });
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("registrationID").GetInt32();
        }
    }
}

