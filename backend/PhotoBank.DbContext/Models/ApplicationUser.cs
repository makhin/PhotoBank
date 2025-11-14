using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace PhotoBank.DbContext.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public ApplicationUser()
        {
            // Generate a new GUID for each new user to avoid Guid.Empty primary key violations
            Id = Guid.NewGuid();
        }

        public long? TelegramUserId { get; set; }
        public TimeSpan? TelegramSendTimeUtc { get; set; }
        public string? TelegramLanguageCode { get; set; }
    }
}
