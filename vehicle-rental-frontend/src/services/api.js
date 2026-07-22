// -----------------------------------------------------------------------
// API service layer.
// Set USE_MOCK = false and configure BASE_URL once the ASP.NET Core API
// is running, so every call below simply switches from mock data to a
// real fetch() against the backend — no component code needs to change.
// -----------------------------------------------------------------------
export const USE_MOCK = false;
const BASE_URL = "http://localhost:5000/api";

const delay = (ms = 350) => new Promise((res) => setTimeout(res, ms));

// ---------------------------- Mock data ---------------------------------
const locations = [
  { locationId: 1, city: "Coimbatore", area: "RS Puram", lat: 11.0068, lng: 76.9558 },
  { locationId: 2, city: "Coimbatore", area: "Peelamedu", lat: 11.0296, lng: 77.0266 },
  { locationId: 3, city: "Chennai", area: "T Nagar", lat: 13.0418, lng: 80.2341 },
  { locationId: 4, city: "Bengaluru", area: "Indiranagar", lat: 12.9716, lng: 77.6412 },
];

let vehicles = [
  { vehicleId: 1, name: "Tesla Model 3", type: "EV", brand: "Tesla", pricePerDay: 6500, seats: 5, transmission: "Automatic", fuel: "Electric", rating: 4.8, status: "Available", locationId: 4, image: "🚗", features: ["Autopilot", "Fast Charging", "Premium Audio"] },
  { vehicleId: 2, name: "Mahindra Thar", type: "Premium", brand: "Mahindra", pricePerDay: 4200, seats: 4, transmission: "Manual", fuel: "Diesel", rating: 4.6, status: "Available", locationId: 1, image: "🚙", features: ["4x4", "Convertible Top"] },
  { vehicleId: 3, name: "Honda Activa", type: "Bike", brand: "Honda", pricePerDay: 500, seats: 2, transmission: "Automatic", fuel: "Petrol", rating: 4.4, status: "Booked", locationId: 1, image: "🛵", features: ["Fuel Efficient"] },
  { vehicleId: 4, name: "Hyundai Creta", type: "Car", brand: "Hyundai", pricePerDay: 3200, seats: 5, transmission: "Automatic", fuel: "Diesel", rating: 4.5, status: "Available", locationId: 2, image: "🚗", features: ["Sunroof", "Cruise Control"] },
  { vehicleId: 5, name: "Royal Enfield Classic 350", type: "Bike", brand: "Royal Enfield", pricePerDay: 900, seats: 2, transmission: "Manual", fuel: "Petrol", rating: 4.7, status: "Available", locationId: 3, image: "🏍️", features: ["Retro Styling"] },
  { vehicleId: 6, name: "Tata Nexon EV", type: "EV", brand: "Tata", pricePerDay: 3800, seats: 5, transmission: "Automatic", fuel: "Electric", rating: 4.3, status: "Maintenance", locationId: 2, image: "🚗", features: ["Fast Charging", "Connected App"] },
  { vehicleId: 7, name: "Toyota Fortuner", type: "Premium", brand: "Toyota", pricePerDay: 7200, seats: 7, transmission: "Automatic", fuel: "Diesel", rating: 4.9, status: "Available", locationId: 4, image: "🚙", features: ["7-Seater", "4x4", "Leather Seats"] },
  { vehicleId: 8, name: "Maruti Swift", type: "Car", brand: "Maruti", pricePerDay: 1800, seats: 5, transmission: "Manual", fuel: "Petrol", rating: 4.2, status: "Available", locationId: 1, image: "🚗", features: ["Fuel Efficient", "Compact"] },
];

let bookings = [
  { bookingId: 101, customerId: 1, vehicleId: 3, startDate: "2026-07-22", endDate: "2026-07-24", status: "Confirmed", totalAmount: 1000, pickupLocationId: 1 },
  { bookingId: 102, customerId: 1, vehicleId: 1, startDate: "2026-06-10", endDate: "2026-06-12", status: "Completed", totalAmount: 13000, pickupLocationId: 4 },
];

let reviews = [
  { reviewId: 1, vehicleId: 1, customerId: 1, rating: 5, comment: "Incredibly smooth ride, autopilot was a game changer on the highway.", date: "2026-06-13" },
  { reviewId: 2, vehicleId: 4, customerId: 2, rating: 4, comment: "Comfortable and spacious, great for family trips.", date: "2026-05-02" },
];

const notifications = [
  { notificationId: 1, customerId: 1, message: "Your booking for Honda Activa is confirmed.", read: false, date: "2026-07-19" },
  { notificationId: 2, customerId: 1, message: "20% off EVs this weekend — book now.", read: true, date: "2026-07-15" },
];

const recommendations = [
  { vehicleId: 7, reason: "Matches your preference for premium 4x4s" },
  { vehicleId: 1, reason: "Trending EV in your city" },
];

const demandForecast = [
  { type: "EV", city: "Coimbatore", week: "2026-W30", predictedDemand: 34 },
  { type: "Bike", city: "Coimbatore", week: "2026-W30", predictedDemand: 61 },
  { type: "Premium", city: "Chennai", week: "2026-W30", predictedDemand: 22 },
];

