# Dapper.Demo
Small showcase on a possible way to use Dapper ORM in .net core projects

## What is Dapper
Dapper is a MicroORM which allows to easily query databases and map response to the CLR types.
Dapper focuses on the simplicity and performance, hence it provide only a limited set of features out of the box. 

## Initial project setup

1. Create initial projects structure
``` 
mkdir src
cd src
dotnet new sln --name Dapper.Demo
dotnet new classlib --name Dapper.Demo.Repositories --framework netstandard2.0
dotnet new xunit --name Dapper.Demo.Test.Unit

dotnet sln .\Dapper.Demo.sln add .\Dapper.Demo.Repositories\Dapper.Demo.Repositories.csproj
dotnet sln .\Dapper.Demo.sln add .\Dapper.Demo.Test.Unit\Dapper.Demo.Test.Unit.csproj

dotnet add .\Dapper.Demo.Repositories\Dapper.Demo.Repositories.csproj package Dapper
dotnet add .\Dapper.Demo.Test.Unit\Dapper.Demo.Test.Unit.csproj reference .\Dapper.Demo.Repositories\Dapper.Demo.Repositories.csproj
dotnet add .\Dapper.Demo.Test.Unit\Dapper.Demo.Test.Unit.csproj package Dapper
dotnet add .\Dapper.Demo.Test.Unit\Dapper.Demo.Test.Unit.csproj package FluentAssertions
``` 

2. (OPTIONAL) Create build file to automate build and tests process
Create a file named build.ps1 in your root directory with the following content:
``` powershell
Param(
    [ValidateNotNullOrEmpty()]
    [string]$Target="Default",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration="Release",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("linux-x64", "win-x64")]
    [string]$Runtime="win-x64"
)

$startDir=Get-Location
$buildDir=$PSScriptRoot
$solutionDir=$buildDir
$srcDir=[System.IO.Path]::Combine($solutionDir, "src")

Write-Host -ForegroundColor Green "*** Building $Configuration in $solutionDir"

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "*** Build"
dotnet build "$srcDir\Dapper.Demo.sln" --configuration $Configuration

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "***** Unit tests"
dotnet test "$srcDir\Dapper.Demo.Test.Unit\Dapper.Demo.Test.Unit.csproj" --configuration $Configuration

Set-Location $startDir
```

3. Create working models
In your Dapper.Demo.Repositories project create some basic models to test against:
``` csharp
    public class Role
    {
        public Guid RoleId { get; set; } = Guid.NewGuid();
        public string Type { get; set; }
    }

    public class User
    {
        public Guid UserId { get; set; } = Guid.NewGuid();

        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public DateTime? DeactivatedOn { get; set; }
        public DateTime? GDPRSignedOn { get; set; }

        public List<Role> Roles { get; set; } = new List<Role>();
    }

    public class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
```

## Basic usage scenarios

### CRUD (using Vanilla Dapper)
Dapper is basically a set of extension methods that operate on the `IDbConnection`.
The connection can be MsSqlServer, MySql, SQLite or any other supported DB driver.
The basic usage scenario for simple Insert/Update/Select/Delete is as follows:
NOTE: All examples can be found in `Dapper.Demo.Test.Unit` project in a file `DapperDemoTests`

1. Create SQL statement to be executed:
    `public const string SqlUserInsert = "INSERT INTO [dbo].[Users]([UserId],[Username],[Email],[PasswordHash],[DeactivatedOn],[GDPRSignedOn]) VALUES(@UserId, @Username, @Email, @PasswordHash, @DeactivatedOn, @GDPRSignedOn)";`
    (NOTE: dapper recognizes @-prefixed parameters as query arguments which will be derived from the object that is passed to the subsequent call)
2. Instantiate IDbConnection to operate on
    ``` csharp
        var dbConnection = new SqlConnection(ConnString);
        dbConnection.Open();
    ```
3. Call Execute or Query extension method on the opened connection:
    ``` csharp
        dbConnection.Execute(SqlUserInsert, new User{});
    ```

Insert/Update/Delete functionality can be achieved by calling `Execute` method as described above
Select functionality can be achieved by calling a generic method Query<T> or QuerySingle<T> as follows:
``` csharp
    public const string SqlUserGetById = "SELECT * FROM Users WHERE UserId = @UserId";
    var user = arrangements.DbConnection.QuerySingle<User>(SqlUserGetById, new {UserId = "<GUID>"});
```

### Updating multiple entries
As a MicroORM Dapper doesn't provide any _smart_ functionality to perform updates for multiple entries.
For scenarios when multiple entries need to be updated in the same way a simple query like `UPDATE Users SET Email='--' WHERE GDPRAccepted IS NULL` can be executed on an `IDbConnection`

For scenarios when a collection of objects needs to be updated Dapper supports passing a list of objects as an argument to an update call. This only means that Dapper will iterate over the collection for you:
``` csharp
    const string SqlRoleInsert = "INSERT INTO Roles(RoleId,[Type]) VALUES(@RoleId, @Type)";
    var roles = new List<Roles>(){new Role(), new Role()};
    dbConnection.Execute(SqlRoleInsert, roles);
```

