# Hospital SMS Service

ASP.NET Core 8 API for managing clinical SMS projects, templates, routing rules,
and test-message delivery. The solution is organized into API, application,
domain, infrastructure, and test projects.

## Prerequisites

- .NET 8 SDK
- SQL Server

## Local configuration

Create the public demo database on Windows LocalDB:

~~~powershell
.\Database\Initialize-LocalDatabase.ps1
~~~

The committed appsettings.json points to that LocalDB database. See
Database/README.md for the schema, seed-data disclaimer, direct sqlcmd commands,
and instructions for another SQL Server instance.

Never commit database credentials, private server names, production origins, or
publish profiles. Local appsettings overrides and publish output are ignored.

## Run

~~~powershell
dotnet restore
dotnet run --project HospitalSms.Admin.Api
~~~

The API's local URLs are defined in
HospitalSms.Admin.Api/Properties/launchSettings.json.

## Test

~~~powershell
dotnet test HospitalSms.sln
~~~
