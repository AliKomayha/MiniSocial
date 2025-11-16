using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniSocial.Models;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace MiniSocial.ApiControllers
{
    [Route("api/[controller]")]
    //[Route("api/[controller]/[action]")] and no need to put the [HttpPost("signup")] only [HttpPost]
    [ApiController]
    public class AuthApiController : ControllerBase
    {


        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public AuthApiController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _config = config;
        }


        [HttpPost("signup")]
        public IActionResult Signup([FromBody] UserSignupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return BadRequest(new { message = "All fields are required." });
            }

            var hashed = BCrypt.Net.BCrypt.HashPassword(request.Password);

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                var command = new SqlCommand(@"
                     INSERT INTO Users (UserName, Email, PasswordHash, IsActive) 
                                OUTPUT INSERTED.Id
                                VALUES (@UserName, @Email, @PasswordHash, @IsActive)
                ", connection, transaction);


                command.Parameters.AddWithValue("@UserName", request.Username);
                command.Parameters.AddWithValue("@Email", request.Email);
                command.Parameters.AddWithValue("@PasswordHash", hashed);
                command.Parameters.AddWithValue("@IsActive", 1);

                int newUserId = (int)command.ExecuteScalar();

                command = new SqlCommand(@"
                    INSERT INTO Profiles (UserId, DisplayName, IsPrivate)
                        VALUES(@UserId, @DisplayName, @IsPrivate)
                    ", connection, transaction);

                command.Parameters.AddWithValue("@UserId", newUserId);
                command.Parameters.AddWithValue("@DisplayName", request.DisplayName);
                command.Parameters.AddWithValue("@IsPrivate", 0);

                command.ExecuteNonQuery();

                transaction.Commit();

                return Ok(new
                {
                    message = "Signup successful",
                    userId = newUserId,
                    username = request.Username
                });


            }
            catch
            {
                transaction.Rollback();
                return StatusCode(500, new { message = "An error occurred during signup." });
            }

        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLoginRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                Select Id, UserName, SecurityStamp, PasswordHash, IsActive 
                From Users 
                WHERE UserName=@Name", connection);
            command.Parameters.AddWithValue("@Name", request.Username);
            var reader = command.ExecuteReader();

            if (!reader.Read())
                return Unauthorized(new { message = "Invalid username or password" });

            var userId = reader.GetInt32(0);
            var userName = reader.GetString(1);
            var securityStamp = reader.GetString(2);
            var passwordHash = reader.GetString(3);
            var isActive = reader.GetBoolean(4);

            if (!isActive)
                return Unauthorized(new { message = "User is inactive" });

            if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
                return Unauthorized(new { message = "Invalid username or password" });

            var token = GenerateJwtToken(userId, userName);


            return Ok(new { Message = "Login Successful",
                Token = token,
                UserId = userId,
                UserName = userName
            });
        }

        private string GenerateJwtToken(int userId, string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim("userId", userId.ToString())


            };

            var token = new JwtSecurityToken(
               issuer: _config["Jwt:Issuer"],
               claims: claims,
               expires: DateTime.UtcNow.AddDays(7),
               signingCredentials: creds
           );


            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {

            return Ok(new { message = "Logout Successful" });
        }


    }


    public class UserLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    //public class LoginResponse
    //{
    //    public string Message { get; set; }
    //    public string Token { get; set; }
    //    public int UserId { get; set; }
    //    public string UserName { get; set; }
    //}

    public class UserSignupRequest
    {
        public string Username { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; }
        public string? DisplayName { get; set; }
    }
}
