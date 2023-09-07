﻿using AutoMapper;
using MessengerApp.Application.Dtos.User;
using MessengerApp.Domain.Constants;
using MessengerApp.Domain.Entities;
using MessengerApp.Domain.Primitives;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MessengerApp.Application.Services.UserService;

public sealed class UserService : IUserService
{
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;

    public UserService(UserManager<User> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<UserPreviewDto>>> FindUsersByUsernameAsync(string? search)
    {
        var users = await _userManager.Users
            .Where(user => EF.Functions.Like(user.UserName!, $"%{search}%"))
            .ToListAsync();

        if (users.Count == 0)
            return new Result<IEnumerable<UserPreviewDto>>
            {
                Message = Results.NoSearchResultsFor(search)
            };

        var userPreviewDtos = users.Select(user => _mapper.Map<UserPreviewDto>(user));

        return new Result<IEnumerable<UserPreviewDto>>
        {
            Data = userPreviewDtos
        };
    }
}