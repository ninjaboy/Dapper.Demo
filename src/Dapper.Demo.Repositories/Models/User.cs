namespace Dapper.Demo.Repositories.Models
{
    using System;
    using System.Collections.Generic;

    public class User
    {
        public Guid UserId { get; set; } = Guid.NewGuid();

        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public DateTime? DeactivatedOn { get; set; }
        public DateTime? GDPRSignedOn { get; set; }

        public List<Role> Roles { get; set; } = new List<Role>();
    }
}