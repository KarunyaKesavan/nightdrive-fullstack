-- NightDrive schema.
-- Safe to re-run: drops tables first (in FK-safe order) so you never hit a
-- "stale old-shape table" error from a leftover draft schema.
-- Seed DATA is handled separately at app startup (see bottom note) — this
-- file is DDL only.

CREATE DATABASE IF NOT EXISTS nightdrive;
USE nightdrive;

SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS Maintenance;
DROP TABLE IF EXISTS Recommendation;
DROP TABLE IF EXISTS DemandForecast;
DROP TABLE IF EXISTS Notification;
DROP TABLE IF EXISTS Review;
DROP TABLE IF EXISTS Payment;
DROP TABLE IF EXISTS VehicleLocation;
DROP TABLE IF EXISTS Booking;
DROP TABLE IF EXISTS Vehicle;
DROP TABLE IF EXISTS Location;
DROP TABLE IF EXISTS Admin;
DROP TABLE IF EXISTS Customer;
SET FOREIGN_KEY_CHECKS = 1;

CREATE TABLE Customer (
  CustomerId INT AUTO_INCREMENT PRIMARY KEY,
  Name VARCHAR(120) NOT NULL,
  Email VARCHAR(180) NOT NULL UNIQUE,
  PasswordHash VARCHAR(200) NOT NULL
);

CREATE TABLE Admin (
  AdminId INT AUTO_INCREMENT PRIMARY KEY,
  Name VARCHAR(120) NOT NULL,
  Email VARCHAR(180) NOT NULL UNIQUE,
  PasswordHash VARCHAR(200) NOT NULL
);

CREATE TABLE Location (
  LocationId INT AUTO_INCREMENT PRIMARY KEY,
  City VARCHAR(100) NOT NULL,
  Area VARCHAR(100) NOT NULL,
  Lat DOUBLE NOT NULL,
  Lng DOUBLE NOT NULL
);

CREATE TABLE Vehicle (
  VehicleId INT AUTO_INCREMENT PRIMARY KEY,
  Name VARCHAR(120) NOT NULL,
  Type VARCHAR(40) NOT NULL,
  Brand VARCHAR(60) NOT NULL,
  PricePerDay DECIMAL(10,2) NOT NULL,
  Seats INT NOT NULL,
  Transmission VARCHAR(30) NOT NULL,
  Fuel VARCHAR(30) NOT NULL,
  Rating DOUBLE NOT NULL DEFAULT 0,
  Status VARCHAR(20) NOT NULL DEFAULT 'Available',
  LocationId INT NOT NULL,
  Image VARCHAR(10) NOT NULL DEFAULT '🚗',
  FeaturesCsv VARCHAR(500) NOT NULL DEFAULT '',
  FOREIGN KEY (LocationId) REFERENCES Location(LocationId)
);

CREATE TABLE Booking (
  BookingId INT AUTO_INCREMENT PRIMARY KEY,
  CustomerId INT NOT NULL,
  VehicleId INT NOT NULL,
  StartDate DATE NOT NULL,
  EndDate DATE NOT NULL,
  Status VARCHAR(20) NOT NULL DEFAULT 'Confirmed',
  TotalAmount DECIMAL(10,2) NOT NULL,
  PickupLocationId INT NOT NULL,
  FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId),
  FOREIGN KEY (VehicleId) REFERENCES Vehicle(VehicleId),
  FOREIGN KEY (PickupLocationId) REFERENCES Location(LocationId)
);

CREATE TABLE Review (
  ReviewId INT AUTO_INCREMENT PRIMARY KEY,
  VehicleId INT NOT NULL,
  CustomerId INT NOT NULL,
  Rating INT NOT NULL,
  Comment VARCHAR(1000) NOT NULL,
  Date DATE NOT NULL,
  FOREIGN KEY (VehicleId) REFERENCES Vehicle(VehicleId),
  FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId)
);

CREATE TABLE Payment (
  PaymentId INT AUTO_INCREMENT PRIMARY KEY,
  BookingId INT NOT NULL,
  Amount DECIMAL(10,2) NOT NULL,
  Method VARCHAR(40) NOT NULL,
  Status VARCHAR(20) NOT NULL DEFAULT 'Success',
  PaidAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (BookingId) REFERENCES Booking(BookingId)
);

-- One row per vehicle, overwritten as its position updates (mock GPS feed,
-- simulated server-side while a vehicle is Booked — see Services/VehicleTrackingSimulator.cs).
CREATE TABLE VehicleLocation (
  VehicleId INT PRIMARY KEY,
  Lat DOUBLE NOT NULL,
  Lng DOUBLE NOT NULL,
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (VehicleId) REFERENCES Vehicle(VehicleId)
);

CREATE TABLE Notification (
  NotificationId INT AUTO_INCREMENT PRIMARY KEY,
  CustomerId INT NOT NULL,
  Message VARCHAR(500) NOT NULL,
  `Read` BOOLEAN NOT NULL DEFAULT FALSE,
  Date DATE NOT NULL,
  FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId)
);

-- Populated by the Python AI service's batch jobs; read-only from the API's point of view.
CREATE TABLE Recommendation (
  RecommendationId INT AUTO_INCREMENT PRIMARY KEY,
  CustomerId INT NOT NULL,
  VehicleId INT NOT NULL,
  Reason VARCHAR(300) NOT NULL,
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId),
  FOREIGN KEY (VehicleId) REFERENCES Vehicle(VehicleId)
);

CREATE TABLE DemandForecast (
  DemandForecastId INT AUTO_INCREMENT PRIMARY KEY,
  Type VARCHAR(40) NOT NULL,
  City VARCHAR(100) NOT NULL,
  Week VARCHAR(10) NOT NULL,
  PredictedDemand INT NOT NULL,
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Maintenance (
  MaintenanceId INT AUTO_INCREMENT PRIMARY KEY,
  VehicleId INT NOT NULL,
  PredictedIssue VARCHAR(300) NOT NULL,
  RiskScore DOUBLE NOT NULL,
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (VehicleId) REFERENCES Vehicle(VehicleId)
);

-- No seed data here on purpose: passwords need bcrypt hashing (can't be
-- hand-written safely in SQL) and Booking/Review/Notification rows have
-- foreign keys into Customer, so ordering matters. All of that is handled
-- idempotently at application startup instead — see Seed/DevSeeder.cs —
-- which only inserts data once (checks if Vehicle/Admin are empty first)
-- and is safe to leave in for local/dev use.
