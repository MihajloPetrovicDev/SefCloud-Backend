using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using SefCloud.Backend.Models;
using SefCloud.Backend.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SefCloud.Backend.Services;
using SefCloud.Backend.Data;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;
    private readonly TokenService _tokenService;
    private readonly ApplicationDbContext _context;


    public AuthController(UserManager<ApplicationUser> userManager,
                          SignInManager<ApplicationUser> signInManager,
                          IConfiguration configuration,
                          AuthService authService,
                          ApplicationDbContext context,
                          TokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _authService = authService;
        _context = context;
        _tokenService = tokenService;
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var user = await _userManager.FindByEmailAsync(loginRequest.Email);

        if (user == null || !(await _userManager.CheckPasswordAsync(user, loginRequest.Password)))
        {
            return Unauthorized();
        }

        var token = _tokenService.GenerateJwtToken(user);
        return Ok(new { Token = token });
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        if (registerRequest.Password != registerRequest.ConfirmPassword)
        {
            return BadRequest("Passwords don't match.");
        }

        var newUser = new ApplicationUser();
        newUser.UserName = registerRequest.Username;
        newUser.Email = registerRequest.Email;

        var result = await _userManager.CreateAsync(newUser, registerRequest.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = "User creation failed", errors = result.Errors });
        }

        return await Login(new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password,
        });
    }


    [HttpPost("check-token")]
    public IActionResult CheckToken()
    {
        var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token))
            return Ok(new { shouldDeleteToken = false });

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
            }, out SecurityToken validatedToken);

            return Ok(new { shouldDeleteToken = false });
        }
        catch (SecurityTokenException)
        {
            return Ok(new { shouldDeleteToken = true });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [HttpPost("validate-token")]
    public IActionResult ValidateToken()
    {
        var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token))
        {
            return Ok(new { valid = false });
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
            }, out SecurityToken validatedToken);

            return Ok(new { valid = true });
        }
        catch (Exception ex)
        {
            return Ok(new { valid = false, error = ex });
        }
    }


    [HttpGet("get-user-info")]
    public async Task<IActionResult> GetUserInfo()
    {
        var authHeaderCheck = _authService.ValidateAuthorizationHeader(Request.Headers["Authorization"]);

        if (authHeaderCheck.IsValid == false)
        {
            return Unauthorized(new { success = false });
        }

        var user = await _context.AspNetUsers
            .Where(u => u.Id == authHeaderCheck.user.Id)
            .Select(u => new
            {
                UserId = u.Id,
                UserName = u.UserName,
                Email = u.Email,
            })
            .FirstOrDefaultAsync();

        return Ok (new { success = true, user = user });
    }
}