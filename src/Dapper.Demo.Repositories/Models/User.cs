namespace Dapper.Demo.Repositories.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Users")]
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public DateTime? DeactivatedOn { get; set; }
        public DateTime? GDPRSignedOn { get; set; }

        public List<Role> Roles { get; set; } = new List<Role>();
    }
}