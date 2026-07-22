"""
Weekly demand forecast per (vehicle type, city).

Trains a small RandomForestRegressor on booking history (week number,
type, city -> booking count). With too little history to train on
(cold start, which is the normal case for a fresh install), falls back
to a rule-based estimate from the current fleet size and type mix, so
the endpoint always returns something sensible.
"""
from datetime import date
import pandas as pd
from sklearn.ensemble import RandomForestRegressor

MIN_ROWS_TO_TRAIN = 15


def _current_iso_week():
    y, w, _ = date.today().isocalendar()
    return f"{y}-W{w:02d}"


def _load_booking_history(conn):
    with conn.cursor() as cur:
        cur.execute(
            """
            SELECT v.Type AS type, l.City AS city, b.StartDate AS startDate
            FROM Booking b
            JOIN Vehicle v ON v.VehicleId = b.VehicleId
            JOIN Location l ON l.LocationId = b.PickupLocationId
            """
        )
        rows = cur.fetchall()
    return pd.DataFrame(rows)


def _load_fleet_mix(conn):
    with conn.cursor() as cur:
        cur.execute(
            """
            SELECT v.Type AS type, l.City AS city, COUNT(*) AS fleetCount
            FROM Vehicle v JOIN Location l ON l.LocationId = v.LocationId
            GROUP BY v.Type, l.City
            """
        )
        return pd.DataFrame(cur.fetchall())


def generate_demand_forecast(conn):
    history = _load_booking_history(conn)
    fleet = _load_fleet_mix(conn)
    week = _current_iso_week()

    if fleet.empty:
        return []

    if len(history) >= MIN_ROWS_TO_TRAIN:
        history["startDate"] = pd.to_datetime(history["startDate"])
        history["isoWeek"] = history["startDate"].dt.isocalendar().week
        grouped = (
            history.groupby(["type", "city", "isoWeek"]).size().reset_index(name="bookings")
        )
        X = pd.get_dummies(grouped[["type", "city", "isoWeek"]], columns=["type", "city"])
        y = grouped["bookings"]
        model = RandomForestRegressor(n_estimators=100, random_state=42)
        model.fit(X, y)

        current_week_num = date.today().isocalendar()[1]
        combos = fleet[["type", "city"]].drop_duplicates()
        combos["isoWeek"] = current_week_num
        Xp = pd.get_dummies(combos[["type", "city", "isoWeek"]], columns=["type", "city"])
        Xp = Xp.reindex(columns=X.columns, fill_value=0)
        combos["predicted"] = model.predict(Xp).round().astype(int).clip(min=0)
        return [
            {"type": r.type, "city": r.city, "week": week, "predictedDemand": int(r.predicted)}
            for r in combos.itertuples()
        ]

    # Cold start fallback: rough estimate scaled off fleet size per (type, city).
    fleet["predictedDemand"] = (fleet["fleetCount"] * 4).clip(lower=3)
    return [
        {"type": r.type, "city": r.city, "week": week, "predictedDemand": int(r.predictedDemand)}
        for r in fleet.itertuples()
    ]


def write_demand_forecast(conn):
    forecast = generate_demand_forecast(conn)
    week = _current_iso_week()
    with conn.cursor() as cur:
        cur.execute("DELETE FROM DemandForecast WHERE Week = %s", (week,))
        for f in forecast:
            cur.execute(
                "INSERT INTO DemandForecast (Type, City, Week, PredictedDemand) VALUES (%s, %s, %s, %s)",
                (f["type"], f["city"], f["week"], f["predictedDemand"]),
            )
    return forecast
