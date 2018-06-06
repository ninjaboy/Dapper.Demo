namespace Dapper.Demo.Benchmarks
{
    using Repositories;
    using Repositories.Models;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;

    public class DbHelper
    {

        public static async Task<List<Guid>> SeedDb(IDbConnection conn, DapperDemoDbAccess dbAccess, int numerOfRowsToSeed)
        {
            var idCache = new List<Guid>();
            for (var i = 0; i < numerOfRowsToSeed; i++)
            {
                var email = GetEmail(i);
                var newUser = new User
                {
                    Username = email,
                    UserId = Guid.NewGuid(),
                    Email = email,
                    PasswordHash = GetPasswordHash()
                };
                if (!await dbAccess.InsertUser(newUser, conn))
                {
                    throw new InvalidOperationException("Db seeding failed to create user entry");
                }

                idCache.Add(newUser.UserId);
            }

            return idCache;
        }

        private static string GetPasswordHash() => "219031209481029348";

        private static string GetEmail(int i) => $"alex{i}@skynet.res";

        public static Task<int> Cleanup(IDbConnection conn, DapperDemoDbAccess dbAccess)
        {
            return dbAccess.DeleteAllUsers(conn);
        }
    }
}