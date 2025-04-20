namespace NextTech.HackerNews.Core.Configurations
{
    public class HackerNewsOptions
    {
        public const string SectionName = "HackerNews";
        public string BaseUrl { get; set; }
        public string CacheKey { get; set; }
        public int CacheDurationMinutes { get; set; }
    }
}
