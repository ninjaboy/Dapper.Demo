namespace Dapper.Demo.Repositories
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    public class DapperDemoDbAccess
    {
        private const string SqlRoleGetAll = "SELECT * FROM Roles";
        private const string SqlRoleGetById = "SELECT RoleId,[Type] FROM Roles WHERE RoleId = @RoleId";
        private const string SqlRoleInsert = "INSERT INTO Roles(RoleId,[Type]) VALUES(@RoleId, @Type)";
        private const string SqlUserGetById = "SELECT * FROM Users WHERE UserId = @UserId";
        private const string SqlUserInsert = "INSERT INTO [dbo].[Users]([UserId],[Username],[Email],[PasswordHash],[DeactivatedOn],[GDPRSignedOn]) VALUES(@UserId, @Username, @Email, @PasswordHash, @DeactivatedOn, @GDPRSignedOn)";
        private const string SqlUserUpdate = "UPDATE [dbo].[Users] SET [Username] = @Username, Email = @Email, PasswordHash = @PasswordHash, DeactivatedOn = @DeactivatedOn, GDPRSignedOn = @GDPRSignedOn " +
                                              "WHERE UserId = @UserId";

        private const string SqlUserRoleGetAll = "SELECT * FROM UserRoles";
        private const string SqlUserRoleInsert = "INSERT INTO [dbo].[UserRoles]([UserId],[RoleId]) VALUES(@UserId, @RoleId)";

        private const string SqlUserGetByIdWithRoles = "select u.*, r.* FROM UserRoles ur " +
                                                      "INNER JOIN Users u ON ur.UserId = u.UserId " +
                                                      "INNER JOIN Roles r ON r.RoleId = ur.RoleId WHERE u.UserId = @UserId";


        public User GetUserById(Guid userId, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            var user = dbConnection.QuerySingle<User>(SqlUserGetById, new { UserId = userId }, transaction);
            return user;
        }

        public User GetUserWithRolesById(Guid userId, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            var user = dbConnection.Query<User, Role, User>(SqlUserGetByIdWithRoles,
                (u, r) =>
                {
                    u.Roles.Add(r);
                    return u;
                },
                new { UserId = userId },
                transaction,
                splitOn: "UserId, RoleId").Single();
            return user;
        }

        public void InsertUser(User user, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            dbConnection.Execute(SqlUserInsert, user, transaction);
        }

        public void UpdateUser(User user, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            dbConnection.Execute(SqlUserUpdate, user, transaction);
        }


        public Role GetRoleById(Guid roleId, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            var role = dbConnection.QuerySingle<Role>(SqlRoleGetById, new { RoleId = roleId }, transaction);
            return role;
        }

        public void InsertRole(Role role, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            dbConnection.Execute(SqlRoleInsert, role, transaction);
        }

        public IEnumerable<Role> GetAllRoles(IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            return dbConnection.Query<Role>(SqlRoleGetAll, transaction: transaction);
        }

        public IEnumerable<UserRole> GetAllUserRoles(IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            return dbConnection.Query<UserRole>(SqlUserRoleGetAll, transaction: transaction);
        }

        public void InsertUserRoles(List<UserRole> userRoles, IDbConnection dbConnection, IDbTransaction transaction = null)
        {
            dbConnection.Execute(SqlUserRoleInsert, userRoles, transaction);
        }

    }
}