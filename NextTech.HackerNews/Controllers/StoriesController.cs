using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NextTech.HackerNews.Core.Dtos;
using NextTech.HackerNews.Core.Interfaces.Services;

namespace NextTech.HackerNews.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController(IHackerNewsService hackerNewsService, IMapper mapper) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StoryDto>>> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var stories = await hackerNewsService.GetNewestStoriesAsync(page * pageSize);
            var pagedStories = stories.Skip((page - 1) * pageSize).Take(pageSize);
            var result = mapper.Map<IEnumerable<StoryDto>>(pagedStories);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<StoryDto>>> Search(
            [FromQuery] string term,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var stories = await hackerNewsService.SearchStoriesAsync(term);
            var pagedStories = stories.Skip((page - 1) * pageSize).Take(pageSize);
            return Ok(mapper.Map<IEnumerable<StoryDto>>(pagedStories));
        }
    }
}
