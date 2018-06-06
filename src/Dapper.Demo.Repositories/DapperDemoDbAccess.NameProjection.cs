namespace Dapper.Demo.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;

    public class NameProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public partial class DapperDemoDbAccess
    {
        private const string SqlUserNameProjectionGetById = "SELECT UserId as Id, Username as Name FROM Users";

        public Task<IEnumerable<NameProjection>> GetAllNameProjections(IDbConnection connection, IDbTransaction transaction = null)
        {
            return connection.QueryAsync<NameProjection>(SqlUserNameProjectionGetById, transaction: transaction);
        }
    }

}