const mockLocationPoints = {}; // vehicleId -> { lat, lng }
let mockPayments = [
  { paymentId: 1, bookingId: 101, vehicleId: 3, vehicleName: "Honda Activa", customerId: 1, customerName: "Arun Kumar", amount: 1000, method: "Mock Card", paidAt: "2026-07-19 10:00" },
  { paymentId: 2, bookingId: 102, vehicleId: 1, vehicleName: "Tesla Model 3", customerId: 1, customerName: "Arun Kumar", amount: 13000, method: "Mock Card", paidAt: "2026-06-10 09:00" },
];

let currentUser = null; // { id, name, email, role: "Customer" | "Admin" }

// ------------------------------ Helpers ----------------------------------
async function mockCall(fn) {
  await delay();
  return fn();
}

async function realCall(path, options = {}) {
  const token = localStorage.getItem("token");
  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers || {}),
    },
  });
  if (!res.ok) throw new Error(`API error ${res.status}`);
  return res.status === 204 ? null : res.json();
}

// ------------------------------ Auth --------------------------------------
// On page load in real-API mode, restore whoever was logged in from localStorage
// (the mock branch never persists across a refresh, which is fine for mock data).
if (!USE_MOCK) {
  const savedUser = localStorage.getItem("user");
  if (savedUser) {
    try { currentUser = JSON.parse(savedUser); } catch { /* ignore corrupt value */ }
  }
}

function persistSession(res) {
  localStorage.setItem("token", res.token);
  localStorage.setItem("user", JSON.stringify(res.user));
  currentUser = res.user;
  return res;
}

