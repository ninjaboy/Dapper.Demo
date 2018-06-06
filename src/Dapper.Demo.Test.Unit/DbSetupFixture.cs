namespace Dapper.Demo.Test.Unit
{
    using Repositories.DbHelpers;
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

            DbScaffolder.InitDb(DbConnection, TestsDbName);
        }


        public void Dispose()
        {
            try
            {
                DbScaffolder.DropDb(DbConnection, TestsDbName);
                DbConnection.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}