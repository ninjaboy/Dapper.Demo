namespace Dapper.Demo.Test.Unit
{
    using System;
    using System.Data.SqlClient;
    using Xunit;

    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<DbSetupFixture>
    {
    }

    public class DbSetupFixture : IDisposable
    {
        public SqlConnection DbConnection { get; set; }
        private const string ConnString = "Server=(local);Initial Catalog=master;Integrated Security=True";
        private const string TestsDbName = "test_dapper";

        public DbSetupFixture()
        {
            DbConnection = new SqlConnection(ConnString);
            DbConnection.Open();

            InitDb();
        }

        private void InitDb()
        {
            DbConnection.Execute($"IF NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = '{TestsDbName}') CREATE DATABASE [{TestsDbName}];");
            DbConnection.Execute($"USE [{TestsDbName}]");
            void DropTable(string schema, string name)
                => DbConnection.Execute($@"IF OBJECT_ID('{schema}.{name}', 'U') IS NOT NULL DROP TABLE [{schema}].[{name}]; ");
            DropTable("dbo", "Users");
            DropTable("dbo", "Roles");
            DropTable("dbo", "UserRoles");
            DbConnection.Execute("CREATE TABLE Users(UserId uniqueidentifier NOT NULL,Username nvarchar(256) NOT NULL, Email nvarchar(256) NOT NULL, PasswordHash nvarchar(512) NOT NULL, DeactivatedOn date NULL, GDPRSignedOn date NULL, CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId ASC)) ");
            DbConnection.Execute("CREATE TABLE UserRoles(UserId uniqueidentifier NOT NULL,RoleId uniqueidentifier NOT NULL)");
            DbConnection.Execute("CREATE TABLE Roles(RoleId uniqueidentifier NOT NULL, [Type] nvarchar(256) NOT NULL, CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId ASC)) ");
        }

        public void Dispose()
        {
            DbConnection.Execute($"USE master; DROP DATABASE {TestsDbName}");
            DbConnection.Dispose();
        }
    }
}