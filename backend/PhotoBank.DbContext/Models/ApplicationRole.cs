using System;
using Microsoft.AspNetCore.Identity;

namespace PhotoBank.DbContext.Models
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public ApplicationRole()
        {
            // Generate a new GUID for each new role to avoid Guid.Empty primary key violations
            Id = Guid.NewGuid();
        }
    }
}
