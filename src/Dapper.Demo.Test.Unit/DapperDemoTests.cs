using Xunit;

namespace Dapper.Demo.Test.Unit
{
    using FluentAssertions;
    using Repositories.Models;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    public class DapperDemoTests : IClassFixture<DbSetupFixture>
    {
        private readonly DbSetupFixture dbSetupFixture;

        public class Arrangements
        {
            public IDbConnection DbConnection { get; set; }
            public List<User> Users { get; set; } = new List<User>();
            public List<Role> Roles { get; set; } = new List<Role>();
            public List<UserRole> UserRoles { get; set; } = new List<UserRole>();

            public const string SqlRoleGetAll = "SELECT * FROM Roles";
            public const string SqlRoleGetById = "SELECT RoleId,[Type] FROM Roles WHERE RoleId = @RoleId";
            public const string SqlRoleInsert = "INSERT INTO Roles(RoleId,[Type]) VALUES(@RoleId, @Type)";
            public const string SqlUserGetById = "SELECT * FROM Users WHERE UserId = @UserId";
            public const string SqlUserInsert = "INSERT INTO [dbo].[Users]([UserId],[Username],[Email],[PasswordHash],[DeactivatedOn],[GDPRSignedOn]) VALUES(@UserId, @Username, @Email, @PasswordHash, @DeactivatedOn, @GDPRSignedOn)";
            public const string SqlUserUsernameUpdate = "UPDATE [dbo].[Users] SET [Username] = @Username WHERE UserId = @UserId";

            public const string SqlUserRoleGetAll = "SELECT * FROM UserRoles";
            public const string SqlUserRoleInsert = "INSERT INTO [dbo].[UserRoles]([UserId],[RoleId]) VALUES(@UserId, @RoleId)";

            public const string SqlUserGetByIdWithRoles = "select u.*, r.* FROM UserRoles ur " +
                    "INNER JOIN Users u ON ur.UserId = u.UserId " +
                    "INNER JOIN Roles r ON r.RoleId = ur.RoleId WHERE u.UserId = @UserId";
        }

        public class ArrangementsBuilder
        {
            private Arrangements A { get; set; }

            public ArrangementsBuilder(IDbConnection dbConnection)
            {
                A = new Arrangements { DbConnection = dbConnection };
            }

            public Arrangements Build()
            {
                return A;
            }

            public ArrangementsBuilder WithNewRole(Func<Role, Role> modifiers = null)
            {
                var role = new Role
                {
                    Type = "Admin"
                };

                role = modifiers != null ? modifiers(role) : role;

                A.Roles.Add(role);
                return this;

            }

            public ArrangementsBuilder WithNewUser(Func<User, User> modifiers = null)
            {
                var user = new User
                {
                    Username = "Mr Smith",
                    Email = "agent.smith@matrix.com",
                    PasswordHash = "123456789",
                };
                user = modifiers != null ? modifiers(user) : user;
                A.Users.Add(user);
                return this;
            }

            public ArrangementsBuilder WithPermuteUserRoles()
            {
                foreach (var user in A.Users)
                {
                    foreach (var role in A.Roles)
                    {
                        A.UserRoles.Add(new UserRole() { RoleId = role.RoleId, UserId = user.UserId });
                    }
                }
                return this;
            }

        }

        public DapperDemoTests(DbSetupFixture dbSetupFixture)
        {
            this.dbSetupFixture = dbSetupFixture;
        }

        private ArrangementsBuilder NewArrangementsBuilder() => new ArrangementsBuilder(dbSetupFixture.DbConnection);

        [Fact]
        public void DapperInsertSelect()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewRole().Build();
            //Act
            arrangements.DbConnection.Execute(Arrangements.SqlRoleInsert, arrangements.Roles[0]);
            var role = arrangements.DbConnection.QuerySingle<Role>(Arrangements.SqlRoleGetById, new { arrangements.Roles[0].RoleId });
            //Assert
            role.RoleId.Should().Be(arrangements.Roles[0].RoleId);
            role.Type.Should().Be(arrangements.Roles[0].Type);
        }



