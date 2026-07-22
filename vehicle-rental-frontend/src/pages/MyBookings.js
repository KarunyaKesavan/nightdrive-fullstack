import React, { useEffect, useState } from "react";
import { bookingApi, vehicleApi, notificationApi } from "../services/api";
import { useAuth } from "../context/AuthContext";

const statusClass = {
  Confirmed: "badge-available",
  Completed: "badge-available",
  Cancelled: "badge-booked",
};

export default function MyBookings() {
  const { user } = useAuth();
  const [bookings, setBookings] = useState([]);
  const [vehicles, setVehicles] = useState({});
  const [notifications, setNotifications] = useState([]);

  useEffect(() => {
    bookingApi.listForCustomer(user.id).then(async (list) => {
      setBookings(list);
      const map = {};
      for (const b of list) {
        map[b.vehicleId] = await vehicleApi.get(b.vehicleId);
      }
      setVehicles(map);
    });
    notificationApi.listForCustomer(user.id).then(setNotifications);
  }, [user.id]);

  const cancelBooking = async (id) => {
    await bookingApi.cancel(id);
    setBookings((prev) => prev.map((b) => (b.bookingId === id ? { ...b, status: "Cancelled" } : b)));
  };

  return (
    <div className="container page">
      <h1 className="display">My bookings</h1>

      {notifications.length > 0 && (
        <section className="section">
          <h2>Notifications</h2>
          {notifications.map((n) => (
            <div key={n.notificationId} className={`card notif-item ${n.read ? "" : "unread"}`}>
              {n.message} <span className="muted small">— {n.date}</span>
            </div>
          ))}
        </section>
      )}

      <section className="section">
        {bookings.length === 0 && <p className="muted">You have no bookings yet.</p>}
        {bookings.map((b) => {
          const v = vehicles[b.vehicleId];
          return (
            <div key={b.bookingId} className="card booking-row">
              <div>
                <strong>{v ? v.name : "Loading…"}</strong>
                <p className="muted small">
                  {b.startDate} → {b.endDate}
                </p>
              </div>
              <span className={`badge ${statusClass[b.status] || ""}`}>{b.status}</span>
              <span>₹{b.totalAmount.toLocaleString("en-IN")}</span>
              {b.status === "Confirmed" && (
                <button className="btn btn-danger" onClick={() => cancelBooking(b.bookingId)}>
                  Cancel
                </button>
              )}
            </div>
          );
        })}
      </section>
    </div>
  );
}
