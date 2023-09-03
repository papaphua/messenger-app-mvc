﻿using AutoMapper;
using MessengerApp.Application.Abstractions.Data;
using MessengerApp.Application.Dtos;
using MessengerApp.Application.Dtos.Group;
using MessengerApp.Domain.Constants;
using MessengerApp.Domain.Entities;
using MessengerApp.Domain.Entities.Joints;
using MessengerApp.Domain.Primitives;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MessengerApp.Application.Services.GroupService;

public sealed class GroupService : IGroupService
{
    private readonly IDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;

    public GroupService(IDbContext dbContext, IUnitOfWork unitOfWork, IMapper mapper, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<Result<GroupDto>> GetGroupAsync(string userId, string groupId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new Result<GroupDto>
            {
                Succeeded = false,
                Message = Results.UserNotFound
            };

        var group = await _dbContext.Set<Group>()
            .Include(group => group.Messages)
            .ThenInclude(message => message.Attachments)
            .Include(group => group.Messages)
            .ThenInclude(message => message.Reactions)
            .Include(group => group.Members)
            .FirstOrDefaultAsync(group => group.Id == groupId &&
                                          group.Members.Any(member => member.Id == userId));

        if (group == null)
            return new Result<GroupDto>
            {
                Succeeded = false,
                Message = Results.ChatNotFound
            };

        var groupDto = _mapper.Map<GroupDto>(group);
        groupDto.Messages = group.Messages
            .OrderBy(message => message.Timestamp)
            .Reverse();
        
        return new Result<GroupDto>
        {
            Data = groupDto
        };
    }

    public async Task<Result<IEnumerable<GroupPreviewDto>>> GetGroupPreviewsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new Result<IEnumerable<GroupPreviewDto>>
            {
                Succeeded = false,
                Message = Results.UserNotFound
            };

        var groups = await _dbContext.Set<Group>()
            .Include(group => group.Members)
            .Where(group => group.Members.Any(member => member.Id == user.Id))
            .ToListAsync();

        if (groups.Count == 0)
            return new Result<IEnumerable<GroupPreviewDto>>
            {
                Message = Results.ChatsEmpty
            };

        var groupPreviewDtos = groups
            .Select(group => _mapper.Map<GroupPreviewDto>(group))
            .ToList();

        return new Result<IEnumerable<GroupPreviewDto>>
        {
            Data = groupPreviewDtos
        };
    }

    public async Task<Result<GroupDto>> CreateGroupAsync(string userId, GroupInfoDto groupInfoDto)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new Result<GroupDto>
            {
                Succeeded = false,
                Message = Results.UserNotFound
            };

        var group = new Group();
        _mapper.Map(groupInfoDto, group);

        var groupMember = GroupMember.AddMemberToGroup(group.Id, user.Id);
        groupMember.IsAdmin = true;
        groupMember.IsOwner = true;

        var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _dbContext.AddAsync(group);
            await _unitOfWork.SaveChangesAsync();

            await _dbContext.AddAsync(groupMember);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            return new Result<GroupDto>
            {
                Succeeded = false,
                Message = Results.ChatCreateError
            };
        }

        await transaction.CommitAsync();

        return await GetGroupAsync(user.Id, group.Id);
    }

    public async Task<Result> LeaveGroupAsync(string userId, string groupId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new Result
            {
                Succeeded = false,
                Message = Results.UserNotFound
            };

        var group = await _dbContext.Set<Group>()
            .Include(group => group.Members)
            .FirstOrDefaultAsync(group => group.Id == groupId);

        if (group == null)
            return new Result
            {
                Succeeded = false,
                Message = Results.ChatNotFound
            };

        var groupMember = await _dbContext.Set<GroupMember>()
            .FirstOrDefaultAsync(member => member.GroupId == group.Id);

        if (groupMember == null)
            return new Result
            {
                Succeeded = false,
                Message = Results.ChatNotFound
            };

        try
        {
            if (groupMember is { IsOwner: true, IsAdmin: true })
            {
                var otherOwners = await _dbContext.Set<GroupMember>()
                    .Where(member => member.GroupId == group.Id &&
                                     member.MembersId != groupMember.MembersId &&
                                     member.IsOwner)
                    .ToListAsync();

                var otherAdmins = await _dbContext.Set<GroupMember>()
                    .Where(member => member.GroupId == group.Id &&
                                     member.MembersId != groupMember.MembersId &&
                                     !member.IsOwner && member.IsAdmin)
                    .ToListAsync();

                var otherMembers = await _dbContext.Set<GroupMember>()
                    .Where(member => member.GroupId == group.Id &&
                                     member.MembersId != groupMember.MembersId &&
                                     !member.IsOwner && !member.IsAdmin)
                    .ToListAsync();

                if (otherOwners.Count >= 1)
                {
                    _dbContext.Remove(groupMember);
                }
                else if (otherAdmins.Count >= 1)
                {
                    var admin = otherAdmins.First();
                    admin.IsOwner = true;

                    _dbContext.Remove(groupMember);
                }
                else if (otherMembers.Count >= 1)
                {
                    var member = otherMembers.First();
                    member.IsOwner = true;
                    member.IsAdmin = true;

                    _dbContext.Remove(groupMember);
                }
                else
                {
                    _dbContext.Remove(group);
                }
            }
            else
            {
                _dbContext.Remove(groupMember);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception)
        {
            return new Result
            {
                Succeeded = false,
                Message = Results.ChatLeaveError
            };
        }

        return new Result();
    }

    public async Task<Result> CreateGroupMessageAsync(string userId, string groupId, CreateMessageDto createMessageDto)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new Result
            {
                Succeeded = false,
                Message = Results.UserNotFound
            };

        var group = await _dbContext.Set<Group>()
            .Include(group => group.Members)
            .FirstOrDefaultAsync(group => group.Id == groupId &&
                                          group.Members.Any(member => member.Id == user.Id));

        if (group == null)
            return new Result
            {
                Succeeded = false,
                Message = Results.ChatNotFound
            };

        var message = new GroupMessage()
        {
            SenderId = user.Id,
            ChatId = group.Id
        };
        _mapper.Map(createMessageDto, message);

        try
        {
            await _dbContext.Set<GroupMessage>()
                .AddAsync(message);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception)
        {
            return new Result
            {
                Succeeded = false,
                Message = Results.MessageSendError
            };
        }

        return new Result();
    }
}