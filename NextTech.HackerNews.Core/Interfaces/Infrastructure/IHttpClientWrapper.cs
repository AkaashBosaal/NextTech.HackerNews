namespace NextTech.HackerNews.Core.Interfaces.Infrastructure
{
    public interface IHttpClientWrapper
    {
        Task<T> GetAsync<T>(string url);
    }
}
