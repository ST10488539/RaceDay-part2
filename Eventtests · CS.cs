
using System.Net;
using System.Net.Http.Json;

using System.Net;
using System.Net.Http.Json;
 
namespace RaceDay.Tests
{
    public class EventTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public EventTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetEvents_WhenNotLoggedIn_Returns401()
        {
            var client = _factory.CreateApiClient();

            var response = await client.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetEvents_AsParticipant_Returns200()
        {
            var participant = await TestHelpers.LoggedInParticipant(_factory);

            var response = await participant.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetEvent_WithUnknownId_Returns404()
        {
            var organiser = await TestHelpers.LoggedInOrganiser(_factory);

            var response = await organiser.GetAsync("/api/events/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_Returns201()
        {
            var organiser = await TestHelpers.LoggedInOrganiser(_factory);

            var response = await organiser.PostAsJsonAsync("/api/events", TestData.EventBody());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_AsParticipant_Returns403()
        {
            var participant = await TestHelpers.LoggedInParticipant(_factory);

            var response = await participant.PostAsJsonAsync("/api/events", TestData.EventBody());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WhenNotLoggedIn_Returns401()
        {
            var client = _factory.CreateApiClient();

            var response = await client.PostAsJsonAsync("/api/events", TestData.EventBody());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithInvalidEventType_Returns400()
        {
            var organiser = await TestHelpers.LoggedInOrganiser(_factory);

            var response = await organiser.PostAsJsonAsync("/api/events", TestData.EventBody("SWIM"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_ByOwner_Returns200()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var response = await owner.PutAsJsonAsync($"/api/events/{eventId}", TestData.EventBody("WALK"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_ByDifferentOrganiser_Returns403()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var otherOrganiser = await TestHelpers.LoggedInOrganiser(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{eventId}", TestData.EventBody());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_ByOwner_Returns204_AndEventIsGone()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var delete = await owner.DeleteAsync($"/api/events/{eventId}");
            var get = await owner.GetAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_AsParticipant_Returns403()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var response = await participant.DeleteAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_WithEnrolments_Returns409()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory);
            int eventId = await TestData.CreateEvent(owner);
            int categoryId = await TestData.CreateCategory(owner, eventId);
            await TestData.Enrol(participant, categoryId);

            var response = await owner.DeleteAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }
}
