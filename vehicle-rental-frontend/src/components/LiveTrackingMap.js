import React, { useEffect, useRef, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { vehicleLocationApi } from "../services/api";

// react-leaflet's default marker icon references image files by URL that
// don't survive a webpack/CRA bundle — this points them at the CDN copies
// instead, which is the standard workaround.
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
});

function Recenter({ position }) {
  const map = useMap();
  useEffect(() => {
    if (position) map.panTo(position);
  }, [position, map]);
  return null;
}

export default function LiveTrackingMap({ vehicleId, fallbackLat, fallbackLng, vehicleName }) {
  const [point, setPoint] = useState(null);
  const [error, setError] = useState(false);
  const intervalRef = useRef(null);

  useEffect(() => {
    let cancelled = false;

    const poll = async () => {
      try {
        const loc = await vehicleLocationApi.get(vehicleId);
        if (!cancelled && loc) {
          setPoint([loc.lat, loc.lng]);
          setError(false);
        }
      } catch {
        if (!cancelled) setError(true);
      }
    };

    poll();
    intervalRef.current = setInterval(poll, 5000);
    return () => {
      cancelled = true;
      clearInterval(intervalRef.current);
    };
  }, [vehicleId]);

  const position = point || [fallbackLat, fallbackLng];

  return (
    <div className="map-wrap">
      <MapContainer center={position} zoom={14} scrollWheelZoom={false} className="live-map">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <Marker position={position}>
          <Popup>{vehicleName}</Popup>
        </Marker>
        <Recenter position={point} />
      </MapContainer>
      <p className="muted small map-caption">
        {point
          ? "Live position — updates every 5s"
          : error
          ? "Live position unavailable — showing pickup location"
          : "Locating vehicle…"}
      </p>
    </div>
  );
}
