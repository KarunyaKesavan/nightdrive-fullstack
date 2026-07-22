"""
Predictive maintenance risk scoring.

Trains on completed bookings per vehicle (total rental days, booking
count) against whether that vehicle has since gone into 'Maintenance'
status, to estimate a risk score for the rest of the active fleet.

Falls back to a simple usage-based rule (more cumulative rental days
since last maintenance record = higher risk) when there isn't enough
label history yet.
"""

import pandas as pd
from sklearn.ensemble import RandomForestClassifier

MIN_ROWS_TO_TRAIN = 20


def _load_usage(conn):
    with conn.cursor() as cur:
        cur.execute(
            """
            SELECT
                v.VehicleId AS vehicleId,
                v.Status AS status,
                COUNT(b.BookingId) AS bookingCount,
                COALESCE(SUM(DATEDIFF(b.EndDate, b.StartDate)), 0) AS totalDays
            FROM Vehicle v
            LEFT JOIN Booking b
                ON b.VehicleId = v.VehicleId
                AND b.Status <> 'Cancelled'
            GROUP BY v.VehicleId, v.Status
            """
        )

        data = cur.fetchall()

    return pd.DataFrame(data)


def generate_maintenance_risk(conn):
    usage = _load_usage(conn)

    if usage.empty:
        return []

    # Convert database values to numeric (handles Decimal values from MySQL)
    usage["bookingCount"] = (
        pd.to_numeric(usage["bookingCount"], errors="coerce")
        .fillna(0)
        .astype(float)
    )

    usage["totalDays"] = (
        pd.to_numeric(usage["totalDays"], errors="coerce")
        .fillna(0)
        .astype(float)
    )

    usage["label"] = (usage["status"] == "Maintenance").astype(int)

    if len(usage) >= MIN_ROWS_TO_TRAIN and usage["label"].nunique() > 1:
        X = usage[["bookingCount", "totalDays"]]
        y = usage["label"]

        model = RandomForestClassifier(
            n_estimators=100,
            random_state=42
        )

        model.fit(X, y)

        usage["risk"] = model.predict_proba(X)[:, 1]

    else:
        # Rule-based fallback
        max_days = max(float(usage["totalDays"].max()), 1.0)

        usage["risk"] = (
            (
                usage["totalDays"] / max_days
            ) * 0.7
            +
            (
                usage["bookingCount"] * 0.05
            )
        ).clip(upper=0.95)

    active = usage[usage["status"] != "Maintenance"]

    flagged = active[
        active["risk"] >= 0.40
    ].sort_values(
        by="risk",
        ascending=False
    )

    predictions = []

    for row in flagged.itertuples():
        predictions.append(
            {
                "vehicleId": int(row.vehicleId),
                "predictedIssue": "Scheduled service recommended — high recent usage",
                "riskScore": round(float(row.risk), 2),
            }
        )

    return predictions


def write_maintenance_predictions(conn):
    predictions = generate_maintenance_risk(conn)

    with conn.cursor() as cur:
        for prediction in predictions:
            cur.execute(
                """
                INSERT INTO Maintenance
                    (VehicleId, PredictedIssue, RiskScore)
                VALUES
                    (%s, %s, %s)
                """,
                (
                    prediction["vehicleId"],
                    prediction["predictedIssue"],
                    prediction["riskScore"],
                ),
            )

    return predictions