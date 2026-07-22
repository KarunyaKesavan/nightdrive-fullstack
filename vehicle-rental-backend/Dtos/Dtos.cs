namespace VehicleRentalApi.Dtos;

// ---------- Auth ----------
public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Name, string Email, string Password, string Role);
public record UserDto(int Id, string Name, string Email, string Role);
public record AuthResponse(string Token, UserDto User);

// ---------- Vehicles ----------
public record VehicleDto(
    int VehicleId, string Name, string Type, string Brand, decimal PricePerDay,
    int Seats, string Transmission, string Fuel, double Rating, string Status,
    int LocationId, string Image, string[] Features);

public record VehicleUpsertRequest(
    string Name, string Type, string Brand, decimal PricePerDay,
    int Seats, string Transmission, string Fuel, double Rating, string Status,
    int LocationId, string Image, string[] Features);

// ---------- Locations ----------
public record LocationDto(int LocationId, string City, string Area, double Lat, double Lng);

// ---------- Bookings ----------
public record BookingDto(
    int BookingId, int CustomerId, int VehicleId, string StartDate, string EndDate,
    string Status, decimal TotalAmount, int PickupLocationId);

public record BookingCreateRequest(
    int CustomerId, int VehicleId, string StartDate, string EndDate,
    decimal TotalAmount, int PickupLocationId);

// ---------- Payments ----------
public record PaymentRequest(int BookingId, decimal Amount, string Method);
public record PaymentDto(int PaymentId, string Status, int BookingId, decimal Amount, string Method);
public record PaymentRecordDto(
    int PaymentId, int BookingId, int VehicleId, string VehicleName,
    int CustomerId, string CustomerName, decimal Amount, string Method, string PaidAt);

// ---------- Live tracking ----------
public record VehicleLocationDto(int VehicleId, double Lat, double Lng, string UpdatedAt);

// ---------- Reviews ----------
public record ReviewDto(int ReviewId, int VehicleId, int CustomerId, int Rating, string Comment, string Date);
public record ReviewCreateRequest(int VehicleId, int CustomerId, int Rating, string Comment);

// ---------- Notifications ----------
public record NotificationDto(int NotificationId, int CustomerId, string Message, bool Read, string Date);

// ---------- AI ----------
public record RecommendationDto(int VehicleId, string Reason);
public record DemandForecastDto(string Type, string City, string Week, int PredictedDemand);

// ---------- Admin ----------
public record AdminStatsDto(
    int TotalVehicles, int Available, int Booked, int Maintenance,
    int TotalBookings, decimal Revenue);
