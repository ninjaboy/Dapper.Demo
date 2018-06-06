namespace Dapper.Demo.Repositories.DbHelpers
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Reflection;

    public class DbScaffolder
    {
        public static void InitDb(IDbConnection connection, string dbName)
        {
            connection.Execute($"IF NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = '{dbName}') CREATE DATABASE [{dbName}];");
            connection.Execute($"USE [{dbName}]");
            void DropTable(string schema, string name)
                => connection.Execute($@"IF OBJECT_ID('{schema}.{name}', 'U') IS NOT NULL DROP TABLE [{schema}].[{name}]; ");
            DropTable("dbo", "UserRoles");
            DropTable("dbo", "Users");
            DropTable("dbo", "Roles");
            DropTable("dbo", "__Migrations");

            var migrationsScript = File.ReadAllText(Path.Combine(
                Path.GetDirectoryName((new Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath) ?? throw new InvalidOperationException(),
                "MigrationScript.sql"));

            connection.Execute(migrationsScript);
        }

        public static void DropDb(SqlConnection dbConnection, string dbName)
        {
            dbConnection.Execute($"USE master; DROP DATABASE {dbName}");
        }
    }
}