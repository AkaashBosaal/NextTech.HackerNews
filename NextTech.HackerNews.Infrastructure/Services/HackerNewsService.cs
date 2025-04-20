using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextTech.HackerNews.Core.Configurations;
using NextTech.HackerNews.Core.Entities;
using NextTech.HackerNews.Core.Interfaces.Infrastructure;
using NextTech.HackerNews.Core.Interfaces.Services;

namespace NextTech.HackerNews.Infrastructure.Services
{
    public class HackerNewsService(
        IHttpClientWrapper httpClient,
        IMemoryCache cache,
        ILogger<HackerNewsService> logger,
        IOptions<HackerNewsOptions> options) : IHackerNewsService
    {
        private readonly HackerNewsOptions _options = options.Value;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<IEnumerable<Story>> GetNewestStoriesAsync(int count)
        {
            try
            {
                await _semaphore.WaitAsync();

                if (!cache.TryGetValue(_options.CacheKey, out List<Story> cachedStories))
                {
                    var storyIds = await httpClient.GetAsync<int[]>($"{_options.BaseUrl}newstories.json");
                    var limitedStoryIds = storyIds.Take(500).ToArray();

                    var storyTasks = limitedStoryIds.Select(id =>
                        httpClient.GetAsync<Story>($"{_options.BaseUrl}item/{id}.json"));
                    var stories = await Task.WhenAll(storyTasks);

                    cachedStories = stories.Where(s => s != null).ToList();
                    cache.Set(_options.CacheKey, cachedStories,
                        TimeSpan.FromMinutes(_options.CacheDurationMinutes));
                }

                return cachedStories.Take(count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching newest stories");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEnumerable<Story>> SearchStoriesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetNewestStoriesAsync(100);

            var stories = await GetNewestStoriesAsync(500);
            return stories.Where(s =>
                s.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }
    }
}
