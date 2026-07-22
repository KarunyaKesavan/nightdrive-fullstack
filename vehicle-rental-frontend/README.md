# NightDrive — AI-Powered Vehicle Rental (Frontend)

Create React App (uses `react-scripts`, runs with `npm start`).

## Run it

```bash
npm install
npm start
```

Opens at http://localhost:3000. Needs the backend running at
http://localhost:5000 first (see `../vehicle-rental-backend/README.md`) —
`src/services/api.js` has `USE_MOCK = false` and points at it.

Flip `USE_MOCK` back to `true` in that file if you want to click around
the UI with in-memory fake data and no backend at all (handy for quick UI
iteration).

## Features

- **Role-based signup** — the Register page has a Customer/Admin toggle.
  Admins list vehicles and see what's been collected; customers browse and book.
- **Booking + mock payment** — pick dates on a vehicle, book it, and it's
  charged through the mock payment flow (`paymentApi.pay`), recorded as a
  real `Payment` row the admin dashboard can see.
- **Reviews & ratings** — post a review on any vehicle; the star rating shown
  everywhere for that vehicle is recomputed from the average of all its reviews.
- **Live tracking** — while a vehicle's status is `Booked`, its detail page
  shows a Leaflet/OpenStreetMap map that polls `/vehicles/{id}/location`
  every 5 seconds and moves the marker (backed by a mock GPS simulator on
  the backend).
- **Admin dashboard** — fleet stats, AI demand forecast, a form to add new
  vehicles to the fleet, per-vehicle status control + remove, and a table of
  every payment collected.

## Structure

```
src/
  components/   Navbar, VehicleCard, FilterBar, LiveTrackingMap
  context/      AuthContext (current user)
  pages/        Home, VehicleDetail, Login, Register, MyBookings, AdminDashboard
  services/     api.js (mock/real API switch — one flag, USE_MOCK)
  styles/       theme.css (Night Drive dark theme)
```

## Logins

- Admin (seeded): `admin@nightdrive.com` / `password123`
- Demo customer (seeded): `arun@example.com` / `password123`
- Or sign up fresh as either role from the Register page.
