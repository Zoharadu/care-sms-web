<p align="center">
  <img src="frontend/public/care-sms-logo.png" alt="CareSMS" width="260">
</p>

# CareSMS

CareSMS is a full-stack demo application for configuring patient SMS journeys.
The interface is RTL-first and supports message templates, clinical delivery
rules, unit assignments, and controlled test sends.

## Repository structure

```text
frontend/          Angular 19 web application
backend/           .NET 8 API and automated tests
backend/Database/  Demo database creation and verification scripts
```

Each application also includes its own README with component-specific setup
and development notes.

## Prerequisites

- Node.js and npm
- .NET 8 SDK
- SQL Server LocalDB

## Quick start on Windows

### 1. Create the demo database

```powershell
cd backend
.\Database\Initialize-LocalDatabase.ps1
```

The initializer creates a local database named `HospitalSms`. It deliberately
stops if that database already exists and never drops or overwrites it.

### 2. Start the API

From the repository root:

```powershell
dotnet run --project .\backend\HospitalSms.Admin.Api\HospitalSms.Admin.Api.csproj --launch-profile HospitalSms.Admin.Api
```

The local HTTP endpoint is `http://localhost:57979`.

### 3. Start the frontend

In a second terminal:

```powershell
cd frontend
npm ci
npm start
```

Open <http://localhost:4200>. The Angular development proxy forwards `/api/`
requests to the local API.

## Validation

Run the frontend checks:

```powershell
cd frontend
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

Run the backend tests:

```powershell
dotnet test .\backend\HospitalSms.sln
```

## Demo-data notice

The demo database contains fictional sample records and reserved demo phone
numbers. It contains no patient information, real credentials, production
service URLs, or affiliation claims.

## Security

Do not commit credentials, patient information, database files, certificates,
or machine-specific configuration. The repository `.gitignore` files exclude
common local secrets, build output, dependency folders, and SQL Server data and
backup files.
