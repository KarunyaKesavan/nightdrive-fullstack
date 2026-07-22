using Dapper;
using VehicleRentalApi.Data;
using VehicleRentalApi.Dtos;
using VehicleRentalApi.Entities;

namespace VehicleRentalApi.Repositories;

public class VehicleRepository
{
    private readonly IDbConnectionFactory _db;
    public VehicleRepository(IDbConnectionFactory db) => _db = db;

    private static VehicleDto ToDto(Vehicle v) => new(
        v.VehicleId, v.Name, v.Type, v.Brand, v.PricePerDay, v.Seats, v.Transmission,
        v.Fuel, v.Rating, v.Status, v.LocationId, v.Image,
        v.FeaturesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    public async Task<IEnumerable<VehicleDto>> ListAsync(string? type, int? locationId, decimal? maxPrice, string? search)
    {
        using var conn = _db.Create();
        var sql = "SELECT * FROM Vehicle WHERE 1=1";
        var args = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(type)) { sql += " AND Type = @Type"; args.Add("Type", type); }
        if (locationId.HasValue) { sql += " AND LocationId = @LocationId"; args.Add("LocationId", locationId); }
        if (maxPrice.HasValue) { sql += " AND PricePerDay <= @MaxPrice"; args.Add("MaxPrice", maxPrice); }
        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += " AND (LOWER(Name) LIKE @Search OR LOWER(Brand) LIKE @Search)";
            args.Add("Search", $"%{search.ToLower()}%");
        }
        var rows = await conn.QueryAsync<Vehicle>(sql, args);
        return rows.Select(ToDto);
    }

    public async Task<VehicleDto?> GetAsync(int id)
    {
        using var conn = _db.Create();
        var v = await conn.QuerySingleOrDefaultAsync<Vehicle>(
            "SELECT * FROM Vehicle WHERE VehicleId = @id", new { id });
        return v is null ? null : ToDto(v);
    }

    public async Task<VehicleDto> CreateAsync(VehicleUpsertRequest r)
    {
        using var conn = _db.Create();
        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Vehicle (Name, Type, Brand, PricePerDay, Seats, Transmission, Fuel, Rating, Status, LocationId, Image, FeaturesCsv)
            VALUES (@Name, @Type, @Brand, @PricePerDay, @Seats, @Transmission, @Fuel, @Rating, @Status, @LocationId, @Image, @FeaturesCsv);
            SELECT LAST_INSERT_ID();",
            new { r.Name, r.Type, r.Brand, r.PricePerDay, r.Seats, r.Transmission, r.Fuel, r.Rating, r.Status, r.LocationId, r.Image, FeaturesCsv = string.Join(",", r.Features) });
        return (await GetAsync(id))!;
    }

    public async Task<VehicleDto?> UpdateAsync(int id, Dictionary<string, object?> fields)
    {
        if (fields.Count == 0) return await GetAsync(id);
        using var conn = _db.Create();
        var setClauses = new List<string>();
        var args = new DynamicParameters();
        args.Add("id", id);
        foreach (var (key, value) in fields)
        {
            var column = key switch
            {
                "status" => "Status",
                "pricePerDay" => "PricePerDay",
                "name" => "Name",
                "type" => "Type",
                "brand" => "Brand",
                "seats" => "Seats",
                "transmission" => "Transmission",
                "fuel" => "Fuel",
                "rating" => "Rating",
                "locationId" => "LocationId",
                "image" => "Image",
                _ => null,
            };
            if (column is null) continue;
            setClauses.Add($"{column} = @{column}");
            args.Add(column, value);
        }
        if (setClauses.Count == 0) return await GetAsync(id);
        var sql = $"UPDATE Vehicle SET {string.Join(", ", setClauses)} WHERE VehicleId = @id";
        await conn.ExecuteAsync(sql, args);
        return await GetAsync(id);
    }

    public async Task RemoveAsync(int id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("DELETE FROM Vehicle WHERE VehicleId = @id", new { id });
    }

    public async Task SetStatusAsync(int id, string status)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("UPDATE Vehicle SET Status = @status WHERE VehicleId = @id", new { id, status });
    }

    // Called after a new review is posted, so the star rating shown on the
    // vehicle card/detail page always reflects the real average — not a
    // static seeded number.
    public async Task RecomputeRatingAsync(int vehicleId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(@"
            UPDATE Vehicle SET Rating = COALESCE(
                (SELECT ROUND(AVG(Rating), 1) FROM Review WHERE VehicleId = @vehicleId), Rating)
            WHERE VehicleId = @vehicleId", new { vehicleId });
    }
}
