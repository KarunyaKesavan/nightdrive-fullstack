using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleRentalApi.Data;
using VehicleRentalApi.Dtos;
using VehicleRentalApi.Repositories;
using Dapper;

namespace VehicleRentalApi.Controllers;

// ---------------- Payments ----------------
// Mock payment gateway: records the payment as an immediate success.
// Swap the body of Pay() for a real gateway call (Razorpay/Stripe test mode) later —
// the frontend contract (POST /payments -> { paymentId, status, ...payload }) won't need to change.
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly PaymentRepository _payments;
    public PaymentsController(PaymentRepository payments) => _payments = payments;

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Pay(PaymentRequest req)
        => Ok(await _payments.RecordAsync(req));
}

// ---------------- Reviews ----------------
[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly ReviewRepository _reviews;
    private readonly VehicleRepository _vehicles;
    public ReviewsController(ReviewRepository reviews, VehicleRepository vehicles)
    {
        _reviews = reviews;
        _vehicles = vehicles;
    }

    [HttpGet("vehicle/{vehicleId}")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> ListForVehicle(int vehicleId)
        => Ok(await _reviews.ListForVehicleAsync(vehicleId));

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(ReviewCreateRequest req)
    {
        var review = await _reviews.CreateAsync(req);
        await _vehicles.RecomputeRatingAsync(req.VehicleId); // keeps the star rating shown on cards accurate
        return Ok(review);
    }
}

// ---------------- Notifications ----------------
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationRepository _notifications;
    public NotificationsController(NotificationRepository notifications) => _notifications = notifications;

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> ListForCustomer(int customerId)
        => Ok(await _notifications.ListForCustomerAsync(customerId));
}

// ---------------- AI (reads what the Python batch jobs have written) ----------------
[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly AiRepository _ai;
    public AiController(AiRepository ai) => _ai = ai;

    // Frontend calls this with no params; the customer is taken from the JWT if logged in.
    [HttpGet("recommendations")]
    public async Task<ActionResult<IEnumerable<RecommendationDto>>> Recommendations()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (sub is null || !int.TryParse(sub, out var customerId))
            return Ok(Array.Empty<RecommendationDto>());
        return Ok(await _ai.RecommendationsAsync(customerId));
    }

    [HttpGet("demand-forecast")]
    public async Task<ActionResult<IEnumerable<DemandForecastDto>>> DemandForecast()
        => Ok(await _ai.DemandForecastAsync());
}

// ---------------- Admin dashboard stats ----------------
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    public AdminController(IDbConnectionFactory db) => _db = db;

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> Stats()
    {
        using var conn = _db.Create();
        var totalVehicles = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Vehicle");
        var available = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Vehicle WHERE Status = 'Available'");
        var booked = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Vehicle WHERE Status = 'Booked'");
        var maintenance = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Vehicle WHERE Status = 'Maintenance'");
        var totalBookings = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Booking");
        var revenue = await conn.ExecuteScalarAsync<decimal?>("SELECT SUM(TotalAmount) FROM Booking WHERE Status <> 'Cancelled'") ?? 0;

        return Ok(new AdminStatsDto(totalVehicles, available, booked, maintenance, totalBookings, revenue));
    }
}

// ---------------- Admin: payments collected ----------------
[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController : ControllerBase
{
    private readonly PaymentRepository _payments;
    public AdminPaymentsController(PaymentRepository payments) => _payments = payments;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentRecordDto>>> List()
        => Ok(await _payments.ListAllAsync());
}

// ---------------- Live vehicle tracking (OpenStreetMap on the frontend) ----------------
// The frontend polls this every few seconds while a vehicle is Booked and
// draws the marker on a Leaflet/OpenStreetMap map. Position itself is a
// mock GPS feed simulated server-side — see Services/VehicleTrackingSimulator.cs.
[ApiController]
[Route("api/vehicles/{vehicleId}/location")]
public class VehicleLocationController : ControllerBase
{
    private readonly VehicleLocationRepository _locations;
    public VehicleLocationController(VehicleLocationRepository locations) => _locations = locations;

    [HttpGet]
    public async Task<ActionResult<VehicleLocationDto>> Get(int vehicleId)
    {
        var loc = await _locations.GetAsync(vehicleId);
        return loc is null ? NotFound() : Ok(loc);
    }
}
