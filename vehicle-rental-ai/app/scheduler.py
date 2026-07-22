from apscheduler.schedulers.background import BackgroundScheduler
from app.db import get_connection
from app.recommend import write_recommendations
from app.demand_forecast import write_demand_forecast
from app.maintenance import write_maintenance_predictions

_scheduler = None


def run_all_jobs():
    conn = get_connection()
    try:
        with conn.cursor() as cur:
            cur.execute("SELECT CustomerId FROM Customer")
            customer_ids = [r["CustomerId"] for r in cur.fetchall()]
        for cid in customer_ids:
            write_recommendations(conn, cid)
        write_demand_forecast(conn)
        write_maintenance_predictions(conn)
    finally:
        conn.close()


def start_scheduler():
    global _scheduler
    if _scheduler is not None:
        return
    run_all_jobs()  # run once immediately on startup
    _scheduler = BackgroundScheduler()
    _scheduler.add_job(run_all_jobs, "interval", hours=24, id="nightly_ai_jobs")
    _scheduler.start()
