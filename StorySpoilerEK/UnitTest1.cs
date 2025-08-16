using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoilerEK.Models;


namespace StorySpoilerEK
{
    [TestFixture]
    public class StorySpoilerEKTests
    {
        private RestClient _client;
        private static string lastCreatedstoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("Ranaya", "123456#");

            var option = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            _client = new RestClient(option);
        }

        private string GetJwtToken(string userName, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { userName, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;

        }

        [Test, Order(1)]
        public void StorySpoilerTest_CreateNewStory_WithAllRequiredFields_ShouldReturnSuccessfullycreated()
        {
            //Arrange
            var story = new StoryDTO
            {
                Title = "Idea first in the list",
                Description = "Description of the first idea in the list",
                Url = ""

            };

            //Act
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = _client.Execute(request);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

            var storyBody = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(storyBody, Is.Not.Null);

            lastCreatedstoryId = storyBody.storyId;
            Assert.That(lastCreatedstoryId, Is.Not.Null);
            Assert.That(storyBody.Msg, Is.EqualTo("Successfully created!"));
        }

        [Test, Order(2)]

        public void StorySpoilerTest_EditStory_ShouldReturnSuccessfullycreated()
        {
            // Arrange
            string newName = "New Edited Story";
            string newDescription = "description of the first edited story";
            string expectedMessage = "Successfully edited";

            var request = new RestRequest($"/api/Story/Edit/{lastCreatedstoryId}");
            var Idea = new StoryDTO
            {
                Title = newName,
                Description = newDescription
            };

            request.AddJsonBody(Idea);

            // Act
            var response = _client.Execute(request, Method.Put);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code OK (200)");

                var storyBody = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

                Assert.That(storyBody, Is.Not.Null);
                Assert.That(storyBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
                Assert.That(response.Content, Does.Contain("Successfully edited"));
            });
        }

        [Test, Order(3)]

        public void StorySpoilerTest_GetAllStories_ShouldReturnAllStories()
        {
            //Arrange
            var request = new RestRequest("/api/Story/All");

            //Act
            var response = _client.Execute(request, Method.Get);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    $"Status code is {response.StatusCode}");

                var allStories = JsonSerializer.Deserialize<ApiResponseDTO[]>(response.Content);

                Assert.That(allStories, Is.Not.Null);

                Assert.That(allStories.Length, Is.GreaterThan(0), "Returned items are less than one");
            });
        }

        [Test, Order(4)]
        public void StorySpoilerTest_DeleteAStories_ShouldReturnDeletedSuccessfully()
        {
            // Arrange           
            string expectedMessage = "Deleted successfully!";
            var request = new RestRequest($"/api/Story/Delete/{lastCreatedstoryId}");

            // Act
            var response = _client.Execute(request, Method.Delete);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            var storyBody = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(storyBody, Is.Not.Null);
            Assert.That(storyBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
        }

        [Test, Order(5)]
        public void StorySpoilerTest_CreateAStoryWithOutRequiredFiels_ShouldReturnBadRequest()
        {
            //Arrange
            var request = new RestRequest("/api/Story/Create", Method.Post);
            var inncorrectIdea = new StoryDTO
            {
                Description = "incorrect"
            };

            //Act
            request.AddJsonBody(inncorrectIdea);
            var response = _client.Execute(request);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"Expected 400 BadRequest, got {response.StatusCode}.");
        }

        [Test, Order(6)]
        public void StorySpoilerTest_EditANonExistingSpoiler_ShouldReturnNotFound()
        {
            // Arrange
            string wrongStoryId = "16";
            string fakeDescription = "description of the fake story";
            string name = "not existing story";
            string expectedMessage = "No spoilers...";

            var request = new RestRequest($"/api/Story/Edit/{wrongStoryId}");
            var notExistingIdea = new StoryDTO
            {
                Title = name,
                Description = fakeDescription
            };

            request.AddJsonBody(notExistingIdea);

            // Act
            var response = _client.Execute(request, Method.Put);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));
        }

        [Test, Order(7)]
        public void StorySpoilerTest_DeeleteANonExistingSpoiler_ShouldReturnBadRequest()
        {
            //Arrange
            string wrongStoryId = "9";
            string expectedMessage = "Unable to delete this story spoiler!";

            var request = new RestRequest($"/api/Story/Delete/{wrongStoryId}");

            // Act
            var response = _client.Execute(request, Method.Delete);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                    $"Status code is {response.StatusCode}");
                Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

                var notExistingStoryBody = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

                Assert.That(notExistingStoryBody, Is.Not.Null);
                Assert.That(notExistingStoryBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
            });
        }

        [OneTimeTearDown]

        public void Cleanup()
        {
            _client?.Dispose();
        }
    }
}