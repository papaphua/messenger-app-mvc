﻿using MessengerApp.Application.Dtos;
using MessengerApp.Application.Dtos.Channel;
using MessengerApp.Application.Services.ChannelService;
using MessengerApp.Application.Services.ProfileService;
using MessengerApp.Domain.Constants;
using MessengerApp.Domain.Enumerations;
using MessengerApp.Domain.Primitives;
using MessengerApp.WebApp.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace MessengerApp.WebApp.Controllers;

public sealed class ChannelController : Controller
{
    private readonly IChannelService _channelService;
    private readonly IProfileService _profileService;

    public ChannelController(IChannelService channelService, IProfileService profileService)
    {
        _channelService = channelService;
        _profileService = profileService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = Parser.ParseUserId(HttpContext);

        var result = await _channelService.GetChannelPreviewsAsync(userId);

        if (!result.Succeeded) return RedirectToAction("Index", "Channel");

        var channelPreviews = result.Data;

        return View(channelPreviews);
    }

    public async Task<IActionResult> Chat(string channelId)
    {
        var userId = Parser.ParseUserId(HttpContext);

        Result<ChannelDto> result;

        if (TempData.TryGetValue("ChannelId", out var value))
            result = await _channelService.GetChannelAsync(userId, value!.ToString()!);
        else
            result = await _channelService.GetChannelAsync(userId, channelId);

        if (!result.Succeeded) return RedirectToAction("Index", "Channel");

        var channel = result.Data!;

        var profile = (await _profileService.GetProfileAsync(userId)).Data!;

        ViewBag.Username = profile.ProfileInfoDto.UserName;
        ViewBag.ProfilePictureBytes = Convert.ToBase64String(profile.ProfilePictureBytes!);

        return View(channel);
    }

    public IActionResult New()
    {
        return View();
    }

    public async Task<IActionResult> Comments(string messageId)
    {
        var userId = Parser.ParseUserId(HttpContext);

        var result = await _channelService.GetChannelMessageCommentsAsync(userId, messageId);
        
        TempData[Notifications.Message] = result.Message;
        TempData[Notifications.Succeeded] = result.Succeeded;

        if (!result.Succeeded) return RedirectToAction("Index");

        return View(result.Data);
    }
    
    public async Task<IActionResult> CreateChannel(ChannelInfoDto channelInfoDto)
    {
        var userId = Parser.ParseUserId(HttpContext);

        var chatPicture = Request.Form.Files[0];

        using var memoryStream = new MemoryStream();

        await chatPicture.CopyToAsync(memoryStream);
        var chatPictureBytes = memoryStream.ToArray();

        channelInfoDto.ChatPictureBytes = chatPictureBytes;

        var result = await _channelService.CreateChannelAsync(userId, channelInfoDto);

        TempData[Notifications.Message] = result.Message;
        TempData[Notifications.Succeeded] = result.Succeeded;

        if (!result.Succeeded) return RedirectToAction("Index", "Channel");

        return View("Chat", result.Data);
    }

    public async Task<IActionResult> JoinChannel(string channelId)
    {
        var userId = Parser.ParseUserId(HttpContext);

        var result = await _channelService.JoinChannelAsync(userId, channelId);

        TempData[Notifications.Message] = result.Message;
        TempData[Notifications.Succeeded] = result.Succeeded;

        if (!result.Succeeded) return RedirectToAction("Index", "Channel");

        return View("Chat", result.Data);
    }

    public async Task<IActionResult> LeaveChannel(string channelId)
    {
        var userId = Parser.ParseUserId(HttpContext);

        var result = await _channelService.LeaveChannelAsync(userId, channelId);

        TempData[Notifications.Message] = result.Message;
        TempData[Notifications.Succeeded] = result.Succeeded;

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> CreateChannelMessage(string channelId, CreateMessageDto createMessageDto)
    {
        var userId = Parser.ParseUserId(HttpContext);

        var attachedFiles = Request.Form.Files;

        var attachments = new List<byte[]>();

        if (attachedFiles.Any())
            foreach (var attachment in attachedFiles)
            {
                using var memoryStream = new MemoryStream();
                await attachment.CopyToAsync(memoryStream);
                attachments.Add(memoryStream.ToArray());
            }

        createMessageDto.Attachments = attachments;

        await _channelService.CreateChannelMessageAsync(userId, channelId, createMessageDto);

        var channel = (await _channelService.GetChannelAsync(userId, channelId)).Data!;

        TempData["ChannelId"] = channel.Id;

        return RedirectToAction("Chat");
    }

    public async Task<IActionResult> CreateComment(string messageId, CreateCommentDto createCommentDto)
    {
        var userId = Parser.ParseUserId(HttpContext);

        var result = await _channelService.CreateChannelMessageCommentAsync(userId, messageId, createCommentDto);
        
        TempData[Notifications.Message] = result.Message;
        TempData[Notifications.Succeeded] = result.Succeeded;

        var commentResult = await _channelService.GetChannelMessageCommentsAsync(userId, messageId);
        
        return View("Comments", commentResult.Data);
    }
    
    public async Task AddReaction(string messageId, Reaction reaction)
    {
        var userId = Parser.ParseUserId(HttpContext);

        await _channelService.CreateChannelReactionAsync(userId, messageId, reaction);
    }
}