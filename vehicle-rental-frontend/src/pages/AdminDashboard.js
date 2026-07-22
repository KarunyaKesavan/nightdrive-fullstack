import React, { useEffect, useState } from "react";
import { adminApi, vehicleApi, aiApi, locationApi } from "../services/api";

const emptyVehicleForm = {
  name: "",
  type: "Car",
  brand: "",
  pricePerDay: "",
  seats: 5,
  transmission: "Automatic",
  fuel: "Petrol",
  rating: 4.5,
  status: "Available",
  locationId: "",
  image: "🚗",
  features: "",
};

export default function AdminDashboard() {
  const [stats, setStats] = useState(null);
  const [vehicles, setVehicles] = useState([]);
  const [forecast, setForecast] = useState([]);
  const [locations, setLocations] = useState([]);
  const [payments, setPayments] = useState([]);
  const [form, setForm] = useState(emptyVehicleForm);
  const [formStatus, setFormStatus] = useState(null);

  useEffect(() => {
    adminApi.stats().then(setStats);
    vehicleApi.list().then(setVehicles);
    adminApi.payments().then(setPayments);
    aiApi.demandForecast().then(setForecast);
    locationApi.list().then((locs) => {
      setLocations(locs);
      setForm((f) => ({ ...f, locationId: locs[0]?.locationId || "" }));
    });
  }, []);

  const setStatus = async (id, status) => {
    const updated = await vehicleApi.update(id, { status });
    setVehicles((prev) => prev.map((v) => (v.vehicleId === id ? updated : v)));
  };

  const removeVehicle = async (id) => {
    await vehicleApi.remove(id);
    setVehicles((prev) => prev.filter((v) => v.vehicleId !== id));
  };

  const updateForm = (key) => (e) => setForm({ ...form, [key]: e.target.value });

  const handleAddVehicle = async (e) => {
    e.preventDefault();
    setFormStatus({ type: "loading", msg: "Adding vehicle…" });
    try {
      const payload = {
        ...form,
        pricePerDay: Number(form.pricePerDay),
        seats: Number(form.seats),
        rating: Number(form.rating),
        locationId: Number(form.locationId),
        features: form.features
          .split(",")
          .map((f) => f.trim())
          .filter(Boolean),
      };
      const created = await vehicleApi.create(payload);
      setVehicles((prev) => [...prev, created]);
      setForm({ ...emptyVehicleForm, locationId: locations[0]?.locationId || "" });
      setFormStatus({ type: "success", msg: `Added "${created.name}" to the fleet.` });
      adminApi.stats().then(setStats);
    } catch {
      setFormStatus({ type: "error", msg: "Could not add vehicle. Check the fields and try again." });
    }
  };

  return (
    <div className="container page">
      <h1 className="display">Admin dashboard</h1>

      {stats && (
        <div className="stat-grid">
          <div className="card stat-card">
            <span className="muted">Total vehicles</span>
            <strong>{stats.totalVehicles}</strong>
          </div>
          <div className="card stat-card">
            <span className="muted">Available</span>
            <strong>{stats.available}</strong>
          </div>
          <div className="card stat-card">
            <span className="muted">Booked</span>
            <strong>{stats.booked}</strong>
          </div>
          <div className="card stat-card">
            <span className="muted">In maintenance</span>
            <strong>{stats.maintenance}</strong>
          </div>
          <div className="card stat-card">
            <span className="muted">Total bookings</span>
            <strong>{stats.totalBookings}</strong>
          </div>
          <div className="card stat-card">
            <span className="muted">Revenue collected</span>
            <strong>₹{stats.revenue.toLocaleString("en-IN")}</strong>
          </div>
        </div>
      )}

      <section className="section">
        <h2>AI demand forecast (next week)</h2>
        <div className="grid">
          {forecast.map((f, i) => (
            <div key={i} className="card forecast-card">
              <strong>{f.type}</strong>
              <p className="muted">{f.city}</p>
              <span className="price">{f.predictedDemand} bookings</span>
            </div>
          ))}
        </div>
      </section>

      <section className="section">
        <h2>List a new vehicle for rental</h2>
        <form className="card vehicle-form" onSubmit={handleAddVehicle}>
          <div className="form-grid">
            <div>
              <label>Name</label>
              <input required value={form.name} onChange={updateForm("name")} placeholder="e.g. Honda City" />
            </div>
            <div>
              <label>Brand</label>
              <input required value={form.brand} onChange={updateForm("brand")} placeholder="e.g. Honda" />
            </div>
            <div>
              <label>Type</label>
              <select value={form.type} onChange={updateForm("type")}>
                <option>Car</option>
                <option>Bike</option>
                <option>EV</option>
                <option>Premium</option>
              </select>
            </div>
            <div>
              <label>Price / day (₹)</label>
              <input required type="number" min="1" value={form.pricePerDay} onChange={updateForm("pricePerDay")} />
            </div>
            <div>
              <label>Seats</label>
              <input required type="number" min="1" max="8" value={form.seats} onChange={updateForm("seats")} />
            </div>
            <div>
              <label>Transmission</label>
              <select value={form.transmission} onChange={updateForm("transmission")}>
                <option>Automatic</option>
                <option>Manual</option>
              </select>
            </div>
            <div>
              <label>Fuel</label>
              <select value={form.fuel} onChange={updateForm("fuel")}>
                <option>Petrol</option>
                <option>Diesel</option>
                <option>Electric</option>
              </select>
            </div>
            <div>
              <label>Pickup location</label>
              <select value={form.locationId} onChange={updateForm("locationId")}>
                {locations.map((l) => (
                  <option key={l.locationId} value={l.locationId}>
                    {l.city} - {l.area}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label>Emoji icon</label>
              <input value={form.image} onChange={updateForm("image")} maxLength={2} />
            </div>
            <div className="form-span-2">
              <label>Features (comma-separated)</label>
              <input value={form.features} onChange={updateForm("features")} placeholder="Sunroof, Cruise Control" />
            </div>
          </div>
          <button className="btn btn-primary" type="submit">
            Add to fleet
          </button>
          {formStatus && <p className={`status-msg status-${formStatus.type}`}>{formStatus.msg}</p>}
        </form>
      </section>

      <section className="section">
        <h2>Fleet management</h2>
        <div className="admin-table">
          {vehicles.map((v) => (
            <div key={v.vehicleId} className="card admin-row">
              <span>
                {v.image} {v.name}
              </span>
              <span className="muted">{v.type}</span>
              <span>₹{v.pricePerDay}/day</span>
              <select value={v.status} onChange={(e) => setStatus(v.vehicleId, e.target.value)}>
                <option>Available</option>
                <option>Booked</option>
                <option>Maintenance</option>
              </select>
              <button className="btn btn-danger" onClick={() => removeVehicle(v.vehicleId)}>
                Remove
              </button>
            </div>
          ))}
        </div>
      </section>

      <section className="section">
        <h2>Payments collected</h2>
        {payments.length === 0 ? (
          <p className="muted">No payments recorded yet.</p>
        ) : (
          <div className="admin-table">
            {payments.map((p) => (
              <div key={p.paymentId} className="card payment-row">
                <span>{p.vehicleName}</span>
                <span className="muted">{p.customerName}</span>
                <span>₹{p.amount.toLocaleString("en-IN")}</span>
                <span className="muted small">{p.method}</span>
                <span className="muted small">{p.paidAt}</span>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
