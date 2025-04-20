namespace NextTech.HackerNews.Core.Dtos
{
    public class StoryDto
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string By { get; set; }
        public DateTime Time { get; set; }
        public int Score { get; set; }
        public int CommentCount { get; set; }
    }
}
