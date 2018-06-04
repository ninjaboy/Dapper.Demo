namespace Dapper.Demo.Repositories
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;

    public class DapperDemoDbAccess
    {
        private const string SqlRoleGetAll = "SELECT * FROM Roles";
        private const string SqlRoleGetById = "SELECT RoleId,[Type] FROM Roles WHERE RoleId = @RoleId";
        private const string SqlRoleInsert = "INSERT INTO Roles(RoleId,[Type]) VALUES(@RoleId, @Type)";

        private const string SqlUserGetById = "SELECT * FROM Users WHERE UserId = @UserId";

        private static readonly string SqlUserInsert =
            WithConcurrencyUpdateDecorator("INSERT INTO [dbo].[Users]" +
                                           "([UserId],[Username],[Email],[PasswordHash],[DeactivatedOn],[GDPRSignedOn]) " +
                                           "VALUES(@UserId, @Username, @Email, @PasswordHash, @DeactivatedOn, @GDPRSignedOn)",
                                           "Users", "UserId");

        private static readonly string SqlUserUpdate = WithConcurrencyUpdateDecorator("UPDATE [dbo].[Users] " +
                                             "SET [Username] = @Username" +
                                             ", Email = @Email" +
                                             ", PasswordHash = @PasswordHash " +
                                             ", DeactivatedOn = @DeactivatedOn " +
                                             ", GDPRSignedOn = @GDPRSignedOn " +
                                              "WHERE UserId = @UserId AND ConcurrencyToken = @ConcurrencyToken"
                                             , "Users", "UserId");


        private const string SqlUserRoleGetAll = "SELECT * FROM UserRoles";
        private const string SqlUserRoleInsert = "INSERT INTO [dbo].[UserRoles]([UserId],[RoleId]) VALUES(@UserId, @RoleId)";

        private const string SqlUserGetByIdWithRoles = "select u.*, r.* FROM UserRoles ur " +
                                                      "INNER JOIN Users u ON ur.UserId = u.UserId " +
                                                      "INNER JOIN Roles r ON r.RoleId = ur.RoleId " +
                                                      "WHERE u.UserId = @UserId";

        //This is so dangerous in so many ways. Use with care
        private static string WithConcurrencyUpdateDecorator(string input, string tableName, string keyName)
        {
            return $"{input}{Environment.NewLine} SELECT @ConcurrencyToken = ConcurrencyToken FROM {tableName} where {keyName} = @{keyName}";
        }
        private DynamicParameters WithConcurrencyTokenUpdate(User user) => new DynamicParameters(user).Output(user, u => u.ConcurrencyToken);


        public Task<User> GetUserById(Guid userId, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            return dbConnection.QuerySingleAsync<User>(SqlUserGetById, new { UserId = userId }, transaction);
        }

        public async Task<User> GetUserWithRolesById(Guid userId, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            var user = (await dbConnection.QueryAsync<User, Role, User>(SqlUserGetByIdWithRoles,
                (u, r) =>
                {
                    u.Roles.Add(r);
                    return u;
                },
                new { UserId = userId },
                transaction,
                splitOn: "UserId, RoleId")).Single();
            return user;
        }

        public async Task<bool> InsertUser(User user, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            var script = SqlUserInsert;
            return await dbConnection.ExecuteAsync(script, WithConcurrencyTokenUpdate(user), transaction) > 0;
        }

        public async Task<bool> UpdateUser(User user, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            return await dbConnection.ExecuteAsync(SqlUserUpdate, WithConcurrencyTokenUpdate(user), transaction) > 0;
        }


        public Task<Role> GetRoleById(Guid roleId, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            return dbConnection.QuerySingleAsync<Role>(SqlRoleGetById, new { RoleId = roleId }, transaction);
        }

        public async Task<bool> InsertRole(Role role, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            return await dbConnection.ExecuteAsync(SqlRoleInsert, role, transaction) > 0;
        }

        public Task<IEnumerable<Role>> GetAllRoles(IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            return dbConnection.QueryAsync<Role>(SqlRoleGetAll, transaction: transaction);
        }

        public Task<IEnumerable<UserRole>> GetAllUserRoles(IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            return dbConnection.QueryAsync<UserRole>(SqlUserRoleGetAll, transaction: transaction);
        }

        public async Task<bool> InsertUserRoles(List<UserRole> userRoles, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            return await dbConnection.ExecuteAsync(SqlUserRoleInsert, userRoles, transaction) > 0;
        }

    }
}