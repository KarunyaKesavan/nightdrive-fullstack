import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { vehicleApi, locationApi, bookingApi, paymentApi, reviewApi } from "../services/api";
import { useAuth } from "../context/AuthContext";
import LiveTrackingMap from "../components/LiveTrackingMap";

export default function VehicleDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [vehicle, setVehicle] = useState(null);
  const [locations, setLocations] = useState([]);
  const [reviews, setReviews] = useState([]);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [status, setStatus] = useState(null);
  const [reviewText, setReviewText] = useState("");
  const [reviewRating, setReviewRating] = useState(5);

  useEffect(() => {
    vehicleApi.get(id).then(setVehicle);
    locationApi.list().then(setLocations);
    reviewApi.listForVehicle(id).then(setReviews);
  }, [id]);

  if (!vehicle) return <div className="container page">Loading…</div>;

  const location = locations.find((l) => l.locationId === vehicle.locationId);
  const days =
    startDate && endDate
      ? Math.max(1, Math.ceil((new Date(endDate) - new Date(startDate)) / 86400000))
      : 0;
  const total = days * vehicle.pricePerDay;

  const handleBook = async () => {
    if (!user) return navigate("/login");
    if (!startDate || !endDate) {
      setStatus({ type: "error", msg: "Pick both start and end dates." });
      return;
    }
    setStatus({ type: "loading", msg: "Processing booking…" });
    const booking = await bookingApi.create({
      customerId: user.id,
      vehicleId: vehicle.vehicleId,
      startDate,
      endDate,
      totalAmount: total,
      pickupLocationId: vehicle.locationId,
    });
    await paymentApi.pay({ bookingId: booking.bookingId, amount: total, method: "Mock Card" });
    setStatus({ type: "success", msg: `Booked! Confirmation #${booking.bookingId}` });
    vehicleApi.get(vehicle.vehicleId).then(setVehicle); // status flips to "Booked" — refresh so tracking map shows
  };

  const handleReview = async (e) => {
    e.preventDefault();
    if (!user || !reviewText.trim()) return;
    const r = await reviewApi.create({
      vehicleId: vehicle.vehicleId,
      customerId: user.id,
      rating: Number(reviewRating),
      comment: reviewText.trim(),
    });
    setReviews([...reviews, r]);
    setReviewText("");
    vehicleApi.get(vehicle.vehicleId).then(setVehicle); // rating is recomputed server-side from all reviews
  };

  return (
    <div className="container page">
      <button className="btn btn-ghost back-btn" onClick={() => navigate(-1)}>
        ← Back
      </button>

      <div className="detail-grid">
        <div className="card detail-main">
          <div className="detail-emoji">{vehicle.image}</div>
          <h1>{vehicle.name}</h1>
          <p className="muted">
            {vehicle.brand} · {vehicle.type} · {vehicle.transmission} · {vehicle.fuel} ·{" "}
            {vehicle.seats} seats
          </p>
          {location && (
            <p className="muted">
              📍 {location.area}, {location.city}
            </p>
          )}
          <div className="feature-list">
            {vehicle.features.map((f) => (
              <span key={f} className="badge badge-available">
                {f}
              </span>
            ))}
          </div>

          {vehicle.status === "Booked" && location && (
            <section className="section">
              <h2>Live tracking</h2>
              <LiveTrackingMap
                vehicleId={vehicle.vehicleId}
                fallbackLat={location.lat}
                fallbackLng={location.lng}
                vehicleName={vehicle.name}
              />
            </section>
          )}

          <section className="section">
            <h2>Reviews ({reviews.length})</h2>
            {reviews.length === 0 && <p className="muted">No reviews yet.</p>}
            {reviews.map((r) => (
              <div key={r.reviewId} className="card review-item">
                <strong>★ {r.rating}</strong>
                <p>{r.comment}</p>
                <span className="muted small">{r.date}</span>
              </div>
            ))}
            {user && (
              <form onSubmit={handleReview} className="review-form">
                <select value={reviewRating} onChange={(e) => setReviewRating(e.target.value)}>
                  {[5, 4, 3, 2, 1].map((n) => (
                    <option key={n} value={n}>
                      {n} star{n > 1 ? "s" : ""}
                    </option>
                  ))}
                </select>
                <input
                  placeholder="Share your experience…"
                  value={reviewText}
                  onChange={(e) => setReviewText(e.target.value)}
                />
                <button className="btn btn-primary" type="submit">
                  Post
                </button>
              </form>
            )}
          </section>
        </div>

        <div className="card booking-panel">
          <div className="price-big">
            ₹{vehicle.pricePerDay.toLocaleString("en-IN")} <small>/day</small>
          </div>
          <label>Pickup date</label>
          <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          <label>Return date</label>
          <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          {days > 0 && (
            <p className="muted">
              {days} day{days > 1 ? "s" : ""} × ₹{vehicle.pricePerDay} ={" "}
              <strong>₹{total.toLocaleString("en-IN")}</strong>
            </p>
          )}
          <button
            className="btn btn-primary full"
            disabled={vehicle.status !== "Available"}
            onClick={handleBook}
          >
            {vehicle.status === "Available" ? "Book now" : vehicle.status}
          </button>
          {status && <p className={`status-msg status-${status.type}`}>{status.msg}</p>}
        </div>
      </div>
    </div>
  );
}
