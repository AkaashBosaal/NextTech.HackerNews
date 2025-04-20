using AutoMapper;
using NextTech.HackerNews.Core.Dtos;
using NextTech.HackerNews.Core.Entities;

namespace NextTech.HackerNews.Api.Mappings
{
    public class StoryProfile : Profile
    {
        public StoryProfile() {

            CreateMap<Story, StoryDto>()
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Descendants))
                .ForMember(dest => dest.Time, opt => opt.MapFrom(src =>
                    DateTimeOffset.FromUnixTimeSeconds(src.Time).DateTime));

        }
    }
}
