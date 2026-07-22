# NightDrive — full stack

Three pieces, run together:

```
vehicle-rental-frontend/   React (Create React App)  — npm start        → :3000
vehicle-rental-backend/    ASP.NET Core 8 Web API     — dotnet run       → :5000
vehicle-rental-ai/         FastAPI + scikit-learn      — uvicorn ...      → :8000
```

## Order to start things in

1. **MySQL** — create the DB: `mysql -u root -p < vehicle-rental-backend/schema.sql`
2. **Backend** — `cd vehicle-rental-backend`, edit `appsettings.json` with
   your real MySQL password, then `dotnet restore && dotnet run`. On first
   run it seeds demo data + an admin/customer login — see its README.
3. **AI service** — `cd vehicle-rental-ai`, set up the venv, `cp .env.example .env`
   and fill in your DB password, then `uvicorn app.main:app --reload --port 8000`.
   It writes recommendations/demand-forecast rows into the same DB the
   backend reads from.
4. **Frontend** — `cd vehicle-rental-frontend`, `npm install && npm start`.
   Already set to `USE_MOCK = false` pointing at `http://localhost:5000/api`.

Log in with `admin@nightdrive.com` / `password123` for the admin dashboard,
or `arun@example.com` / `password123` as a regular customer — both are
seeded automatically by the backend on first run. New accounts can also
sign up as either role from the frontend's Register page.

## Features

- Role-based signup (Customer / Admin)
- Admin: list vehicles for rental, manage fleet status, remove vehicles,
  see every payment collected
- Customer: browse/filter, book with a mock payment flow, leave reviews
  (ratings recompute live from all reviews on a vehicle)
- Live GPS tracking on the vehicle detail page (Leaflet + OpenStreetMap)
  while a vehicle is out on a booking — backed by a mock GPS simulator
- AI: content-based recommendations, demand forecasting, predictive
  maintenance risk scoring (Python/scikit-learn, writes into MySQL)

## If you already ran an older version of schema.sql

This update added `Payment` and `VehicleLocation` tables. Re-run
`mysql -u root -p < vehicle-rental-backend/schema.sql` — it drops and
recreates all tables from scratch, so any data from a previous run will
be lost. That's expected for a dev database; the backend reseeds demo
data automatically on next startup.

## Honesty about what's verified

I wrote all three projects in this session and cross-checked the JSON
field names, route paths, and data shapes between them by hand (frontend
`services/api.js` ↔ backend DTOs/controllers ↔ AI service's DB writes).
But this sandbox has **no .NET SDK, no MySQL server, and no network
access**, so none of it has actually been compiled, run, or hit with a
real request. Treat this as a careful first draft, not a tested build —
budget time for the usual first-run friction (a missing NuGet package
version, a pandas/sklearn quirk, a CORS typo) once you run it for real.
If something breaks, paste me the exact error and I'll fix it.
