namespace Dapper.Demo.Benchmarks
{
    using BenchmarkDotNet.Attributes;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading.Tasks;
    using DapperDemoDbAccess = Repositories.DapperDemoDbAccess;

    public class TestSuite
    {
        private int cachePointer;
        private List<Guid> idCache;
        private DapperDemoDbAccess db;

        private Guid GetNextIdSequentially()
            => cachePointer < idCache.Count
               ? idCache[cachePointer++]
               : idCache[cachePointer = 0];

        private IDbConnection CreateConnection()
        {
            var conn = new SqlConnection("Server=(local);Initial Catalog=dapper_bench;Integrated Security=True");
            conn.Open();
            return conn;
        }

        [Params(10, 1000, 10000)]
        //[Params(1)]
        public int NumerOfRowsToSeed;

        private IDbConnection globalConn;
        private Random rand;

        [GlobalSetup]
        public void Setup()
        {
            db = new DapperDemoDbAccess();
            globalConn = CreateConnection();
            rand = new Random((int)DateTime.Now.Ticks);
            SeedDb();
        }

        private void SeedDb()
        {
            idCache = DbHelper.SeedDb(globalConn, db, NumerOfRowsToSeed).Result;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            var _ = DbHelper.Cleanup(globalConn, db).Result;
            globalConn.Dispose();
            idCache?.Clear();

        }

        [Benchmark]
        public async Task GetOneUserWithNewContext()
        {
            using (var conn = CreateConnection())
            {
                var id = GetNextIdSequentially();
                var user = await db.GetUserById(id, conn);
                if (user == null) throw new InvalidOperationException("Get failed");
            }
        }

        [Benchmark]
        public async Task GetOneUserWithoutContextOverhead()
        {
            var id = GetNextIdSequentially();
            var user = await db.GetUserById(id, globalConn);
            if (user == null) throw new InvalidOperationException("Get failed");
        }

        [Benchmark]
        public async Task UpdateOk()
        {
            using (var conn = CreateConnection())
            {
                var id = GetNextIdSequentially();
                var u = await db.GetUserById(id, conn);
                u.Username = "Johnny";

                var success = await db.UpdateUser(u, conn);
                if (!success)
                {
                    throw new InvalidOperationException("Benchmark failed");
                }
            }
        }

        [Benchmark]
        public async Task UpdateFail()
        {
            try
            {
                using (var conn1 = CreateConnection())
                using (var conn2 = CreateConnection())
                {
                    var id = GetNextIdSequentially();

                    var u1 = await db.GetUserById(id, conn1);
                    var u2 = await db.GetUserById(id, conn2);

                    var success = await db.UpdateUsernameConcurrent(u2, rand.Next(1000000).ToString(), conn2);
                    if (!success)
                    {
                        throw new Exception("Operation failed");
                    }

                    var success2 = await db.UpdateUsernameConcurrent(u1, rand.Next(1000000).ToString(), conn1);
                    if (!success2)
                    {
                        throw new InvalidOperationException("Concurrency check exception");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new Exception("Concurrency check didn't work");
        }

        [Benchmark]
        public async Task GetAllNamesWithNewContext()
        {
            using (var conn = CreateConnection())
            {
                var result = await db.GetAllNameProjections(conn);
                if (result == null) throw new InvalidOperationException("Get all failed");
            }
        }

        [Benchmark]
        public async Task GetAllNamesWithoutContextOverhead()
        {
            var result = await db.GetAllNameProjections(globalConn);
            if (result == null) throw new InvalidOperationException("Get all failed");
        }

    }
}