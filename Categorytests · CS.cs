
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
 
namespace RaceDay.Tests
{
    public class CategoryTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public CategoryTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        private static object CategoryBody(int minAge = 18, int? maxAge = null) => new
        {
            categoryName = "10K Open Run",
            distance = 10,
            minAge,
            maxAge
        };

        [Fact]
        public async Task CreateCategory_ByEventOwner_Returns201()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var response = await owner.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_AsParticipant_Returns403()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_WhenNotLoggedIn_Returns401()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            int eventId = await TestData.CreateEvent(owner);
            var anonymous = _factory.CreateApiClient();

            var response = await anonymous.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_ForAnotherOrganisersEvent_Returns403()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var otherOrganiser = await TestHelpers.LoggedInOrganiser(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var response = await otherOrganiser.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_WithMaxAgeBelowMinAge_Returns400()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var response = await owner.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody(30, 20));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_ForUnknownEvent_Returns404()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);

            var response = await owner.PostAsJsonAsync("/api/events/999999/categories", CategoryBody());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCategories_AsParticipant_Returns200_WithTheCategories()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory);
            int eventId = await TestData.CreateEvent(owner);
            await TestData.CreateCategory(owner, eventId);

            var response = await participant.GetAsync($"/api/events/{eventId}/categories");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetArrayLength());
        }
    }
}

