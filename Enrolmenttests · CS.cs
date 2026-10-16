
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
 
namespace RaceDay.Tests
{
    public class EnrolmentTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public EnrolmentTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Enrol_AsParticipant_Returns201_WithRaceNumber()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory);
            int categoryId = await TestData.CreateCategory(owner, await TestData.CreateEvent(owner));

            var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId });
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.False(string.IsNullOrEmpty(body.GetProperty("raceNumber").GetString()));
        }

        [Fact]
        public async Task Enrol_TwiceInTheSameEvent_Returns409()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory);
            int categoryId = await TestData.CreateCategory(owner, await TestData.CreateEvent(owner));
            await TestData.Enrol(participant, categoryId);

            var second = await participant.PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Enrol_AsOrganiser_Returns403()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            int categoryId = await TestData.CreateCategory(owner, await TestData.CreateEvent(owner));

            var response = await owner.PostAsJsonAsync("/api/enrolments", new { categoryID = categoryId });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_WhenNotLoggedIn_Returns401()
        {
            var anonymous = _factory.CreateApiClient();

            var response = await anonymous.PostAsJsonAsync("/api/enrolments", new { categoryID = 1 });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_WhenAgeIsOutsideTheCategoryRange_Returns400()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory, "2000-05-10");   // about 26
            int eventId = await TestData.CreateEvent(owner);
            int juniorCategoryId = await TestData.CreateCategory(owner, eventId, 16, 20);

            var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryID = juniorCategoryId });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_InUnknownCategory_Returns404()
        {
            var participant = await TestHelpers.LoggedInParticipant(_factory);

            var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryID = 999999 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetMyEnrolments_OnlyReturnsTheParticipantsOwnEntries()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participantA = await TestHelpers.LoggedInParticipant(_factory);
            var participantB = await TestHelpers.LoggedInParticipant(_factory);
            int categoryId = await TestData.CreateCategory(owner, await TestData.CreateEvent(owner));
            await TestData.Enrol(participantA, categoryId);

            var forA = await (await participantA.GetAsync("/api/enrolments/my")).Content.ReadFromJsonAsync<JsonElement>();
            var forB = await (await participantB.GetAsync("/api/enrolments/my")).Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(1, forA.GetArrayLength());
            Assert.Equal(0, forB.GetArrayLength());
        }

        [Fact]
        public async Task GetEventEnrolments_ByEventOwner_Returns200()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory);
            int eventId = await TestData.CreateEvent(owner);
            int categoryId = await TestData.CreateCategory(owner, eventId);
            await TestData.Enrol(participant, categoryId);

            var response = await owner.GetAsync($"/api/events/{eventId}/enrolments");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetArrayLength());
        }

        [Fact]
        public async Task GetEventEnrolments_AsParticipant_Returns403()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEventEnrolments_ByAnotherOrganiser_Returns403()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var otherOrganiser = await TestHelpers.LoggedInOrganiser(_factory);
            int eventId = await TestData.CreateEvent(owner);

            var response = await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}

