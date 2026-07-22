using Dapper;
using VehicleRentalApi.Data;
using VehicleRentalApi.Repositories;

namespace VehicleRentalApi.Services;

// Mock "live tracking" feed: every 5 seconds, nudges each Booked vehicle's
// coordinates a small random amount from wherever it currently is (starting
// from its pickup Location the first time it's seen). This is what
// GET /api/vehicles/{id}/location reports, which the frontend polls to draw
// a moving marker on the OpenStreetMap/Leaflet map.
//
// Swap this out for a real GPS/telematics ingestion endpoint later —
// VehicleLocationRepository.UpsertAsync is the only thing that needs calling.
public class VehicleTrackingSimulator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Random _random = new();

    public VehicleTrackingSimulator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync();
            }
            catch
            {
                // Best-effort simulation — a transient DB hiccup shouldn't crash the app.
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task TickAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        var locations = scope.ServiceProvider.GetRequiredService<VehicleLocationRepository>();

        using var conn = dbFactory.Create();
        var bookedVehicles = await conn.QueryAsync<(int VehicleId, int PickupLocationId)>(@"
            SELECT DISTINCT v.VehicleId, v.LocationId AS PickupLocationId
            FROM Vehicle v
            WHERE v.Status = 'Booked'");

        foreach (var (vehicleId, pickupLocationId) in bookedVehicles)
        {
            var current = await locations.GetAsync(vehicleId);
            double lat, lng;
            if (current is null)
            {
                var pickup = await conn.QuerySingleAsync<(double Lat, double Lng)>(
                    "SELECT Lat, Lng FROM Location WHERE LocationId = @id", new { id = pickupLocationId });
                lat = pickup.Lat;
                lng = pickup.Lng;
            }
            else
            {
                // Small random walk — roughly city-block scale per tick.
                lat = current.Lat + (_random.NextDouble() - 0.5) * 0.004;
                lng = current.Lng + (_random.NextDouble() - 0.5) * 0.004;
            }

            await locations.UpsertAsync(vehicleId, lat, lng);
        }
    }
}
