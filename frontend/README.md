<p align="center">
  <img src="public/care-sms-logo.png" alt="CareSMS" width="260">
</p>

# CareSMS Web

CareSMS is an RTL-first web application for configuring patient SMS journeys.
It provides a focused interface for managing message templates, clinical
delivery rules, unit assignments, and controlled test sends.

This repository contains the Angular frontend. It is designed to run with the
companion CareSMS API and its public demo database.

## Features

- Project and message-category selection.
- Multilingual SMS templates with live message preview.
- Clinical routing and delivery-rule management.
- Hospital-unit and category assignments.
- Placeholder and trigger catalog views.
- Controlled test sends to allow-listed demo phone numbers.
- Responsive, accessible, right-to-left user interface.

## Technology

- Angular 19
- TypeScript
- RxJS
- SCSS and Tailwind CSS
- Jasmine and Karma

## Prerequisites

- Node.js and npm
- The CareSMS API running locally
- The public demo database initialized by the API repository

## Local development

Install dependencies:

```powershell
npm ci
```

Start the development server:

```powershell
npm start
```

Open <http://localhost:4200>. Requests under `/api/` are forwarded by
`proxy.conf.json` to the local API at `http://localhost:57979`.

## Available scripts

| Command | Purpose |
| --- | --- |
| `npm start` | Start the Angular development server |
| `npm run build` | Create a development build |
| `npm run watch` | Rebuild automatically after source changes |
| `npm test` | Run the unit-test suite |

## Project structure

```text
public/                 Static assets and CareSMS branding
src/app/features/       Feature pages and UI components
src/app/api.service.ts  Typed API client
src/environments/       Runtime environment configuration
```

## Demo-data notice

The public demo environment contains fictional records that use healthcare
organization names only as illustrative examples. It contains no patient
information, production credentials, real recipient numbers, or affiliation
claims.

## Security

Do not commit credentials, private service URLs, patient information, or local
environment overrides. The frontend uses a relative `/api/` URL so deployment
configuration can remain outside the source tree.
