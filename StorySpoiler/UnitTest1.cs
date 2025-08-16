using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;


namespace Story
{
    [TestFixture]
    public class StoryTests
    {
        private RestClient client;
        private static string createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("rradev7904", "rradev7904");
            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            var response = loginClient.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreated()
        {
            var story = new
            {
                title = "Test Story",
                description = "This is a test story.",
                url = ""
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "The expected status code is 201 Created.");
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdStoryId = json.GetProperty("storyId").GetString() ?? string.Empty;
            Assert.That(createdStoryId, Is.Not.Null.And.Not.Empty, "The Story ID should not be null or empty.");
            Assert.That(response.Content, Does.Contain("Successfully created!"), "Response content should not be null.");
        }

        [Test, Order(2)]
        public void EditStoryTitleAndDescription_ShouldReturnOk()
        {
            var updatedStory = new
            {
                title = "Updated Test Story",
                description = "This is an updated test story.",
                url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(updatedStory);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "The expected status code is 200 OK.");
            Assert.That(response.Content, Does.Contain("Successfully edited"), "The expected message for the successfully edited story.");
        }

        [Test, Order(3)]
        public void GetAllStories_ShouldReturnOkAndAList()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "The expected status code is 200 OK.");
            var stories = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(stories, Is.Not.Empty, "The list of stories should not be null.");
        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "The expected status code is 200 OK.");
            Assert.That(response.Content, Does.Contain("Deleted successfully!"), "The expected message for the successfully deleted story.");
        }

        [Test, Order(5)]
        public void CreateStoryWithoutRequiredFields_ShouldReturnBadRequest()
        {
            var story = new
            {
                title = "",
                description = "",
                url = ""
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "The expected status code is 400 Bad Request.");
        }

        [Test, Order(6)]
        public void EditNonExistingStorySpoiler_ShouldReturnNotFound()
        {
            string nonExistingStoryId = "non-existing-id";
            var updatedStory = new
            {
                title = "Non-existing Story",
                description = "This story does not exist.",
                url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{nonExistingStoryId}", Method.Put);
            request.AddJsonBody(updatedStory);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "The expected status code is 404 Not Found.");
            Assert.That(response.Content, Does.Contain("No spoilers..."), "The expected message for the non-existing story.");
        }

        [Test, Order(7)]
        public void DeleteNonExistingStorySpoiler_ShouldReturnBadRequest()
        {
            string nonExistingStoryId = "non-existing-id";
            var request = new RestRequest($"/api/Story/Delete/{nonExistingStoryId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "The expected status code is 400 Bad Request.");
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"), "The expected message for the non-existing story.");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}