Dapper doesn't provide any nested entities tracking information, so if the collection consists of nested elements that span information across several tables then the update has to be performed manually. 

Dapper also doesn't track which fields were changed and it's implementors responsibility to choose how to generate update SQL query (e.g. for only specific fields, for all fields or somehow identofy which fields were changed and implement dynamic query generation)

### Support for transaction scope
As Dapper is based on ADO.NET it's transaction management is quite straightforward. The Execute/Query methods have a parameter called `transaction` that can be passed along with the call. This parameter is a simple ADO.NET transaction opened on the connection:

``` csharp
    using (var transaction = dbConnection.BeginTransaction())
    {
        dbConnection.Execute(SqlRoleInsert, new Role(), transaction);
        dbConnection.Execute(SqlUserRoleInsert, new UserRole(), transaction);
        transaction.Commit(); 
    } //transaction will automatically rollback if not committed removing any operation performed in transaction scope
```

_NOTE: .NET `TransactionScope` feature that allows creating a distributed transaction that can span across multiple actors is also natively supported as it is implemented at the ADO.NET level. This functionality however is Windows-only (uses MSDTS service), hence wasn't explored as part of this research_

### How do we do functions like Compare-and-swap?
Dapper doesn't provide any optimistic concurrency support by default. If such feature is needed for specific tables then it would require introducing `rowversion` field to the table and a field to the type accordingly. Sql queries would need to be carefully crafted to respect this field.

### How easy is it to test?
It hugely depends on the type of testing that needs to be performed. In case if Vanilla dapper is used and IDbConnection factory is injected as a main entry point to perform DB operations then there the following solutions are possible:

- Setting up a test DB instance (see unit tests in the repository)
  This however pushes type of tests to rather be integration than unit. This also has compelxity of setting up and seeding the DB with test values
- Using SQLite In-Memory driver to isntantiate IDbConnection
  Slightly less integration like option, but may have it's own problems considering that SQLite dialect may have it's own specifics that may affect tests results

  However, it is unlikely that in a reasonably production focused solution Dapper will be used directly as a IDbConnection, it is more likely that some sort of Unit Of Work/Repository patter will be implemented and in such case testing is a matter of mocking/stubbing of relevant abstractions



### Migrations
Dapper doesn't provide any functionality to support database migrations. If using Dapper a completely ad-hoc process would need to be introduced to support DB versioning and applying of changes.
Such solution would probably require a table that keeps track of all changes that were applied to the database and all newly introduced changes to the DB schema would need to be wrapped in some sort of versioning template.

### Performance
Performance was not tested as a part of this excercise.
Existing reports on the internet vary on the method of how benchmarking was done but overall show Dapper to be one of the fastest ORMs available for .net.
Some interesting benchmark results can be found here:

