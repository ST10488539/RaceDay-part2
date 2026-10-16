
using System.Net.Http.Json;


using System.Net.Http.Json;
 
namespace RaceDay.Tests
{
    public static class TestHelpers
    {
        public const string Password = "Password123";

        public static string NewEmail(string prefix) =>
            $"{prefix}.{Guid.NewGuid():N}@example.com";

        public static async Task<string> RegisterOrganiser(HttpClient client)
        {
            string email = NewEmail("organiser");

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Olivia",
                lastName = "Organiser",
                email,
                password = Password,
                role = "Organiser"
            });
            response.EnsureSuccessStatusCode();

            return email;
        }

        public static async Task<string> RegisterParticipant(HttpClient client, string dateOfBirth = "2000-05-10")
        {
            string email = NewEmail("participant");

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Parker",
                lastName = "Participant",
                email,
                password = Password,
                role = "Participant",
                dateOfBirth,
                gender = "MALE"
            });
            response.EnsureSuccessStatusCode();

            return email;
        }

        public static async Task Login(HttpClient client, string email)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email,
                password = Password
            });
            response.EnsureSuccessStatusCode();
        }

        /// <summary>Creates a new organiser account and returns a client that is logged in as them.</summary>
        public static async Task<HttpClient> LoggedInOrganiser(RaceDayFactory factory)
        {
            var client = factory.CreateApiClient();
            string email = await RegisterOrganiser(client);
            await Login(client, email);
            return client;
        }

        /// <summary>Creates a new participant account and returns a client that is logged in as them.</summary>
        public static async Task<HttpClient> LoggedInParticipant(RaceDayFactory factory, string dateOfBirth = "2000-05-10")
        {
            var client = factory.CreateApiClient();
            string email = await RegisterParticipant(client, dateOfBirth);
            await Login(client, email);
            return client;
        }
    }
}