        [Fact]
        public void DapperUpdate()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().Build();
            arrangements.DbConnection.Execute(Arrangements.SqlUserInsert, arrangements.Users[0]);

            //Act
            arrangements.Users[0].Username = "Smith";
            arrangements.DbConnection.Execute(Arrangements.SqlUserUsernameUpdate, arrangements.Users[0]);
            var user = arrangements.DbConnection.QuerySingle<User>(Arrangements.SqlUserGetById, new { arrangements.Users[0].UserId });
            //Assert
            user.UserId.Should().Be(arrangements.Users[0].UserId);
            user.Username.Should().Be(arrangements.Users[0].Username);
        }

        [Fact]
        public void DapperInsertList()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewRole().WithNewRole().Build();
            //Act
            arrangements.DbConnection.Execute(Arrangements.SqlRoleInsert, arrangements.Roles);
            var role1 = arrangements.DbConnection.Query<Role>(Arrangements.SqlRoleGetById, new { arrangements.Roles[0].RoleId });
            var role2 = arrangements.DbConnection.Query<Role>(Arrangements.SqlRoleGetById, new { arrangements.Roles[1].RoleId });
            //Assert
            role1.Should().NotBeNullOrEmpty();
            role1.Single().Type.Should().Be(arrangements.Roles[0].Type);
            role2.Should().NotBeNullOrEmpty();
            role2.Single().Type.Should().Be(arrangements.Roles[1].Type);
        }


        [Fact]
        public void DapperTransaction()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().WithNewRole().WithPermuteUserRoles().Build();

            //Act
            using (var transaction = arrangements.DbConnection.BeginTransaction())
            {
                arrangements.DbConnection.Execute(Arrangements.SqlRoleInsert, arrangements.Roles[0], transaction);
                arrangements.DbConnection.Execute(Arrangements.SqlUserRoleInsert, arrangements.UserRoles[0], transaction);
                transaction.Rollback(); //Rollback to show that operations in scope will not happen
            }

            var role = arrangements.DbConnection.Query<Role>(Arrangements.SqlRoleGetById, new { arrangements.Roles[0].RoleId });
            var user = arrangements.DbConnection.Query<User>(Arrangements.SqlUserGetById, new { arrangements.Users[0].UserId });

            //Assert
            role.Should().BeEmpty();
            user.Should().BeEmpty();
        }

        [Fact]
        public void DapperSelectNested()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().WithNewRole().WithPermuteUserRoles().Build();

            //Act
            using (var transaction = arrangements.DbConnection.BeginTransaction())
            {
                arrangements.DbConnection.Execute(Arrangements.SqlRoleInsert, arrangements.Roles[0], transaction);
                arrangements.DbConnection.Execute(Arrangements.SqlUserInsert, arrangements.Users[0], transaction);
                arrangements.DbConnection.Execute(Arrangements.SqlUserRoleInsert, arrangements.UserRoles[0], transaction);
                transaction.Commit();
            }

            var userWithRoles = arrangements.DbConnection.Query<User, Role, User>(Arrangements.SqlUserGetByIdWithRoles,
                (u, r) =>
                {
                    u.Roles.Add(r);
                    return u;
                },
                new { arrangements.Users[0].UserId },
                splitOn: "UserId, RoleId"
                ).Single();

            //Assert
            userWithRoles.UserId.Should().Be(arrangements.Users[0].UserId);
            userWithRoles.Username.Should().Be(arrangements.Users[0].Username);
            userWithRoles.Roles.Should().NotBeNullOrEmpty();
            userWithRoles.Roles[0].RoleId.Should().Be(arrangements.Roles[0].RoleId);
            userWithRoles.Roles[0].Type.Should().Be(arrangements.Roles[0].Type);

        }


    }
}
