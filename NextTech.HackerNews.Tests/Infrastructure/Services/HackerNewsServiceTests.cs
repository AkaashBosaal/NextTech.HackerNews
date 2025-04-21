using Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NextTech.HackerNews.Core.Entities;
using NextTech.HackerNews.Core.Interfaces.Infrastructure;
using NextTech.HackerNews.Infrastructure.Services;
using Microsoft.Extensions.Options;
using NextTech.HackerNews.Core.Configurations;

namespace HackerNewsApi.Infrastructure.Tests
{
    [TestFixture]
    public class HackerNewsServiceTests
    {
        private Mock<IHttpClientWrapper> _mockHttpClient;
        private Mock<IMemoryCache> _mockMemoryCache;
        private Mock<ILogger<HackerNewsService>> _mockLogger;
        private HackerNewsService _hackerNewsService;
        private MemoryCache _realCache;
        private List<Story> _testStories;
        private Mock<IOptions<HackerNewsOptions>> _mockOptions;

        [SetUp]
        public void Setup()
        {
            _mockHttpClient = new Mock<IHttpClientWrapper>();
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockLogger = new Mock<ILogger<HackerNewsService>>();
            _mockOptions = new Mock<IOptions<HackerNewsOptions>>();
            _realCache = new MemoryCache(new MemoryCacheOptions());
            _testStories =
            [
                new () { Id = 1, Title = "Test Story 1", Url = "http://test1.com" },
                new () { Id = 2, Title = "Test Story 2", Url = "http://test2.com" }
            ];

            _mockOptions.Setup(o => o.Value).Returns(new HackerNewsOptions());

            _hackerNewsService = new HackerNewsService(
                _mockHttpClient.Object,
                _mockMemoryCache.Object,
                _mockLogger.Object,
                _mockOptions.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _realCache?.Dispose();
        }

        [Test]
        public async Task GetNewestStoriesAsync_ReturnsStories_WhenNotCached()
        {
            // Arrange
            var storyIds = new[] { 1, 2 };
            object cachedStories = null;

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedStories))
                .Returns(false);

            _mockHttpClient.Setup(x => x.GetAsync<int[]>(It.IsAny<string>()))
                .ReturnsAsync(storyIds);

            _mockHttpClient.Setup(x => x.GetAsync<Story>(It.Is<string>(s => s.Contains("item/1.json"))))
                .ReturnsAsync(_testStories[0]);

            _mockHttpClient.Setup(x => x.GetAsync<Story>(It.Is<string>(s => s.Contains("item/2.json"))))
                .ReturnsAsync(_testStories[1]);

            var cacheEntryMock = new Mock<ICacheEntry>();
            _mockMemoryCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            // Act
            var result = await _hackerNewsService.GetNewestStoriesAsync(2);

            // Assert
            Assert.Equals(2, result.Count());
            _mockMemoryCache.Verify(x => x.TryGetValue(It.IsAny<string>(), out cachedStories), Times.Once);
            _mockHttpClient.Verify(x => x.GetAsync<int[]>(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GetNewestStoriesAsync_ReturnsCachedStories_WhenAvailable()
        {
            // Arrange
            object cachedStories = _testStories;

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedStories))
                .Returns(true);

            // Act
            var result = await _hackerNewsService.GetNewestStoriesAsync(2);

            // Assert
            Assert.Equals(2, result.Count());
            _mockMemoryCache.Verify(x => x.TryGetValue(It.IsAny<string>(), out cachedStories), Times.Once);
            _mockHttpClient.Verify(x => x.GetAsync<int[]>(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetNewestStoriesAsync_ReturnsLimitedCount_WhenRequested()
        {
            // Arrange
            object cachedStories = _testStories;

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedStories))
                .Returns(true);

            // Act
            var result = await _hackerNewsService.GetNewestStoriesAsync(1);

            // Assert
            Assert.Equals(1, result.Count());
        }

        [Test]
        public async Task GetNewestStoriesAsync_ReturnsEmptyList_WhenServiceReturnsNull()
        {
            // Arrange
            object cachedStories = null;

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedStories))
                .Returns(false);

            _mockHttpClient.Setup(x => x.GetAsync<int[]>(It.IsAny<string>()))
                .ReturnsAsync((int[])null);

            // Act
            var result = await _hackerNewsService.GetNewestStoriesAsync(10);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetNewestStoriesAsync_ThrowsException_WhenHttpClientFails()
        {
            // Arrange
            object cachedStories = null;

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedStories))
                .Returns(false);

            _mockHttpClient.Setup(x => x.GetAsync<int[]>(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API failure"));

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(() =>
                _hackerNewsService.GetNewestStoriesAsync(10));
        }

        [Test]
        public async Task SearchStoriesAsync_ReturnsMatchingStories_WhenTermExists()
        {
            // Arrange
            object cachedStories = _testStories;
            var searchTerm = "Test";

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedStories))
                .Returns(true);

            // Act
            var result = await _hackerNewsService.SearchStoriesAsync(searchTerm);

            // Assert
            Assert.Equals(2, result.Count());
            Assert.That(result?.All(dto => dto.Title.Contains(searchTerm)), Is.True);
        }

        [Test]
        public async Task SearchStoriesAsync_ReturnsEmptyList_WhenNoMatches()
        {
            // Arrange
            object cachedStories = _testStories;
            var searchTerm = "NonExistent";

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedStories))
                .Returns(true);

            // Act
            var result = await _hackerNewsService.SearchStoriesAsync(searchTerm);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task SearchStoriesAsync_ReturnsAllStories_WhenTermIsEmpty()
        {
            // Arrange
            object cachedStories = _testStories;
            var searchTerm = "";

            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedStories))
                .Returns(true);

            // Act
            var result = await _hackerNewsService.SearchStoriesAsync(searchTerm);

            // Assert
            Assert.Equals(2, result.Count());
        }
    }
}