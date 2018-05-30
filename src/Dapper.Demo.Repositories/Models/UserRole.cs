namespace Dapper.Demo.Repositories.Models
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("UserRoles")]
    public class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}