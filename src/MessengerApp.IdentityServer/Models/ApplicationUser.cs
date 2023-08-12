﻿using Microsoft.AspNetCore.Identity;

namespace MessengerApp.IdentityServer.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Biography { get; set; }
}