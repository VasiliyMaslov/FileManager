using AutoMapper;
using FileManager.Dtos;
using FileManager.Entities;

namespace FileManager.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
        }
    }
}
