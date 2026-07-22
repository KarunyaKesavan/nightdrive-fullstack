using Microsoft.AspNetCore.Mvc;
using VehicleRentalApi.Dtos;
using VehicleRentalApi.Repositories;
using VehicleRentalApi.Services;

namespace VehicleRentalApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly CustomerRepository _customers;
    private readonly TokenService _tokens;

    public AuthController(CustomerRepository customers, TokenService tokens)
    {
        _customers = customers;
        _tokens = tokens;
    }

    // Frontend calls: POST /auth/login { email, password }
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        // Admin accounts are seeded separately (see schema.sql) — checked first
        var admin = await _customers.FindAdminByEmailAsync(req.Email);
        if (admin is not null && BCrypt.Net.BCrypt.Verify(req.Password, admin.PasswordHash))
        {
            var adminToken = _tokens.Generate(admin.AdminId, admin.Name, admin.Email, "Admin");
            return Ok(new AuthResponse(adminToken, new UserDto(admin.AdminId, admin.Name, admin.Email, "Admin")));
        }

        var customer = await _customers.FindByEmailAsync(req.Email);
        if (customer is null || !BCrypt.Net.BCrypt.Verify(req.Password, customer.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _tokens.Generate(customer.CustomerId, customer.Name, customer.Email, "Customer");
        return Ok(new AuthResponse(token, new UserDto(customer.CustomerId, customer.Name, customer.Email, "Customer")));
    }

    // Frontend calls: POST /auth/register { name, email, password, role }
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var role = string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Customer";
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        if (role == "Admin")
        {
            if (await _customers.FindAdminByEmailAsync(req.Email) is not null)
                return Conflict(new { message = "An admin account with that email already exists." });

            var admin = await _customers.CreateAdminAsync(req.Name, req.Email, hash);
            var adminToken = _tokens.Generate(admin.AdminId, admin.Name, admin.Email, "Admin");
            return Ok(new AuthResponse(adminToken, new UserDto(admin.AdminId, admin.Name, admin.Email, "Admin")));
        }

        if (await _customers.FindByEmailAsync(req.Email) is not null)
            return Conflict(new { message = "An account with that email already exists." });

        var customer = await _customers.CreateAsync(req.Name, req.Email, hash);
        var token = _tokens.Generate(customer.CustomerId, customer.Name, customer.Email, "Customer");
        return Ok(new AuthResponse(token, new UserDto(customer.CustomerId, customer.Name, customer.Email, "Customer")));
    }
}
