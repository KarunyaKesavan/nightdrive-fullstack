import React from "react";

const TYPES = ["", "Car", "Bike", "EV", "Premium"];

export default function FilterBar({ filters, setFilters, locations }) {
  const update = (key) => (e) => setFilters({ ...filters, [key]: e.target.value });

  return (
    <div className="card filter-bar">
      <div>
        <label>Search</label>
        <input
          type="text"
          placeholder="Search by name or brand"
          value={filters.search || ""}
          onChange={update("search")}
        />
      </div>
      <div>
        <label>Type</label>
        <select value={filters.type || ""} onChange={update("type")}>
          {TYPES.map((t) => (
            <option key={t} value={t}>
              {t || "All types"}
            </option>
          ))}
        </select>
      </div>
      <div>
        <label>Location</label>
        <select value={filters.locationId || ""} onChange={update("locationId")}>
          <option value="">All locations</option>
          {locations.map((l) => (
            <option key={l.locationId} value={l.locationId}>
              {l.city} - {l.area}
            </option>
          ))}
        </select>
      </div>
      <div>
        <label>Max price / day</label>
        <input
          type="number"
          placeholder="e.g. 5000"
          value={filters.maxPrice || ""}
          onChange={update("maxPrice")}
        />
      </div>
    </div>
  );
}
