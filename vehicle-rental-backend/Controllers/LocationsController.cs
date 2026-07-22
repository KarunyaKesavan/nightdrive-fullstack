using Microsoft.AspNetCore.Mvc;
using VehicleRentalApi.Dtos;
using VehicleRentalApi.Repositories;

namespace VehicleRentalApi.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly LocationRepository _locations;
    public LocationsController(LocationRepository locations) => _locations = locations;

    // Frontend calls: GET /locations
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationDto>>> List()
        => Ok(await _locations.ListAsync());
}
