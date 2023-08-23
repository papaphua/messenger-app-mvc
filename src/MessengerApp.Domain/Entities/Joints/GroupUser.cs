﻿using MessengerApp.Domain.Abstractions;

namespace MessengerApp.Domain.Entities.Joints;

public sealed class GroupUser : IEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string GroupId { get; set; } = null!;

    public string UserId { get; set; } = null!;
}