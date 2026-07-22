using Dapper;
using VehicleRentalApi.Data;
using VehicleRentalApi.Dtos;
using VehicleRentalApi.Entities;

namespace VehicleRentalApi.Repositories;

public class LocationRepository
{
    private readonly IDbConnectionFactory _db;
    public LocationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<LocationDto>> ListAsync()
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Location>("SELECT * FROM Location");
        return rows.Select(l => new LocationDto(l.LocationId, l.City, l.Area, l.Lat, l.Lng));
    }
}

public class CustomerRepository
{
    private readonly IDbConnectionFactory _db;
    public CustomerRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Customer?> FindByEmailAsync(string email)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Customer>(
            "SELECT * FROM Customer WHERE Email = @email", new { email });
    }

    public async Task<Admin?> FindAdminByEmailAsync(string email)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Admin>(
            "SELECT * FROM Admin WHERE Email = @email", new { email });
    }

    public async Task<Customer> CreateAsync(string name, string email, string passwordHash)
    {
        using var conn = _db.Create();
        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Customer (Name, Email, PasswordHash) VALUES (@name, @email, @passwordHash);
            SELECT LAST_INSERT_ID();", new { name, email, passwordHash });
        return new Customer { CustomerId = id, Name = name, Email = email, PasswordHash = passwordHash };
    }

    public async Task<Admin> CreateAdminAsync(string name, string email, string passwordHash)
    {
        using var conn = _db.Create();
        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Admin (Name, Email, PasswordHash) VALUES (@name, @email, @passwordHash);
            SELECT LAST_INSERT_ID();", new { name, email, passwordHash });
        return new Admin { AdminId = id, Name = name, Email = email, PasswordHash = passwordHash };
    }
}

public class BookingRepository
{
    private readonly IDbConnectionFactory _db;
    public BookingRepository(IDbConnectionFactory db) => _db = db;

    private static BookingDto ToDto(Booking b) => new(
        b.BookingId, b.CustomerId, b.VehicleId,
        b.StartDate.ToString("yyyy-MM-dd"), b.EndDate.ToString("yyyy-MM-dd"),
        b.Status, b.TotalAmount, b.PickupLocationId);

    public async Task<IEnumerable<BookingDto>> ListForCustomerAsync(int customerId)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Booking>(
            "SELECT * FROM Booking WHERE CustomerId = @customerId ORDER BY BookingId DESC", new { customerId });
        return rows.Select(ToDto);
    }

    public async Task<IEnumerable<BookingDto>> ListAllAsync()
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Booking>("SELECT * FROM Booking ORDER BY BookingId DESC");
        return rows.Select(ToDto);
    }

    public async Task<BookingDto> CreateAsync(BookingCreateRequest r)
    {
        using var conn = _db.Create();
        using var tx = conn.BeginTransaction();
        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Booking (CustomerId, VehicleId, StartDate, EndDate, Status, TotalAmount, PickupLocationId)
            VALUES (@CustomerId, @VehicleId, @StartDate, @EndDate, 'Confirmed', @TotalAmount, @PickupLocationId);
            SELECT LAST_INSERT_ID();",
            new { r.CustomerId, r.VehicleId, r.StartDate, r.EndDate, r.TotalAmount, r.PickupLocationId }, tx);
        await conn.ExecuteAsync("UPDATE Vehicle SET Status = 'Booked' WHERE VehicleId = @VehicleId",
            new { r.VehicleId }, tx);
        tx.Commit();
        var b = await conn.QuerySingleAsync<Booking>("SELECT * FROM Booking WHERE BookingId = @id", new { id });
        return ToDto(b);
    }

    public async Task CancelAsync(int id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("UPDATE Booking SET Status = 'Cancelled' WHERE BookingId = @id", new { id });
    }
}

public class ReviewRepository
{
    private readonly IDbConnectionFactory _db;
    public ReviewRepository(IDbConnectionFactory db) => _db = db;

    private static ReviewDto ToDto(Review r) => new(
        r.ReviewId, r.VehicleId, r.CustomerId, r.Rating, r.Comment, r.Date.ToString("yyyy-MM-dd"));

