namespace Dapper.Demo.Repositories.Models
{
    using System;

    public class Role
    {
        public Guid RoleId { get; set; } = Guid.NewGuid();
        public string Type { get; set; }
    }
}