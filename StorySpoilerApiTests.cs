using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoilerTests.Models;

namespace StorySpoilerTests
{
    [TestFixture]
    public class StorySpoilerApiTests
    {
        private RestClient client;
        private static string lastCreatedStoryId;

        private readonly string BaseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? 
            "https://d3s5nxhwblsjbi.cloudfront.net";
        private readonly string LoginUserName = Environment.GetEnvironmentVariable("TEST_USER_NAME") ?? "DenisTestUser";
        private readonly string LoginPassword = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD") ?? "DenisTestUser123";

        private const string SuccessCreateMessage = "Successfully created!";
        private const string SuccessEditMessage = "Successfully edited";
        private const string SuccessDeleteMessage = "Deleted successfully!";
        private const string NoSuchStoryMessage = "No spoilers...";
        private const string UnableToDeleteMessage = "Unable to delete this story spoiler!";

        [OneTimeSetUp]
        public void Setup()
        {
            var jwtToken = GetJwtToken(LoginUserName, LoginPassword);
            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };
            this.client = new RestClient(options);
        }

        private string GetJwtToken(string userName, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { userName, password });
            var response = tempClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(
                    $"Failed to authenticate. Status: {response.StatusCode}, Content: {response.Content}, UserName: {userName}");
            }

            var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var token = content.GetProperty("accessToken").GetString();
            return token ?? throw new InvalidOperationException("Failed to retrieve JWT token.");
        }

        [Order(1)]
        [Test]
        public void CreateStory_WithRequiredFields_ShouldReturnCreated()
        {
            var storyRequest = new StoryDTO
            {
                Title = "Test Story",
                Description = "A thrilling test story spoiler.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code 201.");
            Assert.That(createResponse.StoryId, Is.Not.Null, "Response must contain a storyId.");
            Assert.That(createResponse.Msg, Is.EqualTo(SuccessCreateMessage), "Expected success message.");

            lastCreatedStoryId = createResponse.StoryId;
        }

        [Order(2)]
        [Test]
        public void EditStory_ShouldReturnSuccess()
        {
            Assert.That(lastCreatedStoryId, Is.Not.Null, "No story ID stored from previous test.");

            var editRequest = new StoryEditDTO
            {
                Title = "Updated Test Story",
                Description = "An updated thrilling story spoiler.",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{lastCreatedStoryId}", Method.Put);
            request.AddJsonBody(editRequest);
            var response = client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200.");
            Assert.That(editResponse.Msg, Is.EqualTo(SuccessEditMessage), "Expected success message.");
        }

        [Order(3)]
        [Test]
        public void GetAllStories_ShouldReturnNonEmptyList()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);
            var responseItems = JsonSerializer.Deserialize<List<StoryDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200.");
            Assert.That(responseItems, Is.Not.Null, "Response must not be null.");
            Assert.That(responseItems, Is.Not.Empty, "Response must contain at least one story.");
        }

        [Order(4)]
        [Test]
        public void DeleteStory_ShouldReturnSuccess()
        {
            Assert.That(lastCreatedStoryId, Is.Not.Null, "No story ID stored from previous test.");

            var request = new RestRequest($"/api/Story/Delete/{lastCreatedStoryId}", Method.Delete);
            var response = client.Execute(request);
            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200.");
            Assert.That(deleteResponse.Msg, Is.EqualTo(SuccessDeleteMessage), "Expected success message.");
        }

        [Order(5)]
        [Test]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var storyRequest = new StoryDTO
            {
                Title = "",
                Description = "",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400.");
        }

        [Order(6)]
        [Test]
        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            var nonExistingStoryId = Guid.NewGuid().ToString();
            var editRequest = new StoryEditDTO
            {
                Title = "Non-existing Story",
                Description = "Non-existing description",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{nonExistingStoryId}", Method.Put);
            request.AddJsonBody(editRequest);
            var response = client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404.");
            Assert.That(editResponse.Msg, Is.EqualTo(NoSuchStoryMessage), "Expected error message.");
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            var nonExistingStoryId = Guid.NewGuid().ToString();
            var request = new RestRequest($"/api/Story/Delete/{nonExistingStoryId}", Method.Delete);
            var response = client.Execute(request);
            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400.");
            Assert.That(deleteResponse.Msg, Is.EqualTo(UnableToDeleteMessage), "Expected error message.");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client?.Dispose();
        }
    }
}