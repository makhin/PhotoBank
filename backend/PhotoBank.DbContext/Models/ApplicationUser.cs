﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace PhotoBank.DbContext.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Telegram { get; set; }
    }
}