    public async Task<IEnumerable<ReviewDto>> ListForVehicleAsync(int vehicleId)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Review>(
            "SELECT * FROM Review WHERE VehicleId = @vehicleId ORDER BY ReviewId DESC", new { vehicleId });
        return rows.Select(ToDto);
    }

    public async Task<ReviewDto> CreateAsync(ReviewCreateRequest r)
    {
        using var conn = _db.Create();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Review (VehicleId, CustomerId, Rating, Comment, Date)
            VALUES (@VehicleId, @CustomerId, @Rating, @Comment, @today);
            SELECT LAST_INSERT_ID();",
            new { r.VehicleId, r.CustomerId, r.Rating, r.Comment, today });
        return new ReviewDto(id, r.VehicleId, r.CustomerId, r.Rating, r.Comment, today.ToString("yyyy-MM-dd"));
    }
}

public class PaymentRepository
{
    private readonly IDbConnectionFactory _db;
    public PaymentRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PaymentDto> RecordAsync(PaymentRequest req)
    {
        using var conn = _db.Create();
        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Payment (BookingId, Amount, Method, Status) VALUES (@BookingId, @Amount, @Method, 'Success');
            SELECT LAST_INSERT_ID();", req);
        return new PaymentDto(id, "Success", req.BookingId, req.Amount, req.Method);
    }

    // For the admin dashboard: what has actually been collected, per vehicle/customer.
    public async Task<IEnumerable<PaymentRecordDto>> ListAllAsync()
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<PaymentRecordDto>(@"
            SELECT p.PaymentId AS PaymentId, p.BookingId AS BookingId, v.VehicleId AS VehicleId,
                   v.Name AS VehicleName, c.CustomerId AS CustomerId, c.Name AS CustomerName,
                   p.Amount AS Amount, p.Method AS Method, DATE_FORMAT(p.PaidAt, '%Y-%m-%d %H:%i') AS PaidAt
            FROM Payment p
            JOIN Booking b ON b.BookingId = p.BookingId
            JOIN Vehicle v ON v.VehicleId = b.VehicleId
            JOIN Customer c ON c.CustomerId = b.CustomerId
            ORDER BY p.PaymentId DESC");
        return rows;
    }
}

public class VehicleLocationRepository
{
    private readonly IDbConnectionFactory _db;
    public VehicleLocationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<VehicleLocationDto?> GetAsync(int vehicleId)
    {
        using var conn = _db.Create();
        var loc = await conn.QuerySingleOrDefaultAsync<VehicleLocation>(
            "SELECT * FROM VehicleLocation WHERE VehicleId = @vehicleId", new { vehicleId });
        return loc is null ? null : new VehicleLocationDto(loc.VehicleId, loc.Lat, loc.Lng, loc.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    public async Task UpsertAsync(int vehicleId, double lat, double lng)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(@"
            INSERT INTO VehicleLocation (VehicleId, Lat, Lng) VALUES (@vehicleId, @lat, @lng)
            ON DUPLICATE KEY UPDATE Lat = @lat, Lng = @lng, UpdatedAt = CURRENT_TIMESTAMP",
            new { vehicleId, lat, lng });
    }
}

public class NotificationRepository
{
    private readonly IDbConnectionFactory _db;
    public NotificationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<NotificationDto>> ListForCustomerAsync(int customerId)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Notification>(
            "SELECT * FROM Notification WHERE CustomerId = @customerId ORDER BY NotificationId DESC", new { customerId });
        return rows.Select(n => new NotificationDto(n.NotificationId, n.CustomerId, n.Message, n.Read, n.Date.ToString("yyyy-MM-dd")));
    }
}

// AI results are written into these tables by the Python batch jobs;
// this API only reads them back out for the frontend.
public class AiRepository
{
    private readonly IDbConnectionFactory _db;
    public AiRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<RecommendationDto>> RecommendationsAsync(int customerId)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<Recommendation>(
            "SELECT VehicleId, Reason FROM Recommendation WHERE CustomerId = @customerId ORDER BY RecommendationId DESC LIMIT 10",
            new { customerId });
        return rows.Select(r => new RecommendationDto(r.VehicleId, r.Reason));
    }

    public async Task<IEnumerable<DemandForecastDto>> DemandForecastAsync()
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync<DemandForecast>(
            "SELECT Type, City, Week, PredictedDemand FROM DemandForecast ORDER BY Week DESC LIMIT 20");
        return rows.Select(f => new DemandForecastDto(f.Type, f.City, f.Week, f.PredictedDemand));
    }
}
