﻿using MessengerApp.Domain.Abstractions.Chat;

namespace MessengerApp.Domain.Entities;

public sealed class Group
    : Chat<Group, GroupMessage, GroupAttachment, GroupReaction>, IAdministratedChat
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public byte[]? ChatPictureBytes { get; set; }

    public bool IsPrivate { get; set; }
}