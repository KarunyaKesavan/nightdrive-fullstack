using Dapper;
using VehicleRentalApi.Data;

namespace VehicleRentalApi.Seed;

// Runs once at startup and only inserts data if the tables are empty.
// Mirrors src/services/api.js's mock dataset so the app looks the same
// the moment the frontend switches from USE_MOCK=true to a real API call.
public static class DevSeeder
{
    public static async Task RunAsync(IDbConnectionFactory dbFactory, ILogger logger)
    {
        using var conn = dbFactory.Create();

        var adminCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Admin");
        if (adminCount == 0)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("password123");
            await conn.ExecuteAsync(
                "INSERT INTO Admin (Name, Email, PasswordHash) VALUES ('Site Admin', 'admin@nightdrive.com', @hash)",
                new { hash });
            logger.LogInformation("Seeded default admin: admin@nightdrive.com / password123");
        }

        var customerCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Customer");
        int customerId;
        if (customerCount == 0)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("password123");
            customerId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO Customer (Name, Email, PasswordHash) VALUES ('Arun Kumar', 'arun@example.com', @hash);
                SELECT LAST_INSERT_ID();", new { hash });
            logger.LogInformation("Seeded demo customer: arun@example.com / password123");
        }
        else
        {
            customerId = await conn.ExecuteScalarAsync<int>("SELECT CustomerId FROM Customer ORDER BY CustomerId LIMIT 1");
        }

        var locationCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Location");
        if (locationCount == 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO Location (City, Area, Lat, Lng) VALUES
                ('Coimbatore', 'RS Puram', 11.0068, 76.9558),
                ('Coimbatore', 'Peelamedu', 11.0296, 77.0266),
                ('Chennai', 'T Nagar', 13.0418, 80.2341),
                ('Bengaluru', 'Indiranagar', 12.9716, 77.6412);");
        }

        var vehicleCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Vehicle");
        if (vehicleCount == 0)
        {
            var loc = (await conn.QueryAsync<(int LocationId, string City)>("SELECT LocationId, City FROM Location")).ToList();
            int LocId(string city) => loc.First(l => l.City == city).LocationId;

            await conn.ExecuteAsync(@"
                INSERT INTO Vehicle (Name, Type, Brand, PricePerDay, Seats, Transmission, Fuel, Rating, Status, LocationId, Image, FeaturesCsv) VALUES
                ('Tesla Model 3', 'EV', 'Tesla', 6500, 5, 'Automatic', 'Electric', 4.8, 'Available', @bengaluru, '🚗', 'Autopilot,Fast Charging,Premium Audio'),
                ('Mahindra Thar', 'Premium', 'Mahindra', 4200, 4, 'Manual', 'Diesel', 4.6, 'Available', @coimbatore, '🚙', '4x4,Convertible Top'),
                ('Honda Activa', 'Bike', 'Honda', 500, 2, 'Automatic', 'Petrol', 4.4, 'Booked', @coimbatore, '🛵', 'Fuel Efficient'),
                ('Hyundai Creta', 'Car', 'Hyundai', 3200, 5, 'Automatic', 'Diesel', 4.5, 'Available', @coimbatore, '🚗', 'Sunroof,Cruise Control'),
                ('Royal Enfield Classic 350', 'Bike', 'Royal Enfield', 900, 2, 'Manual', 'Petrol', 4.7, 'Available', @chennai, '🏍️', 'Retro Styling'),
                ('Tata Nexon EV', 'EV', 'Tata', 3800, 5, 'Automatic', 'Electric', 4.3, 'Maintenance', @coimbatore, '🚗', 'Fast Charging,Connected App'),
                ('Toyota Fortuner', 'Premium', 'Toyota', 7200, 7, 'Automatic', 'Diesel', 4.9, 'Available', @bengaluru, '🚙', '7-Seater,4x4,Leather Seats'),
                ('Maruti Swift', 'Car', 'Maruti', 1800, 5, 'Manual', 'Petrol', 4.2, 'Available', @coimbatore, '🚗', 'Fuel Efficient,Compact');",
                new { bengaluru = LocId("Bengaluru"), coimbatore = LocId("Coimbatore"), chennai = LocId("Chennai") });

            logger.LogInformation("Seeded demo vehicle fleet ({Count} vehicles)", 8);

            var teslaId = await conn.ExecuteScalarAsync<int>("SELECT VehicleId FROM Vehicle WHERE Name = 'Tesla Model 3'");
            var activaId = await conn.ExecuteScalarAsync<int>("SELECT VehicleId FROM Vehicle WHERE Name = 'Honda Activa'");
            var cretaId = await conn.ExecuteScalarAsync<int>("SELECT VehicleId FROM Vehicle WHERE Name = 'Hyundai Creta'");
            var fortunerId = await conn.ExecuteScalarAsync<int>("SELECT VehicleId FROM Vehicle WHERE Name = 'Toyota Fortuner'");
            var pickupCoimbatore = LocId("Coimbatore");
            var pickupBengaluru = LocId("Bengaluru");

            await conn.ExecuteAsync(@"
                INSERT INTO Booking (CustomerId, VehicleId, StartDate, EndDate, Status, TotalAmount, PickupLocationId) VALUES
                (@customerId, @activaId, '2026-07-22', '2026-07-24', 'Confirmed', 1000, @pickupCoimbatore),
                (@customerId, @teslaId, '2026-06-10', '2026-06-12', 'Completed', 13000, @pickupBengaluru);",
                new { customerId, activaId, teslaId, pickupCoimbatore, pickupBengaluru });

            var activaBookingId = await conn.ExecuteScalarAsync<int>(
                "SELECT BookingId FROM Booking WHERE VehicleId = @activaId AND CustomerId = @customerId", new { activaId, customerId });
            var teslaBookingId = await conn.ExecuteScalarAsync<int>(
                "SELECT BookingId FROM Booking WHERE VehicleId = @teslaId AND CustomerId = @customerId", new { teslaId, customerId });

            await conn.ExecuteAsync(@"
                INSERT INTO Payment (BookingId, Amount, Method, Status) VALUES
                (@activaBookingId, 1000, 'Mock Card', 'Success'),
                (@teslaBookingId, 13000, 'Mock Card', 'Success');",
                new { activaBookingId, teslaBookingId });

            await conn.ExecuteAsync(@"
                INSERT INTO Review (VehicleId, CustomerId, Rating, Comment, Date) VALUES
                (@teslaId, @customerId, 5, 'Incredibly smooth ride, autopilot was a game changer on the highway.', '2026-06-13'),
                (@cretaId, @customerId, 4, 'Comfortable and spacious, great for family trips.', '2026-05-02');",
                new { teslaId, cretaId, customerId });

            await conn.ExecuteAsync(@"
                INSERT INTO Notification (CustomerId, Message, `Read`, Date) VALUES
                (@customerId, 'Your booking for Honda Activa is confirmed.', FALSE, '2026-07-19'),
                (@customerId, '20% off EVs this weekend — book now.', TRUE, '2026-07-15');",
                new { customerId });

            await conn.ExecuteAsync(@"
                INSERT INTO Recommendation (CustomerId, VehicleId, Reason) VALUES
                (@customerId, @fortunerId, 'Matches your preference for premium 4x4s'),
                (@customerId, @teslaId, 'Trending EV in your city');",
                new { customerId, fortunerId, teslaId });

            await conn.ExecuteAsync(@"
                INSERT INTO DemandForecast (Type, City, Week, PredictedDemand) VALUES
                ('EV', 'Coimbatore', '2026-W30', 34),
                ('Bike', 'Coimbatore', '2026-W30', 61),
                ('Premium', 'Chennai', '2026-W30', 22);");
        }
    }
}
