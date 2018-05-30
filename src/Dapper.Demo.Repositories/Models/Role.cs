namespace Dapper.Demo.Repositories.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Roles")]
    public class Role
    {
        [Key]
        public Guid RoleId { get; set; } = Guid.NewGuid();
        public string Type { get; set; }
    }
}