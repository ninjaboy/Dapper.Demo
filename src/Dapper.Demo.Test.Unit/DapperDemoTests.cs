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
        public void DapperInsertSelect()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewRole().Build();
            //Act

            arrangements.SUT.InsertRole(arrangements.Roles[0], arrangements.DbConnection);
            var role = arrangements.SUT.GetRoleById(arrangements.Roles[0].RoleId, arrangements.DbConnection);
            //Assert
            role.RoleId.Should().Be(arrangements.Roles[0].RoleId);
            role.Type.Should().Be(arrangements.Roles[0].Type);
        }



        [Fact]
        public void DapperUpdate()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().Build();
            arrangements.SUT.InsertUser(arrangements.Users[0], arrangements.DbConnection);

            //Act
            arrangements.Users[0].Username = "Smith";
            arrangements.SUT.UpdateUser(arrangements.Users[0], arrangements.DbConnection);
            var user = arrangements.SUT.GetUserById(arrangements.Users[0].UserId, arrangements.DbConnection);
            //Assert
            user.UserId.Should().Be(arrangements.Users[0].UserId);
            user.Username.Should().Be(arrangements.Users[0].Username);
        }

        [Fact]
        public void DapperInsertList()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewRole().WithNewRole().WithNewUser().WithPermuteUserRoles().Build();
            //Act
            arrangements.SUT.InsertUserRoles(arrangements.UserRoles, arrangements.DbConnection);
            var userRoles = arrangements.SUT.GetAllUserRoles(arrangements.DbConnection);

            //Assert
            userRoles.Count().Should().Be(2);
            userRoles.First().UserId.Should().Be(arrangements.Users[0].UserId);
            userRoles.First().RoleId.Should().Be(arrangements.Roles[0].RoleId);
        }


        [Fact]
        public void DapperTransaction()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().WithNewRole().WithPermuteUserRoles().Build();

            //Act
            using (var transaction = arrangements.DbConnection.BeginTransaction())
            {
                arrangements.SUT.InsertRole(arrangements.Roles[0], arrangements.DbConnection, transaction);
                arrangements.SUT.InsertUserRoles(new List<UserRole>() { arrangements.UserRoles[0] }, arrangements.DbConnection, transaction);
                arrangements.SUT.InsertUser(arrangements.Users[0], arrangements.DbConnection, transaction);
                transaction.Rollback(); //Rollback to show that operations in scope will not happen
            }

            //Assert
            Assert.Throws<InvalidOperationException>(() =>
                arrangements.SUT.GetRoleById(arrangements.Roles[0].RoleId, arrangements.DbConnection));
        }

        [Fact]
        public void DapperSelectNested()
        {
            //Arrange
            var arrangements = NewArrangementsBuilder().WithNewUser().WithNewRole().WithPermuteUserRoles().Build();
            using (var transaction = arrangements.DbConnection.BeginTransaction())
            {
                arrangements.SUT.InsertRole(arrangements.Roles[0], arrangements.DbConnection, transaction);
                arrangements.SUT.InsertUserRoles(new List<UserRole>() { arrangements.UserRoles[0] }, arrangements.DbConnection, transaction);
                arrangements.SUT.InsertUser(arrangements.Users[0], arrangements.DbConnection, transaction);
                transaction.Commit();
            }

            //Act
            var userWithRoles = arrangements.SUT.GetUserWithRolesById(arrangements.Users[0].UserId, arrangements.DbConnection);

            //Assert
            userWithRoles.UserId.Should().Be(arrangements.Users[0].UserId);
            userWithRoles.Username.Should().Be(arrangements.Users[0].Username);
            userWithRoles.Roles.Should().NotBeNullOrEmpty();
            userWithRoles.Roles[0].RoleId.Should().Be(arrangements.Roles[0].RoleId);
            userWithRoles.Roles[0].Type.Should().Be(arrangements.Roles[0].Type);

        }


    }
}
