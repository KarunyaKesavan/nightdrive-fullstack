namespace VehicleRentalApi.Entities;

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public class Admin
{
    public int AdminId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public class Location
{
    public int LocationId { get; set; }
    public string City { get; set; } = "";
    public string Area { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class Vehicle
{
    public int VehicleId { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Brand { get; set; } = "";
    public decimal PricePerDay { get; set; }
    public int Seats { get; set; }
    public string Transmission { get; set; } = "";
    public string Fuel { get; set; } = "";
    public double Rating { get; set; }
    public string Status { get; set; } = "Available";
    public int LocationId { get; set; }
    public string Image { get; set; } = "🚗";
    // stored as a comma-separated string in the DB; exposed as string[] in the DTO
    public string FeaturesCsv { get; set; } = "";
}

public class Booking
{
    public int BookingId { get; set; }
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Confirmed";
    public decimal TotalAmount { get; set; }
    public int PickupLocationId { get; set; }
}

public class Review
{
    public int ReviewId { get; set; }
    public int VehicleId { get; set; }
    public int CustomerId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateOnly Date { get; set; }
}

public class Notification
{
    public int NotificationId { get; set; }
    public int CustomerId { get; set; }
    public string Message { get; set; } = "";
    public bool Read { get; set; }
    public DateOnly Date { get; set; }
}

public class Payment
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public string Status { get; set; } = "Success";
    public DateTime PaidAt { get; set; }
}

public class VehicleLocation
{
    public int VehicleId { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Recommendation
{
    public int VehicleId { get; set; }
    public string Reason { get; set; } = "";
}

public class DemandForecast
{
    public string Type { get; set; } = "";
    public string City { get; set; } = "";
    public string Week { get; set; } = "";
    public int PredictedDemand { get; set; }
}
