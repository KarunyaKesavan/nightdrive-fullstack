# NightDrive API (ASP.NET Core 8)

Matches the frontend's `src/services/api.js` route-for-route and field-for-field
(camelCase JSON, same property names as the old mock data), so switching
`USE_MOCK` to `false` in the frontend works against this with no other changes.

## Requirements

- .NET 8 SDK
- MySQL 8.x running locally

## Setup

1. Create the database schema:
   ```bash
   mysql -u root -p < schema.sql
   ```
2. Edit `appsettings.json` — set your real MySQL password in
   `ConnectionStrings:Default`, and change `Jwt:Secret` to your own random
   string (32+ characters) before deploying anywhere beyond your laptop.
3. Run it:
   ```bash
   dotnet restore
   dotnet run
   ```
   Starts on **http://localhost:5000** (see `Properties/launchSettings.json`).

On first run, `Seed/DevSeeder.cs` populates the database with the same
locations/vehicles/bookings/reviews the frontend used to mock, plus:

- Admin login: `admin@nightdrive.com` / `password123`
- Demo customer login: `arun@example.com` / `password123`

(Passwords are hashed with BCrypt at seed time — nothing is stored in plain
text or hardcoded as a hash in SQL.)

New accounts can also be created via `/api/auth/register` with a `role` of
`"Customer"` or `"Admin"` — the frontend's signup page has a toggle for this.

## Endpoints

| Method | Route                          | Auth        |
|--------|---------------------------------|-------------|
| POST   | /api/auth/login                 | —           |
| POST   | /api/auth/register (role: Customer/Admin) | — |
| GET    | /api/vehicles                   | —           |
| GET    | /api/vehicles/{id}               | —           |
| POST   | /api/vehicles                    | Admin       |
| PUT    | /api/vehicles/{id}                | Admin       |
| DELETE | /api/vehicles/{id}                | Admin       |
| GET    | /api/vehicles/{id}/location        | —           |
| GET    | /api/locations                   | —           |
| GET    | /api/bookings/customer/{id}       | logged in   |
| GET    | /api/bookings                    | Admin       |
| POST   | /api/bookings                    | logged in   |
| POST   | /api/bookings/{id}/cancel          | logged in   |
| POST   | /api/payments                    | logged in   |
| GET    | /api/reviews/vehicle/{id}          | —           |
| POST   | /api/reviews                     | logged in   |
| GET    | /api/notifications/customer/{id}   | logged in   |
| GET    | /api/ai/recommendations          | reads JWT if present |
| GET    | /api/ai/demand-forecast          | —           |
| GET    | /api/admin/stats                 | Admin       |
| GET    | /api/admin/payments               | Admin       |

`/api/ai/*` reads from the `Recommendation` and `DemandForecast` tables —
it doesn't call the Python service directly. The AI service writes into
those tables on its own schedule (see `../vehicle-rental-ai/README.md`).

`/api/vehicles/{id}/location` is fed by `Services/VehicleTrackingSimulator.cs`,
a background service that nudges each `Booked` vehicle's coordinates every 5
seconds (a mock GPS feed, starting from its pickup location) — that's what the
frontend's live-tracking map polls and draws with Leaflet/OpenStreetMap.

## Security note

Letting anyone self-register as `"Admin"` via `/api/auth/register` is fine
for a training/demo project like this one, but not something you'd ship to
production — normally admin accounts are provisioned separately (invite-only,
or created directly in the DB) rather than being a public signup option.

## Not yet verified

This code was written and cross-checked field-by-field against the
frontend's mock shapes, but this sandbox has no .NET SDK or network
access, so it has **not been compiled or run**. Run `dotnet build` first
and fix any straggling errors before relying on it.