1. [https://weblogs.asp.net/fbouma/net-micro-orm-fetch-benchmark-results-and-the-fine-details]
2. [https://msit.powerbi.com/view?r=eyJrIjoiYTZjMTk3YjEtMzQ3Yi00NTI5LTg5ZDItNmUyMGRlOTkwMGRlIiwidCI6IjcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0NyIsImMiOjV9]
3. [https://dev.to/rickab10/is-entity-framework-core-20-faster]
4. [https://koukia.ca/entity-framework-core-2-0-vs-dapper-net-performance-benchmark-querying-sql-azure-tables-7696e8e3ed28]

In overall these benchmarks show that Dapper is only a bit faster than EF Core implementation in comparable scenarios, EF Core with tracking is slower for obvious reasons.

### Benchmarking
As a part of this investigation a very simple set of benchmarks was created using DotNetBenchmark and can be explored in the `Dapper.Demo.Benchmark` project folder of this repository.

The tests included the following scenarios:

1. Get one entity on new connection everytime
2. Get one entity on existing connection
3. Update an entity
4. Update an entity with precondition
5. Get a set of projections from one table on new connection
6. Get a set of projections from one table reusing existing connection

As part of this research task we have also created same (as much as possible) set of scenarios using EFCore 2.0 to be able to compare the performance. See this done here: [https://github.com/ape-box/PVR-EfcoreSpike]

#### Running benchmark on your machine
In order to run benchmarks on this machine please create a database named `dapper_bench` in your local SQL server. Then use `src\Dapper.Demo.Repositories\MigrationScript.sql ` file to scaffold the DB. 
Build the solution in release mode and then run: `dotnet src\Dapper.Demo.Benchmarks\bin\Release\netcoreapp2.0\Dapper.Demo.Benchmarks.dll`

#### Benchmarking results for Dapper


                            Method | NumerOfRowsToSeed |        Mean |      Error |     StdDev |      Median |
          GetOneUserWithNewContext |                10 |    241.2 us |   4.675 us |   4.801 us |    240.3 us |
  GetOneUserWithoutContextOverhead |                10 |    220.9 us |   4.405 us |   5.727 us |    221.0 us |
                          UpdateOk |                10 |    662.5 us |  13.210 us |  27.573 us |    655.6 us |
                        UpdateFail |                10 |  1,048.6 us |  20.485 us |  19.162 us |  1,047.0 us |
         GetAllNamesWithNewContext |                10 |    253.7 us |   5.070 us |  13.620 us |    249.6 us |
 GetAllNamesWithoutContextOverhead |                10 |    225.7 us |   4.388 us |   5.858 us |    225.2 us |
          GetOneUserWithNewContext |              1000 |    259.6 us |   5.160 us |  11.434 us |    259.0 us |
  GetOneUserWithoutContextOverhead |              1000 |    221.0 us |   3.353 us |   3.137 us |    220.7 us |
                          UpdateOk |              1000 |    635.2 us |  12.517 us |  20.913 us |    627.4 us |
                        UpdateFail |              1000 |  1,145.5 us |  40.312 us | 116.953 us |  1,186.4 us |
         GetAllNamesWithNewContext |              1000 |  1,278.7 us |  26.192 us |  36.718 us |  1,277.1 us |
 GetAllNamesWithoutContextOverhead |              1000 |  1,109.9 us |  28.704 us |  84.636 us |  1,116.9 us |
          GetOneUserWithNewContext |             10000 |    258.4 us |   5.148 us |  12.234 us |    258.3 us |
  GetOneUserWithoutContextOverhead |             10000 |    234.4 us |   4.590 us |   8.622 us |    234.4 us |
                          UpdateOk |             10000 |    699.4 us |  13.840 us |  33.159 us |    694.2 us |
                        UpdateFail |             10000 |  1,076.2 us |  21.489 us |  56.232 us |  1,078.1 us |
         GetAllNamesWithNewContext |             10000 | 12,898.7 us | 242.285 us | 483.869 us | 12,833.2 us |
 GetAllNamesWithoutContextOverhead |             10000 | 12,613.1 us | 249.749 us | 475.173 us | 12,476.6 us |



#### Benchmarking results for EFCore 2.0 (for comparison)

                            Method | NumerOfRowsToSeed |        Mean |      Error |     StdDev |      Median |
          GetOneUserWithNewContext |                10 |    466.7 us |   9.314 us |  12.433 us |    465.1 us |
  GetOneUserWithoutContextOverhead |                10 |    358.1 us |   7.089 us |   6.631 us |    359.8 us |
                          UpdateOk |                10 |    539.2 us |  11.378 us |  24.000 us |    535.5 us |
                        UpdateFail |                10 |  2,887.6 us |  51.197 us |  42.752 us |  2,873.1 us |
         GetAllNamesWithNewContext |                10 |    437.0 us |   7.709 us |   6.834 us |    436.3 us |
 GetAllNamesWithoutContextOverhead |                10 |    324.0 us |   7.525 us |   7.728 us |    324.1 us |
          GetOneUserWithNewContext |              1000 |    462.5 us |  10.102 us |  10.809 us |    461.7 us |
  GetOneUserWithoutContextOverhead |              1000 |    369.8 us |   7.844 us |  14.539 us |    370.2 us |
                          UpdateOk |              1000 |    524.0 us |  14.638 us |  42.931 us |    510.4 us |
                        UpdateFail |              1000 |  3,169.4 us |  62.226 us | 140.454 us |  3,159.5 us |
         GetAllNamesWithNewContext |              1000 |  3,638.7 us |  69.531 us |  74.397 us |  3,633.1 us |
 GetAllNamesWithoutContextOverhead |              1000 |  3,567.4 us |  45.451 us |  42.515 us |  3,576.9 us |
          GetOneUserWithNewContext |             10000 |    471.2 us |   9.171 us |  12.553 us |    470.4 us |
  GetOneUserWithoutContextOverhead |             10000 |    346.7 us |   6.740 us |  11.074 us |    345.3 us |
                          UpdateOk |             10000 |    497.2 us |   9.827 us |  24.291 us |    490.8 us |
                        UpdateFail |             10000 |  2,764.8 us |  56.332 us |  57.849 us |  2,757.0 us |
         GetAllNamesWithNewContext |             10000 | 39,172.4 us | 765.074 us | 967.574 us | 39,169.2 us |
 GetAllNamesWithoutContextOverhead |             10000 | 37,099.5 us | 672.288 us | 595.966 us | 36,985.8 us |



#### Benchmarks analysis
As we can see Dapper has slightly better performance in almost all the cases even considering that EF was loading data in detached mode. 

NOTE: UpdateOk test gives different results because the implementation of update not only performs the update itself but also propagates updated ConcurencyToken back to the updated entity. (This can be removed in future to get cleaner results)

We can also see that Dapper performs way faster when querying large datasets which has been known Achilles heel of EF for years and we can see it remains one of the weak points of Core version of the framework still