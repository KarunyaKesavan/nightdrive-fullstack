using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleRentalApi.Dtos;
using VehicleRentalApi.Repositories;

namespace VehicleRentalApi.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleRepository _vehicles;
    public VehiclesController(VehicleRepository vehicles) => _vehicles = vehicles;

    // Frontend calls: GET /vehicles?type=&locationId=&maxPrice=&search=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> List(
        [FromQuery] string? type, [FromQuery] int? locationId,
        [FromQuery] decimal? maxPrice, [FromQuery] string? search)
        => Ok(await _vehicles.ListAsync(type, locationId, maxPrice, search));

    [HttpGet("{id}")]
    public async Task<ActionResult<VehicleDto>> Get(int id)
    {
        var v = await _vehicles.GetAsync(id);
        return v is null ? NotFound() : Ok(v);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create(VehicleUpsertRequest req)
        => Ok(await _vehicles.CreateAsync(req));

    // Frontend sends partial updates, e.g. { status: "Maintenance" } from the admin dashboard,
    // so this accepts a raw JSON object rather than a strict DTO.
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<VehicleDto>> Update(int id, [FromBody] JsonElement body)
    {
        var fields = new Dictionary<string, object?>();
        foreach (var prop in body.EnumerateObject())
        {
            object? value = prop.Value.ValueKind switch
            {
                JsonValueKind.Number => prop.Value.GetDecimal(),
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.True or JsonValueKind.False => prop.Value.GetBoolean(),
                _ => null,
            };
            fields[prop.Name] = value;
        }
        var updated = await _vehicles.UpdateAsync(id, fields);
        return updated is null ? NotFound() : Ok(updated);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(int id)
    {
        await _vehicles.RemoveAsync(id);
        return NoContent();
    }
}
