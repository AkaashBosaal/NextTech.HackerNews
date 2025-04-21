using Moq;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using NextTech.HackerNews.Core.Interfaces.Services;
using NextTech.HackerNews.Api.Controllers;
using NextTech.HackerNews.Core.Entities;
using NextTech.HackerNews.Core.Dtos;

namespace HackerNewsApi.Api.Tests
{
    [TestFixture]
    public class StoriesControllerTests
    {
        private Mock<IHackerNewsService> _mockHackerNewsService;
        private Mock<IMapper> _mockMapper;
        private StoriesController _controller;
        private List<Story> _testStories;
        private List<StoryDto> _testStoryDtos;

        [SetUp]
        public void Setup()
        {
            _mockHackerNewsService = new Mock<IHackerNewsService>();
            _mockMapper = new Mock<IMapper>();
            _controller = new StoriesController(_mockHackerNewsService.Object, _mockMapper.Object);

            _testStories =
            [
                new() { Id = 1, Title = "Test Story 1", Url = "http://test1.com", By = "user1", Time = 123456789, Score = 100, Descendants = 50 },
                new() { Id = 2, Title = "Test Story 2", Url = "http://test2.com", By = "user2", Time = 123456790, Score = 200, Descendants = 60 }
            ];

            _testStoryDtos =
            [
                new() { Title = "Test Story 1", Url = "http://test1.com", By = "user1", Time = DateTime.Now, Score = 100, CommentCount = 50 },
                new() { Title = "Test Story 2", Url = "http://test2.com", By = "user2", Time = DateTime.Now, Score = 200, CommentCount = 60 }
            ];
        }

        #region Get Stories Tests

        [Test]
        public async Task Get_ReturnsOkResultWithStories_WhenServiceReturnsStories()
        {
            // Arrange
            var page = 1;
            var pageSize = 10;
            var expectedStories = _testStories.Take(pageSize);
            var expectedDtos = _testStoryDtos.Take(pageSize);

            _mockHackerNewsService.Setup(x => x.GetNewestStoriesAsync(page * pageSize))
                .ReturnsAsync(_testStories);

            _mockMapper.Setup(x => x.Map<IEnumerable<StoryDto>>(It.IsAny<IEnumerable<Story>>()))
                .Returns(expectedDtos);

            // Act
            var result = await _controller.Get(page, pageSize);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            var returnedDtos = okResult?.Value as IEnumerable<StoryDto>;

            Assert.Equals(expectedDtos.Count(), returnedDtos?.Count() ?? default);
            Assert.Equals(expectedDtos.First().Title, returnedDtos?.First().Title ?? string.Empty);

            _mockHackerNewsService.Verify(x => x.GetNewestStoriesAsync(page * pageSize), Times.Once);
            _mockMapper.Verify(x => x.Map<IEnumerable<StoryDto>>(It.IsAny<IEnumerable<Story>>()), Times.Once);
        }

        [Test]
        public async Task Get_ReturnsEmptyList_WhenServiceReturnsNoStories()
        {
            // Arrange
            var page = 1;
            var pageSize = 10;

            _mockHackerNewsService.Setup(x => x.GetNewestStoriesAsync(page * pageSize))
                .ReturnsAsync(new List<Story>());

            _mockMapper.Setup(x => x.Map<IEnumerable<StoryDto>>(It.IsAny<IEnumerable<Story>>()))
                .Returns(new List<StoryDto>());

            // Act
            var result = await _controller.Get(page, pageSize);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            var returnedDtos = okResult?.Value as IEnumerable<StoryDto>;

            Assert.That(returnedDtos, Is.Empty);
        }

        [Test]
        public async Task Get_ReturnsCorrectPagedResults()
        {
            // Arrange
            var page = 2;
            var pageSize = 1;
            var expectedStory = _testStories.Skip((page - 1) * pageSize).Take(pageSize).First();
            var expectedDto = _testStoryDtos.Skip((page - 1) * pageSize).Take(pageSize).First();

            _mockHackerNewsService.Setup(x => x.GetNewestStoriesAsync(page * pageSize))
                .ReturnsAsync(_testStories);

            _mockMapper.Setup(x => x.Map<IEnumerable<StoryDto>>(It.IsAny<IEnumerable<Story>>()))
                .Returns(new List<StoryDto> { expectedDto });

            // Act
            var result = await _controller.Get(page, pageSize);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var returnedDtos = okResult?.Value as IEnumerable<StoryDto>;

            Assert.Equals(1, returnedDtos?.Count() ?? default);
            Assert.Equals(expectedDto.Title, returnedDtos?.First().Title ?? string.Empty);
        }

