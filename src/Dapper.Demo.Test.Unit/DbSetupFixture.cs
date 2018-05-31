namespace Dapper.Demo.Test.Unit
{
    using System;
    using System.Data.SqlClient;
    using System.IO;
    using System.Reflection;
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
            DropTable("dbo", "__Migrations");

            string migrationsScript = File.ReadAllText(Path.Combine(
                Path.GetDirectoryName((new Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath),
                "MigrationScript.sql"));

            DbConnection.Execute(migrationsScript);
        }

        public void Dispose()
        {
            DbConnection.Execute($"USE master; DROP DATABASE {TestsDbName}");
            DbConnection.Dispose();
        }
    }
}