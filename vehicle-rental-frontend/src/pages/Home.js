import React, { useEffect, useMemo, useState } from "react";
import { vehicleApi, locationApi, aiApi } from "../services/api";
import VehicleCard from "../components/VehicleCard";
import FilterBar from "../components/FilterBar";

export default function Home() {
  const [vehicles, setVehicles] = useState([]);
  const [locations, setLocations] = useState([]);
  const [recommendations, setRecommendations] = useState([]);
  const [filters, setFilters] = useState({});
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    locationApi.list().then(setLocations);
    aiApi.recommendations().then(setRecommendations);
  }, []);

  useEffect(() => {
    setLoading(true);
    vehicleApi.list(filters).then((data) => {
      setVehicles(data);
      setLoading(false);
    });
  }, [filters]);

  const recommendedVehicles = useMemo(
    () =>
      recommendations
        .map((r) => ({ ...vehicles.find((v) => v.vehicleId === r.vehicleId), reason: r.reason }))
        .filter((v) => v.vehicleId),
    [recommendations, vehicles]
  );

  return (
    <div className="container page">
      <section className="hero">
        <h1 className="display">Rent smarter, drive further.</h1>
        <p className="hero-sub">
          AI-matched vehicles, transparent pricing, and instant booking across your city.
        </p>
      </section>

      <FilterBar filters={filters} setFilters={setFilters} locations={locations} />

      {recommendedVehicles.length > 0 && (
        <section className="section">
          <h2>Recommended for you</h2>
          <div className="grid">
            {recommendedVehicles.map((v) => (
              <VehicleCard key={v.vehicleId} vehicle={v} />
            ))}
          </div>
        </section>
      )}

      <section className="section">
        <h2>All vehicles {loading ? "" : `(${vehicles.length})`}</h2>
        {loading ? (
          <p className="muted">Loading vehicles…</p>
        ) : vehicles.length === 0 ? (
          <p className="muted">No vehicles match your filters.</p>
        ) : (
          <div className="grid">
            {vehicles.map((v) => (
              <VehicleCard key={v.vehicleId} vehicle={v} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