        [Test]
        public async Task Get_Returns500StatusCode_WhenServiceThrowsException()
        {
            // Arrange
            var page = 1;
            var pageSize = 10;

            _mockHackerNewsService.Setup(x => x.GetNewestStoriesAsync(page * pageSize))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.Get(page, pageSize);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
            var objectResult = result.Result as ObjectResult;
            Assert.Equals(500, objectResult?.StatusCode ?? default);
        }

        #endregion

        #region Search Stories Tests

        [Test]
        public async Task Search_ReturnsOkResultWithMatchingStories_WhenSearchTermExists()
        {
            // Arrange
            var searchTerm = "Test";
            var page = 1;
            var pageSize = 10;
            var expectedStories = _testStories.Where(s => s.Title.Contains(searchTerm));
            var expectedDtos = _testStoryDtos.Where(s => s.Title.Contains(searchTerm));

            _mockHackerNewsService.Setup(x => x.SearchStoriesAsync(searchTerm))
                .ReturnsAsync(expectedStories);

            _mockMapper.Setup(x => x.Map<IEnumerable<StoryDto>>(expectedStories))
                .Returns(expectedDtos);

            // Act
            var result = await _controller.Search(searchTerm, page, pageSize);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            var returnedDtos = okResult?.Value as IEnumerable<StoryDto>;

            Assert.That(returnedDtos?.All(dto => dto.Title.Contains(searchTerm)), Is.True);
            Assert.Equals(_testStoryDtos.Count, returnedDtos?.Count() ?? default);
        }

        [Test]
        public async Task Search_ReturnsAllStories_WhenSearchTermIsEmpty()
        {
            // Arrange
            var searchTerm = "";
            var page = 1;
            var pageSize = 10;

            _mockHackerNewsService.Setup(x => x.SearchStoriesAsync(searchTerm))
                .ReturnsAsync(_testStories);

            _mockMapper.Setup(x => x.Map<IEnumerable<StoryDto>>(_testStories))
                .Returns(_testStoryDtos);

            // Act
            var result = await _controller.Search(searchTerm, page, pageSize);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var returnedDtos = okResult?.Value as IEnumerable<StoryDto>;
            
            Assert.That(returnedDtos?.All(dto => dto.Title.Contains(searchTerm)), Is.True);
            Assert.Equals(_testStoryDtos.Count, returnedDtos?.Count() ?? default);
        }

        [Test]
        public async Task Search_ReturnsEmptyList_WhenNoMatchesFound()
        {
            // Arrange
            var searchTerm = "NonExistentTerm";
            var page = 1;
            var pageSize = 10;

            _mockHackerNewsService.Setup(x => x.SearchStoriesAsync(searchTerm))
                .ReturnsAsync(new List<Story>());

            _mockMapper.Setup(x => x.Map<IEnumerable<StoryDto>>(It.IsAny<IEnumerable<Story>>()))
                .Returns(new List<StoryDto>());

            // Act
            var result = await _controller.Search(searchTerm, page, pageSize);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var returnedDtos = okResult?.Value as IEnumerable<StoryDto>;

            Assert.That(returnedDtos, Is.Empty);
        }

        [Test]
        public async Task Search_Returns500StatusCode_WhenServiceThrowsException()
        {
            // Arrange
            var searchTerm = "Test";
            var page = 1;
            var pageSize = 10;

            _mockHackerNewsService.Setup(x => x.SearchStoriesAsync(searchTerm))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.Search(searchTerm, page, pageSize);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
            var objectResult = result.Result as ObjectResult;
            Assert.Equals(500, objectResult?.StatusCode ?? default);
        }

        #endregion
    }
}