namespace Dapper.Demo.Repositories.DbContexts
{
    using MicroOrm.Dapper.Repositories;
    using MicroOrm.Dapper.Repositories.DbContext;
    using MicroOrm.Dapper.Repositories.SqlGenerator;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Data;

    public class DapperDemoDbContext : DapperDbContext, IDapperDemoDbContext
    {
        private IDapperRepository<User> _users;
        private IDapperRepository<Role> _roles;
        private IDapperRepository<UserRole> _userRoles;

        private readonly SqlGeneratorConfig _config = new SqlGeneratorConfig
        {
            SqlConnector = ESqlConnector.MSSQL,
            UseQuotationMarks = true
        };

        public DapperDemoDbContext(IDbConnection connection) : base(connection)
        {
        }

        public IDapperRepository<User> Users => _users ?? (_users = new DapperRepository<User>(Connection, _config));
        public IDapperRepository<Role> Roles => _roles ?? (_roles = new DapperRepository<Role>(Connection, _config));
        public IDapperRepository<UserRole> UserRoles => _userRoles ?? (_userRoles = new DapperRepository<UserRole>(Connection, _config));

        public IEnumerable<User> GetAllUsers()
        {
            return Users.FindAll();
        }

        public User CreateUser(string email, string password, bool gdprAccepted, IList<Guid> roleIds)
        {
            return InTransactionContext((transaction) =>
            {
                var newUser = new User
                {
                    Email = email,
                    Username = email,
                    PasswordHash = GeneratePasswordHash(password),
                    GDPRSignedOn = gdprAccepted ? DateTime.UtcNow : (DateTime?)null,
                };

                Users.Insert(newUser, transaction);

                foreach (var roleId in roleIds)
                {
                    UserRoles.Insert(new UserRole
                    {
                        UserId = newUser.UserId,
                        RoleId = roleId
                    }, transaction);
                }

                transaction.Commit();
                return newUser;

            });
        }

        private T InTransactionContext<T>(Func<IDbTransaction, T> func)
        {
            using (var transaction = this.BeginTransaction())
            {
                return func(transaction);
            }
        }

        private string GeneratePasswordHash(string password)
        {
            return password;
        }

    }
}