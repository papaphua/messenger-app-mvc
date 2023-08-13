﻿using AutoMapper;
using MessengerApp.Application.Dtos;
using MessengerApp.Domain.Entities;

namespace MessengerApp.Application;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.UserEmailDto, opt => opt.MapFrom(src => new UserEmailDto { Email = src.Email }))
            .ForMember(dest => dest.UserProfileDto, opt => opt.MapFrom(src => new UserProfileDto
            {
                UserName = src.UserName,
                FirstName = src.FirstName,
                LastName = src.LastName,
                Biography = src.Biography
            }));

        CreateMap<UserProfileDto, User>();
    }
}