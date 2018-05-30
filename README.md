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

### Dapper extensions
//TODO: