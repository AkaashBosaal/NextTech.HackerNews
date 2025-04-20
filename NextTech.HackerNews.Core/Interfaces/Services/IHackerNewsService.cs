using NextTech.HackerNews.Core.Entities;

namespace NextTech.HackerNews.Core.Interfaces.Services
{
    public interface IHackerNewsService
    {
        Task<IEnumerable<Story>> GetNewestStoriesAsync(int count);
        Task<IEnumerable<Story>> SearchStoriesAsync(string searchTerm);
    }
}
