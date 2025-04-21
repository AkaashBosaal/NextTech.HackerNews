using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using System.Text;
using NextTech.HackerNews.Infrastructure;

namespace HackerNewsApi.Infrastructure.Tests
{
    [TestFixture]
    public class HttpClientWrapperTests
    {
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private HttpClientWrapper _httpClientWrapper;

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _httpClientWrapper = new HttpClientWrapper(_httpClient);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }

        [Test]
        public async Task GetAsync_ReturnsDeserializedObject_WhenResponseIsSuccessful()
        {
            // Arrange
            var testData = new { Id = 1, Name = "Test" };
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(testData),
                Encoding.UTF8,
                "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _httpClientWrapper.GetAsync<dynamic>("https://hacker-news.firebaseio.com/v0/");

            // Assert
            Assert.Equals(testData.Id, (int)result.Id);
            Assert.Equals(testData.Name, (string)result.Name);
        }

        [Test]
        public async Task GetAsync_ThrowsHttpRequestException_WhenResponseIsNotSuccessful()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(() =>
                _httpClientWrapper.GetAsync<object>("https://hacker-news.firebaseio.com/v0/"));
        }

        [Test]
        public async Task GetAsync_ThrowsJsonException_WhenResponseIsInvalidJson()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("invalid json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act & Assert
            Assert.ThrowsAsync<JsonException>(() =>
                _httpClientWrapper.GetAsync<object>("https://hacker-news.firebaseio.com/v0/"));
        }

        [Test]
        public async Task GetAsync_ThrowsException_WhenRequestFails()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(() =>
                _httpClientWrapper.GetAsync<object>("https://hacker-news.firebaseio.com/v0/"));
        }
    }
}