using Xunit;

namespace Dapper.Demo.Test.Unit
{
    using FluentAssertions;
    using Repositories;
    using Repositories.Models;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;

    [Collection("Database collection")]
    public class DapperDemoTests
    {
        private readonly DbSetupFixture dbSetupFixture;

        public class Arrangements
        {
            public IDbConnection DbConnection { get; set; }
            public List<User> Users { get; set; } = new List<User>();
            public List<Role> Roles { get; set; } = new List<Role>();
            public List<UserRole> UserRoles { get; set; } = new List<UserRole>();

            public DapperDemoDbAccess SUT = new DapperDemoDbAccess();

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
            dbSetupFixture.DbConnection.Execute("DELETE FROM Users; DELETE FROM Roles; DELETE FROM UserRoles");
        }

        private ArrangementsBuilder NewArrangementsBuilder() => new ArrangementsBuilder(dbSetupFixture.DbConnection);

        [Fact]
        public async Task DapperInsert()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewRole().Build();
            //Act

            bool success = await arrangements.SUT.InsertRole(arrangements.Roles[0], arrangements.DbConnection);
            //Assert
            success.Should().BeTrue();
        }


        [Fact]
        public async Task DapperGet()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().Build();
            var success = await arrangements.SUT.InsertUser(arrangements.Users[0], arrangements.DbConnection);
            if (!success)
            {
                throw new Exception("Error creating User");
            }
            //Act
            var user = await arrangements.SUT.GetUserById(arrangements.Users[0].UserId, arrangements.DbConnection);

            //Assert
            user.UserId.Should().Be(arrangements.Users[0].UserId);
        }



        [Fact]
        public async Task DapperUpdate()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().Build();
            await arrangements.SUT.InsertUser(arrangements.Users[0], arrangements.DbConnection);

            //Act
            arrangements.Users[0].Username = "Smith";
            var version = arrangements.Users[0].ConcurrencyToken;
            var success = await arrangements.SUT.UpdateUser(arrangements.Users[0], arrangements.DbConnection);

            //Assert
            success.Should().BeTrue();
            arrangements.Users[0].ConcurrencyToken.Should().NotEqual(version);
        }

        [Fact]
        public async Task DapperInsertList()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewRole().WithNewRole().WithNewUser().WithPermuteUserRoles().Build();
            //Act
            await arrangements.SUT.InsertUserRoles(arrangements.UserRoles, arrangements.DbConnection);
            var userRoles = await arrangements.SUT.GetAllUserRoles(arrangements.DbConnection);

            //Assert
            userRoles.Count().Should().Be(2);
            userRoles.First().UserId.Should().Be(arrangements.Users[0].UserId);
            userRoles.First().RoleId.Should().Be(arrangements.Roles[0].RoleId);
        }


        [Fact]
        public async Task DapperTransaction()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().WithNewRole().WithPermuteUserRoles().Build();

            //Act
            using (var transaction = arrangements.DbConnection.BeginTransaction())
            {
                await arrangements.SUT.InsertRole(arrangements.Roles[0], arrangements.DbConnection, transaction);
                await arrangements.SUT.InsertUserRoles(new List<UserRole>() { arrangements.UserRoles[0] }, arrangements.DbConnection, transaction);
                await arrangements.SUT.InsertUser(arrangements.Users[0], arrangements.DbConnection, transaction);
                transaction.Rollback(); //Rollback to show that operations in scope will not happen
            }

            //Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                arrangements.SUT.GetRoleById(arrangements.Roles[0].RoleId, arrangements.DbConnection));
        }

        [Fact]
        public async Task DapperSelectNested()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().WithNewRole().WithPermuteUserRoles().Build();
            using (var transaction = arrangements.DbConnection.BeginTransaction())
            {
                await arrangements.SUT.InsertRole(arrangements.Roles[0], arrangements.DbConnection, transaction);
                await arrangements.SUT.InsertUserRoles(new List<UserRole>() { arrangements.UserRoles[0] }, arrangements.DbConnection, transaction);
                await arrangements.SUT.InsertUser(arrangements.Users[0], arrangements.DbConnection, transaction);
                transaction.Commit();
            }

            //Act
            var userWithRoles = await arrangements.SUT.GetUserWithRolesById(arrangements.Users[0].UserId, arrangements.DbConnection);

            //Assert
            userWithRoles.UserId.Should().Be(arrangements.Users[0].UserId);
            userWithRoles.Username.Should().Be(arrangements.Users[0].Username);
            userWithRoles.Roles.Should().NotBeNullOrEmpty();
            userWithRoles.Roles[0].RoleId.Should().Be(arrangements.Roles[0].RoleId);
            userWithRoles.Roles[0].Type.Should().Be(arrangements.Roles[0].Type);
        }


    }
}
