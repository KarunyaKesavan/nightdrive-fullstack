# NightDrive AI Service (FastAPI + Python)

Writes into the same MySQL database the .NET API reads from — it does
**not** get called directly by the frontend. Flow is:

```
Frontend  --HTTP-->  ASP.NET Core API  --reads-->  MySQL (Recommendation, DemandForecast, Maintenance)
                                                          ^
                                              this service writes here, on a schedule
```

## Requirements

- Python 3.10+
- The same MySQL database the backend uses (run the backend's `schema.sql`
  and let it seed data first, so there's something to train/recommend on)

## Setup

```bash
python -m venv venv
source venv/bin/activate        # Windows: venv\Scripts\activate
pip install -r requirements.txt
cp .env.example .env            # then edit .env with your real DB password
uvicorn app.main:app --reload --port 8000
```

On startup it immediately runs all three jobs once (so the DB has fresh
recommendations right away), then repeats every 24 hours in the background.

## What it computes

- **`app/recommend.py`** — content-based recommendations per customer
  (matches booking/review history against vehicle type, brand, and price;
  falls back to "trending" vehicles for new customers with no history).
  Writes to the `Recommendation` table.
- **`app/demand_forecast.py`** — weekly demand per (vehicle type, city).
  Trains a small RandomForest once there's ~15+ historical bookings;
  before that, falls back to a fleet-size-based estimate. Writes to
  `DemandForecast`.
- **`app/maintenance.py`** — flags vehicles at elevated breakdown/service
  risk based on cumulative rental days and booking count. Writes to
  `Maintenance` (not yet surfaced in the frontend UI — the table and job
  exist for when you add that screen).

## Manual endpoints (for testing, not used by the frontend)

- `GET /health`
- `POST /jobs/recommendations/{customer_id}`
- `POST /jobs/demand-forecast`
- `POST /jobs/maintenance`

## Not yet verified

Written and reasoned through carefully, but this sandbox has no network
access to `pip install` scikit-learn/pandas/etc. and no MySQL server to
test against, so it has **not actually been run**. Small pandas/sklearn
API mismatches are the most likely thing to need a fix on first run —
if `demand_forecast.py` or `maintenance.py` throw, check the `pd.get_dummies`
/ `reindex` column-alignment lines first, since those are the most
version-sensitive part.
