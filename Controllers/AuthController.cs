using System.Security.Claims;
using System.Transactions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MiniSocial.Models;

namespace MiniSocial.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }


        // ceate user
        [HttpGet]
        public IActionResult Signup() => View();
        [HttpPost]
        public IActionResult Signup(User user, Profile profile)
        {

            if (ModelState.IsValid)
            {
                var hashed = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert into Users
                            var command = new SqlCommand(@"
                                INSERT INTO Users (UserName, Email, PasswordHash, IsActive) 
                                OUTPUT INSERTED.Id
                                VALUES (@UserName, @Email, @PasswordHash, @IsActive)", connection, transaction);

                            command.Parameters.AddWithValue("@UserName", user.Username);
                            command.Parameters.AddWithValue("@Email", user.Email);
                            command.Parameters.AddWithValue("@PasswordHash", hashed);
                            command.Parameters.AddWithValue("@IsActive", 1);
                            
                            int newUserId = (int)command.ExecuteScalar(); // get user id created 

                            // Insert into Profiles

                            command = new SqlCommand(@" 
                                INSERT INTO Profiles (UserId, DisplayName, IsPrivate)
                                   
                                VALUES (@UserId, @DisplayName, @IsPrivate)", connection, transaction);

                            command.Parameters.AddWithValue("@UserId", newUserId);
                            command.Parameters.AddWithValue("@DisplayName",  profile.DisplayName);
                            command.Parameters.AddWithValue("@IsPrivate", 0);

                            command.ExecuteNonQuery();

                            transaction.Commit();

                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                    
                }
                return RedirectToAction("Index", "Home");

            }
            return View(user);

        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string name, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("Select Id, UserName, SecurityStamp, PasswordHash, IsActive From Users WHERE UserName=@Name", connection);
            command.Parameters.AddWithValue("@Name", name);
            var reader = command.ExecuteReader();


            if (!reader.Read())
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View();
            }

            var userId = reader.GetInt32(0);
            var userName = reader.GetString(1);
            
            var securityStamp = reader.GetString(2);
            var passwordHash = reader.GetString(3);
            var isActive = reader.GetBoolean(4);


            if (!isActive)
            {
                ModelState.AddModelError("", "User is inactive");
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
            {
                ModelState.AddModelError("", "Invalid name or password");
                return View();
            }



            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim("SecurityStamp", securityStamp ),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    
    }//class
}
