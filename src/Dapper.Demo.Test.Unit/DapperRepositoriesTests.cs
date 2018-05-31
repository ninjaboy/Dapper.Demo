namespace Dapper.Demo.Test.Unit
{
    using FluentAssertions;
    using Repositories.DbContexts;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    [Collection("Database collection")]
    public class DapperRepositoriesTests
    {
        private readonly DbSetupFixture dbSetupFixture;

        public DapperRepositoriesTests(DbSetupFixture dbSetupFixture)
        {
            this.dbSetupFixture = dbSetupFixture;
        }

        [Fact]
        public void DapperRepositoriesTest()
        {
            var dapperDemoContext = new DapperDemoDbContext(dbSetupFixture.DbConnection);
            dapperDemoContext.CreateUser("email", "password", false, new List<Guid>() { });

            var users = dapperDemoContext.GetAllUsers();

            users.Count().Should().Be(1);
            users.First().Username.Should().Be("email");
            users.First().PasswordHash.Should().Be("password");
        }
    }
}