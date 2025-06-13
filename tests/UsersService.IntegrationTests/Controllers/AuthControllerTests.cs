using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using UsersService.Models;
using Xunit;
using Microsoft.Extensions.Configuration;
using Shared.Models;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using MongoDB.Driver;

namespace UsersService.IntegrationTests.Controllers
{
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly IMongoDatabase _database;

        public AuthControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Get MongoDB configuration from test settings
            var configuration = factory.Services.GetRequiredService<IConfiguration>();
            var connectionString = configuration["MongoDB:ConnectionString"];
            var databaseName = configuration["MongoDB:DatabaseName"] + "_Test"; // Use a test database

            // Connect to MongoDB and get the database
            var mongoClient = new MongoClient(connectionString);
            _database = mongoClient.GetDatabase(databaseName);
            
            // Clean the users collection before each test
            _database.GetCollection<User>("Users").DeleteMany(Builders<User>.Filter.Empty);
        }

        [Fact]
        public async Task Register_WithValidData_ReturnsOk()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(registerRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<ApiResponse<object>>(responseString);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(responseObject.Success);
            Assert.Equal("Inscription réussie. Vous pouvez maintenant vous connecter.", responseObject.Message);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerRequest1 = new RegisterRequest
            {
                Username = "testuser1",
                Email = "duplicate@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var registerRequest2 = new RegisterRequest
            {
                Username = "testuser2",
                Email = "duplicate@example.com", // Same email
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var content1 = new StringContent(
                JsonConvert.SerializeObject(registerRequest1),
                Encoding.UTF8,
                "application/json");

            var content2 = new StringContent(
                JsonConvert.SerializeObject(registerRequest2),
                Encoding.UTF8,
                "application/json");

            // Act
            var response1 = await _client.PostAsync("/api/auth/register", content1);
            var response2 = await _client.PostAsync("/api/auth/register", content2);

            // Assert
            response1.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

            var responseString = await response2.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<ApiResponse<object>>(responseString);
            
            Assert.False(responseObject.Success);
            Assert.Equal("Cet email est déjà utilisé.", responseObject.Message);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsToken()
        {
            // Arrange - First register a user
            var registerRequest = new RegisterRequest
            {
                Username = "logintest",
                Email = "login@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var registerContent = new StringContent(
                JsonConvert.SerializeObject(registerRequest),
                Encoding.UTF8,
                "application/json");

            await _client.PostAsync("/api/auth/register", registerContent);

            // Arrange - Now try to login
            var loginRequest = new LoginRequest
            {
                Email = "login@example.com",
                Password = "Password123!"
            };

            var loginContent = new StringContent(
                JsonConvert.SerializeObject(loginRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);

            // Assert
            loginResponse.EnsureSuccessStatusCode();
            var responseString = await loginResponse.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<ApiResponse<TokenResponse>>(responseString);
            
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            Assert.True(responseObject.Success);
            Assert.Equal("Connexion réussie", responseObject.Message);
            Assert.NotNull(responseObject.Data);
            Assert.NotNull(responseObject.Data.Token);
            Assert.NotNull(responseObject.Data.RefreshToken);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword123!"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(loginRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<ApiResponse<object>>(responseString);
            
            Assert.False(responseObject.Success);
            Assert.Equal("Email ou mot de passe incorrect.", responseObject.Message);
        }
        
        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsNewToken()
        {
            // Arrange - Register and login a user first to get a refresh token
            var registerRequest = new RegisterRequest
            {
                Username = "refreshtest",
                Email = "refresh@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var registerContent = new StringContent(
                JsonConvert.SerializeObject(registerRequest),
                Encoding.UTF8,
                "application/json");

            await _client.PostAsync("/api/auth/register", registerContent);

            var loginRequest = new LoginRequest
            {
                Email = "refresh@example.com",
                Password = "Password123!"
            };

            var loginContent = new StringContent(
                JsonConvert.SerializeObject(loginRequest),
                Encoding.UTF8,
                "application/json");

            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
            var loginResponseObject = JsonConvert.DeserializeObject<ApiResponse<TokenResponse>>(loginResponseString);
            
            // Extract the refresh token
            var refreshToken = loginResponseObject.Data.RefreshToken;
            
            // Arrange - Send refresh token request
            var refreshRequest = new RefreshTokenRequest
            {
                RefreshToken = refreshToken
            };
            
            var refreshContent = new StringContent(
                JsonConvert.SerializeObject(refreshRequest),
                Encoding.UTF8,
                "application/json");
                
            // Act
            var refreshResponse = await _client.PostAsync("/api/auth/refresh-token", refreshContent);
            
            // Assert
            refreshResponse.EnsureSuccessStatusCode();
            var refreshResponseString = await refreshResponse.Content.ReadAsStringAsync();
            var refreshResponseObject = JsonConvert.DeserializeObject<ApiResponse<TokenResponse>>(refreshResponseString);
            
            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
            Assert.True(refreshResponseObject.Success);
            Assert.Equal("Token rafraîchi avec succès", refreshResponseObject.Message);
            Assert.NotNull(refreshResponseObject.Data);
            Assert.NotNull(refreshResponseObject.Data.Token);
            Assert.NotNull(refreshResponseObject.Data.RefreshToken);
            Assert.NotEqual(refreshToken, refreshResponseObject.Data.RefreshToken);
        }
        
        [Fact]
        public async Task ResetPasswordRequest_WithValidEmail_ReturnsOk()
        {
            // Arrange - Register a user first
            var registerRequest = new RegisterRequest
            {
                Username = "resettest",
                Email = "reset@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var registerContent = new StringContent(
                JsonConvert.SerializeObject(registerRequest),
                Encoding.UTF8,
                "application/json");

            await _client.PostAsync("/api/auth/register", registerContent);
            
            // Arrange - Send reset password request
            var resetRequest = new ResetPasswordRequestDto
            {
                Email = "reset@example.com"
            };
            
            var resetContent = new StringContent(
                JsonConvert.SerializeObject(resetRequest),
                Encoding.UTF8,
                "application/json");
                
            // Act
            var response = await _client.PostAsync("/api/auth/reset-password-request", resetContent);
            
            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<ApiResponse<object>>(responseString);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(responseObject.Success);
            Assert.Equal("Si l'email existe dans notre base de données, un email de réinitialisation a été envoyé.", 
                responseObject.Message);
        }
    }
}
