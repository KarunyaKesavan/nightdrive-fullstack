"""
Content-based vehicle recommendations.

For each customer, looks at the vehicle types/brands they've booked or
reviewed positively, then scores the remaining catalogue by similarity
(shared type, brand, and a price-closeness bonus). Falls back to
"trending" (highest-rated Available vehicles) for customers with no
history — mirrors the two reasons already hard-coded in the frontend
mock ("Matches your preference for..." / "Trending ... in your city").
"""
import pandas as pd


def generate_recommendations_for_customer(conn, customer_id: int, top_n: int = 3):
    with conn.cursor() as cur:
        cur.execute("SELECT * FROM Vehicle")
        vehicles = pd.DataFrame(cur.fetchall())
        if vehicles.empty:
            return []

        cur.execute(
            "SELECT VehicleId FROM Booking WHERE CustomerId = %s "
            "UNION SELECT VehicleId FROM Review WHERE CustomerId = %s AND Rating >= 4",
            (customer_id, customer_id),
        )
        liked_ids = [r["VehicleId"] for r in cur.fetchall()]

    candidates = vehicles[vehicles["Status"] == "Available"].copy()

    if not liked_ids:
        # No history yet: recommend the highest-rated available vehicles.
        top = candidates.sort_values("Rating", ascending=False).head(top_n)
        return [
            {"vehicleId": int(row.VehicleId), "reason": f"Trending {row.Type} on NightDrive"}
            for row in top.itertuples()
        ]

    liked = vehicles[vehicles["VehicleId"].isin(liked_ids)]
    preferred_types = set(liked["Type"])
    preferred_brands = set(liked["Brand"])
    avg_price = liked["PricePerDay"].astype(float).mean()

    candidates = candidates[~candidates["VehicleId"].isin(liked_ids)]
    if candidates.empty:
        return []

    def score(row):
        s = 0.0
        if row.Type in preferred_types:
            s += 2.0
        if row.Brand in preferred_brands:
            s += 1.5
        price_gap = abs(float(row.PricePerDay) - avg_price) / max(avg_price, 1)
        s += max(0.0, 1.0 - price_gap)
        s += float(row.Rating) * 0.2
        return s

    candidates["score"] = candidates.apply(score, axis=1)
    top = candidates.sort_values("score", ascending=False).head(top_n)

    results = []
    for row in top.itertuples():
        if row.Type in preferred_types:
            reason = f"Matches your preference for {row.Type.lower()} vehicles"
        elif row.Brand in preferred_brands:
            reason = f"You've liked {row.Brand} before"
        else:
            reason = f"Popular {row.Type.lower()} pick near you"
        results.append({"vehicleId": int(row.VehicleId), "reason": reason})
    return results


def write_recommendations(conn, customer_id: int):
    recs = generate_recommendations_for_customer(conn, customer_id)
    with conn.cursor() as cur:
        cur.execute("DELETE FROM Recommendation WHERE CustomerId = %s", (customer_id,))
        for r in recs:
            cur.execute(
                "INSERT INTO Recommendation (CustomerId, VehicleId, Reason) VALUES (%s, %s, %s)",
                (customer_id, r["vehicleId"], r["reason"]),
            )
    return recs
