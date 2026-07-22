from fastapi import FastAPI, HTTPException
from app.db import get_connection
from app.recommend import write_recommendations
from app.demand_forecast import write_demand_forecast
from app.maintenance import write_maintenance_predictions
from app.scheduler import start_scheduler

app = FastAPI(title="NightDrive AI Service")


@app.on_event("startup")
def on_startup():
    # Runs the three jobs once immediately, then on a daily schedule after that.
    start_scheduler()


@app.get("/health")
def health():
    return {"status": "ok"}


# These endpoints are for manual testing / an admin "recompute now" button.
# In normal operation the scheduler (app/scheduler.py) runs the same
# functions daily and writes straight into the DB, which is all the
# ASP.NET Core API reads from (GET /api/ai/recommendations, /demand-forecast).

@app.post("/jobs/recommendations/{customer_id}")
def run_recommendations(customer_id: int):
    conn = get_connection()
    try:
        return {"customerId": customer_id, "recommendations": write_recommendations(conn, customer_id)}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        conn.close()


@app.post("/jobs/demand-forecast")
def run_demand_forecast():
    conn = get_connection()
    try:
        return {"forecast": write_demand_forecast(conn)}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        conn.close()


@app.post("/jobs/maintenance")
def run_maintenance():
    conn = get_connection()
    try:
        return {"predictions": write_maintenance_predictions(conn)}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        conn.close()
