
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Data;
 
namespace RaceDay.Tests
{
    public class AuthTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public AuthTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        // ---------- Register ----------

        [Fact]
        public async Task Register_Organiser_WithValidData_Returns201()
        {
            var client = _factory.CreateApiClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Olivia",
                lastName = "Organiser",
                email = TestHelpers.NewEmail("org"),
                password = TestHelpers.Password,
                role = "Organiser"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_Participant_WithValidData_Returns201()
        {
            var client = _factory.CreateApiClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Parker",
                lastName = "Participant",
                email = TestHelpers.NewEmail("part"),
                password = TestHelpers.Password,
                role = "Participant",
                dateOfBirth = "2000-05-10",
                gender = "FEMALE"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_ParticipantWithoutDateOfBirth_Returns400()
        {
            var client = _factory.CreateApiClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Parker",
                lastName = "Participant",
                email = TestHelpers.NewEmail("part"),
                password = TestHelpers.Password,
                role = "Participant",
                gender = "MALE"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_InvalidRole_Returns400()
        {
            var client = _factory.CreateApiClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Test",
                lastName = "User",
                email = TestHelpers.NewEmail("bad"),
                password = TestHelpers.Password,
                role = "Admin"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_ShortPassword_Returns400()
        {
            var client = _factory.CreateApiClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Test",
                lastName = "User",
                email = TestHelpers.NewEmail("short"),
                password = "abc",
                role = "Organiser"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns409()
        {
            var client = _factory.CreateApiClient();
            string email = TestHelpers.NewEmail("dup");

            var body = new
            {
                firstName = "Test",
                lastName = "User",
                email,
                password = TestHelpers.Password,
                role = "Organiser"
            };

            var first = await client.PostAsJsonAsync("/api/auth/register", body);
            var second = await client.PostAsJsonAsync("/api/auth/register", body);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Register_StoresPasswordHashed_NotPlainText()
        {
            var client = _factory.CreateApiClient();
            string email = await TestHelpers.RegisterOrganiser(client);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            var user = db.Users.Single(u => u.Email == email);

            Assert.NotEqual(TestHelpers.Password, user.PasswordHash);
            Assert.DoesNotContain(TestHelpers.Password, user.PasswordHash);
        }

        // ---------- Login ----------

        [Fact]
        public async Task Login_WithCorrectCredentials_Returns200()
        {
            var client = _factory.CreateApiClient();
            string email = await TestHelpers.RegisterOrganiser(client);

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email,
                password = TestHelpers.Password
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            var client = _factory.CreateApiClient();
            string email = await TestHelpers.RegisterOrganiser(client);

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email,
                password = "WrongPassword999"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_Returns401()
        {
            var client = _factory.CreateApiClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = TestHelpers.NewEmail("nobody"),
                password = TestHelpers.Password
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ---------- Sessions ----------

        [Fact]
        public async Task Me_WhenNotLoggedIn_Returns401()
        {
            var client = _factory.CreateApiClient();

            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Me_WhenLoggedIn_ReturnsTheUsersRole()
        {
            var client = await TestHelpers.LoggedInParticipant(_factory);

            var response = await client.GetAsync("/api/auth/me");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Participant", body.GetProperty("role").GetString());
        }

        [Fact]
        public async Task Logout_EndsTheSession()
        {
            var client = await TestHelpers.LoggedInOrganiser(_factory);

            var logout = await client.PostAsync("/api/auth/logout", null);
            var afterLogout = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
        }

        [Fact]
        public async Task Logout_WhenNotLoggedIn_Returns401()
        {
            var client = _factory.CreateApiClient();

            var response = await client.PostAsync("/api/auth/logout", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}

