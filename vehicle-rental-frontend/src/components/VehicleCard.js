import React from "react";
import { Link } from "react-router-dom";

const badgeClass = {
  Available: "badge-available",
  Booked: "badge-booked",
  Maintenance: "badge-maintenance",
};

export default function VehicleCard({ vehicle }) {
  return (
    <Link to={`/vehicles/${vehicle.vehicleId}`} className="card vehicle-card">
      <div className="vehicle-card-top">
        <span className="vehicle-emoji">{vehicle.image}</span>
        <span className={`badge ${badgeClass[vehicle.status] || ""}`}>
          {vehicle.status}
        </span>
      </div>
      <h3>{vehicle.name}</h3>
      <p className="vehicle-meta">
        {vehicle.type} · {vehicle.transmission} · {vehicle.fuel}
      </p>
      <div className="vehicle-card-bottom">
        <span className="price">
          ₹{vehicle.pricePerDay.toLocaleString("en-IN")}
          <small>/day</small>
        </span>
        <span className="rating">★ {vehicle.rating}</span>
      </div>
    </Link>
  );
}
