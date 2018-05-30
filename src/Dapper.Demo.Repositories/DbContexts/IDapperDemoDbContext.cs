namespace Dapper.Demo.Repositories.DbContexts
{
    using MicroOrm.Dapper.Repositories;
    using MicroOrm.Dapper.Repositories.DbContext;
    using Models;

    public interface IDapperDemoDbContext : IDapperDbContext
    {
        IDapperRepository<User> Users { get; }
        IDapperRepository<Role> Roles { get; }
        IDapperRepository<UserRole> UserRoles { get; }
    }
}