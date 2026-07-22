import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ name: "", email: "", password: "", role: "Customer" });

  const update = (key) => (e) => setForm({ ...form, [key]: e.target.value });

  const handleSubmit = async (e) => {
    e.preventDefault();
    await register(form);
    navigate(form.role === "Admin" ? "/admin" : "/");
  };

  return (
    <div className="container page auth-page">
      <form className="card auth-card" onSubmit={handleSubmit}>
        <h1 className="display">Create your account</h1>

        <label>I am signing up as</label>
        <div className="role-toggle">
          <button
            type="button"
            className={`role-btn ${form.role === "Customer" ? "active" : ""}`}
            onClick={() => setForm({ ...form, role: "Customer" })}
          >
            Customer — rent vehicles
          </button>
          <button
            type="button"
            className={`role-btn ${form.role === "Admin" ? "active" : ""}`}
            onClick={() => setForm({ ...form, role: "Admin" })}
          >
            Admin — list vehicles
          </button>
        </div>

        <label>Full name</label>
        <input required value={form.name} onChange={update("name")} />
        <label>Email</label>
        <input type="email" required value={form.email} onChange={update("email")} />
        <label>Password</label>
        <input type="password" required value={form.password} onChange={update("password")} />
        <button className="btn btn-primary full" type="submit">
          Sign up
        </button>
        <p className="muted small">
          Already have an account? <Link to="/login">Log in</Link>
        </p>
      </form>
    </div>
  );
}
