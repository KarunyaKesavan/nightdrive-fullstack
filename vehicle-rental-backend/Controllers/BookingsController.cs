using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleRentalApi.Dtos;
using VehicleRentalApi.Repositories;

namespace VehicleRentalApi.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly BookingRepository _bookings;
    public BookingsController(BookingRepository bookings) => _bookings = bookings;

    // Frontend calls: GET /bookings/customer/{customerId}
    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> ListForCustomer(int customerId)
        => Ok(await _bookings.ListForCustomerAsync(customerId));

    // Frontend calls: GET /bookings  (admin fleet-wide view)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDto>>> ListAll()
        => Ok(await _bookings.ListAllAsync());

    // Frontend calls: POST /bookings
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(BookingCreateRequest req)
        => Ok(await _bookings.CreateAsync(req));

    // Frontend calls: POST /bookings/{id}/cancel
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _bookings.CancelAsync(id);
        return NoContent();
    }
}
