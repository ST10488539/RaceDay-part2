
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
 
namespace RaceDay.Tests
{
    public class ResultTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public ResultTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        // An organiser, a participant, and the participant's enrolment in the organiser's event
        private async Task<(HttpClient owner, HttpClient participant, int eventId, int registrationId)> SetUp()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);
            var participant = await TestHelpers.LoggedInParticipant(_factory);
            int eventId = await TestData.CreateEvent(owner);
            int categoryId = await TestData.CreateCategory(owner, eventId);
            int registrationId = await TestData.Enrol(participant, categoryId);
            return (owner, participant, eventId, registrationId);
        }

        [Fact]
        public async Task CreateResult_ByEventOwner_Returns201_AndCalculatesPace()
        {
            var (owner, _, _, registrationId) = await SetUp();

            var response = await owner.PostAsJsonAsync("/api/results", TestData.ResultBody(registrationId));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(5.23m, body.GetProperty("pace").GetDecimal());
        }

        [Fact]
        public async Task CreateResult_AsParticipant_Returns403()
        {
            var (_, participant, _, registrationId) = await SetUp();

            var response = await participant.PostAsJsonAsync("/api/results", TestData.ResultBody(registrationId));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateResult_WhenNotLoggedIn_Returns401()
        {
            var anonymous = _factory.CreateApiClient();

            var response = await anonymous.PostAsJsonAsync("/api/results", TestData.ResultBody(1));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateResult_ByAnotherOrganiser_Returns403()
        {
            var (_, _, _, registrationId) = await SetUp();
            var otherOrganiser = await TestHelpers.LoggedInOrganiser(_factory);

            var response = await otherOrganiser.PostAsJsonAsync("/api/results", TestData.ResultBody(registrationId));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateResult_ForUnknownEnrolment_Returns404()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);

            var response = await owner.PostAsJsonAsync("/api/results", TestData.ResultBody(999999));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateResult_WithInvalidStatus_Returns400()
        {
            var (owner, _, _, registrationId) = await SetUp();

            var response = await owner.PostAsJsonAsync("/api/results", TestData.ResultBody(registrationId, "string"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateResult_Twice_Returns409()
        {
            var (owner, _, _, registrationId) = await SetUp();
            await owner.PostAsJsonAsync("/api/results", TestData.ResultBody(registrationId));

            var second = await owner.PostAsJsonAsync("/api/results", TestData.ResultBody(registrationId));

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task GetMyResults_ReturnsTheParticipantsOwnResult()
        {
            var (owner, participant, _, registrationId) = await SetUp();
            await owner.PostAsJsonAsync("/api/results", TestData.ResultBody(registrationId));

            var response = await participant.GetAsync("/api/results/my");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetArrayLength());
        }

        [Fact]
        public async Task GetMyResults_AnotherParticipantSeesNone()
        {
            var (owner, _, _, registrationId) = await SetUp();
            await owner.PostAsJsonAsync("/api/results", TestData.ResultBody(registrationId));
            var otherParticipant = await TestHelpers.LoggedInParticipant(_factory);

            var response = await otherParticipant.GetAsync("/api/results/my");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(0, body.GetArrayLength());
        }

        [Fact]
        public async Task GetMyResults_AsOrganiser_Returns403()
        {
            var owner = await TestHelpers.LoggedInOrganiser(_factory);

            var response = await owner.GetAsync("/api/results/my");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetMyResults_WhenNotLoggedIn_Returns401()
        {
            var anonymous = _factory.CreateApiClient();

            var response = await anonymous.GetAsync("/api/results/my");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetEventResults_AsParticipant_Returns403()
        {
            var (_, participant, eventId, _) = await SetUp();

            var response = await participant.GetAsync($"/api/events/{eventId}/results");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEventResults_ByEventOwner_Returns200()
        {
            var (owner, _, eventId, registrationId) = await SetUp();
            await owner.PostAsJsonAsync("/api/results", TestData.ResultBody(registrationId));

            var response = await owner.GetAsync($"/api/events/{eventId}/results");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, body.GetArrayLength());
        }
    }
}