export const authApi = {
  login: (email, password) =>
    USE_MOCK
      ? mockCall(() => {
          currentUser = { id: 1, name: "Arun Kumar", email, role: email.includes("admin") ? "Admin" : "Customer" };
          return { token: "mock-jwt-token", user: currentUser };
        })
      : realCall("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }).then(persistSession),

  register: (payload) =>
    USE_MOCK
      ? mockCall(() => {
          currentUser = { id: 2, name: payload.name, email: payload.email, role: "Customer" };
          return { token: "mock-jwt-token", user: currentUser };
        })
      : realCall("/auth/register", { method: "POST", body: JSON.stringify(payload) }).then(persistSession),

  currentUser: () => currentUser,
  logout: () => {
    currentUser = null;
    if (!USE_MOCK) {
      localStorage.removeItem("token");
      localStorage.removeItem("user");
    }
  },
};

// ---------------------------- Vehicles -------------------------------------
export const vehicleApi = {
  list: (filters = {}) =>
    USE_MOCK
      ? mockCall(() => {
          let out = [...vehicles];
          if (filters.type) out = out.filter((v) => v.type === filters.type);
          if (filters.locationId) out = out.filter((v) => v.locationId === Number(filters.locationId));
          if (filters.maxPrice) out = out.filter((v) => v.pricePerDay <= Number(filters.maxPrice));
          if (filters.search) {
            const q = filters.search.toLowerCase();
            out = out.filter((v) => v.name.toLowerCase().includes(q) || v.brand.toLowerCase().includes(q));
          }
          return out;
        })
      : realCall(`/vehicles?${new URLSearchParams(filters)}`),

  get: (id) =>
    USE_MOCK
      ? mockCall(() => vehicles.find((v) => v.vehicleId === Number(id)))
      : realCall(`/vehicles/${id}`),

  create: (payload) =>
    USE_MOCK
      ? mockCall(() => {
          const v = { ...payload, vehicleId: Math.max(...vehicles.map((x) => x.vehicleId)) + 1, status: "Available" };
          vehicles.push(v);
          return v;
        })
      : realCall("/vehicles", { method: "POST", body: JSON.stringify(payload) }),

  update: (id, payload) =>
    USE_MOCK
      ? mockCall(() => {
          vehicles = vehicles.map((v) => (v.vehicleId === Number(id) ? { ...v, ...payload } : v));
          return vehicles.find((v) => v.vehicleId === Number(id));
        })
      : realCall(`/vehicles/${id}`, { method: "PUT", body: JSON.stringify(payload) }),

  remove: (id) =>
    USE_MOCK
      ? mockCall(() => { vehicles = vehicles.filter((v) => v.vehicleId !== Number(id)); })
      : realCall(`/vehicles/${id}`, { method: "DELETE" }),
};

// ----------------------------- Locations ------------------------------------
export const locationApi = {
  list: () => (USE_MOCK ? mockCall(() => locations) : realCall("/locations")),
};

// ----------------------------- Bookings -------------------------------------
export const bookingApi = {
  listForCustomer: (customerId) =>
    USE_MOCK
      ? mockCall(() => bookings.filter((b) => b.customerId === customerId))
      : realCall(`/bookings/customer/${customerId}`),

  listAll: () => (USE_MOCK ? mockCall(() => bookings) : realCall("/bookings")),

  create: (payload) =>
    USE_MOCK
      ? mockCall(() => {
          const b = { ...payload, bookingId: Math.max(...bookings.map((x) => x.bookingId)) + 1, status: "Confirmed" };
          bookings.push(b);
          vehicles = vehicles.map((v) => (v.vehicleId === payload.vehicleId ? { ...v, status: "Booked" } : v));
          return b;
        })
      : realCall("/bookings", { method: "POST", body: JSON.stringify(payload) }),

  cancel: (id) =>
    USE_MOCK
      ? mockCall(() => {
          bookings = bookings.map((b) => (b.bookingId === id ? { ...b, status: "Cancelled" } : b));
        })
      : realCall(`/bookings/${id}/cancel`, { method: "POST" }),
};

// ----------------------------- Payments -------------------------------------
export const paymentApi = {
  pay: (payload) =>
    USE_MOCK
      ? mockCall(() => {
          const p = { paymentId: Date.now(), status: "Success", ...payload };
          const vehicle = vehicles.find((v) => v.vehicleId === payload.vehicleId);
          const booking = bookings.find((b) => b.bookingId === payload.bookingId);
          mockPayments.push({
            paymentId: p.paymentId,
            bookingId: payload.bookingId,
            vehicleId: booking?.vehicleId,
            vehicleName: vehicle?.name || "Vehicle",
            customerId: booking?.customerId,
            customerName: currentUser?.name || "Customer",
            amount: payload.amount,
            method: payload.method,
            paidAt: new Date().toISOString().slice(0, 16).replace("T", " "),
          });
          return p;
        })
      : realCall("/payments", { method: "POST", body: JSON.stringify(payload) }),
};

// ----------------------------- Reviews --------------------------------------
export const reviewApi = {
  listForVehicle: (vehicleId) =>
    USE_MOCK
      ? mockCall(() => reviews.filter((r) => r.vehicleId === Number(vehicleId)))
      : realCall(`/reviews/vehicle/${vehicleId}`),

  create: (payload) =>
    USE_MOCK
      ? mockCall(() => {
          const r = { ...payload, reviewId: reviews.length + 1, date: new Date().toISOString().slice(0, 10) };
          reviews.push(r);
          const vehicleReviews = reviews.filter((rv) => rv.vehicleId === payload.vehicleId);
          const avg = vehicleReviews.reduce((sum, rv) => sum + rv.rating, 0) / vehicleReviews.length;
          vehicles = vehicles.map((v) =>
            v.vehicleId === payload.vehicleId ? { ...v, rating: Math.round(avg * 10) / 10 } : v
          );
          return r;
        })
      : realCall("/reviews", { method: "POST", body: JSON.stringify(payload) }),
};

// --------------------------- Notifications -----------------------------------
export const notificationApi = {
  listForCustomer: (customerId) =>
    USE_MOCK
      ? mockCall(() => notifications.filter((n) => n.customerId === customerId))
      : realCall(`/notifications/customer/${customerId}`),
};

// ------------------------------- AI -------------------------------------------
export const aiApi = {
  recommendations: () => (USE_MOCK ? mockCall(() => recommendations) : realCall("/ai/recommendations")),
  demandForecast: () => (USE_MOCK ? mockCall(() => demandForecast) : realCall("/ai/demand-forecast")),
};

// --------------------------- Live tracking (OpenStreetMap) ----------------------
export const vehicleLocationApi = {
  get: (vehicleId) =>
    USE_MOCK
      ? mockCall(() => {
          // Simulate a slow random walk around the vehicle's pickup location.
          const vehicle = vehicles.find((v) => v.vehicleId === Number(vehicleId));
          if (!vehicle) return null;
          const base = locations.find((l) => l.locationId === vehicle.locationId) || { lat: 11.0068, lng: 76.9558 };
          const prev = mockLocationPoints[vehicleId] || { lat: base.lat, lng: base.lng };
          const next = {
            lat: prev.lat + (Math.random() - 0.5) * 0.004,
            lng: prev.lng + (Math.random() - 0.5) * 0.004,
          };
          mockLocationPoints[vehicleId] = next;
          return { vehicleId: Number(vehicleId), lat: next.lat, lng: next.lng, updatedAt: new Date().toISOString() };
        })
      : realCall(`/vehicles/${vehicleId}/location`),
};

// ------------------------------ Admin dashboard --------------------------------
export const adminApi = {
  stats: () =>
    USE_MOCK
      ? mockCall(() => ({
          totalVehicles: vehicles.length,
          available: vehicles.filter((v) => v.status === "Available").length,
          booked: vehicles.filter((v) => v.status === "Booked").length,
          maintenance: vehicles.filter((v) => v.status === "Maintenance").length,
          totalBookings: bookings.length,
          revenue: bookings.reduce((sum, b) => sum + b.totalAmount, 0),
        }))
      : realCall("/admin/stats"),

  payments: () =>
    USE_MOCK ? mockCall(() => [...mockPayments].reverse()) : realCall("/admin/payments"),
};
