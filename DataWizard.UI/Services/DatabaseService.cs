using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataWizard.UI.Services
{
    public class DatabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;

        public DatabaseService()
        {
            _supabaseUrl = "https://rrlmejrtlqnfaavyrrtf.supabase.co";
            _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InJybG1lanJ0bHFuZmFhdnlycnRmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDgyMzI5NzUsImV4cCI6MjA2MzgwODk3NX0.8uC7og_bfk2C-Ok6KNGAY5Ej-nz_wBz07-94BG1rUZY";

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }

        public async Task<(bool success, UserData user, string error)> ValidateUserCredentialsAsync(string username, string password)
        {
            try
            {
                Debug.WriteLine($"Attempting login for user: {username}");

                var payload = new
                {
                    p_username = username,
                    p_password = password
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                Debug.WriteLine($"Login payload: {jsonContent}");

                var response = await _httpClient.PostAsync(
                    $"{_supabaseUrl}/rest/v1/rpc/fn_user_login",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );

                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Login API Response Status: {response.StatusCode}");
                Debug.WriteLine($"Response content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var loginResults = JsonSerializer.Deserialize<List<LoginResult>>(content, options);
                    Debug.WriteLine($"Deserialized results: {JsonSerializer.Serialize(loginResults, options)}");

                    var result = loginResults?.FirstOrDefault();
                    if (result != null)
                    {
                        Debug.WriteLine($"Login status: {result.LoginStatus}");
                        Debug.WriteLine($"User ID: {result.Id}");

                        if (result.LoginStatus == 0 && result.Id != null)
                        {
                            // Explicitly update last_login_at if needed
                            await UpdateLastLoginAsync(result.Id.Value);

                            Debug.WriteLine("Login successful");
                            return (true, new UserData
                            {
                                UserId = result.Id.Value,
                                Username = result.Username,
                                Email = result.Email,
                                FullName = result.FullName
                            }, null);
                        }
                    }

                    Debug.WriteLine("Login failed - invalid credentials");
                    return (false, null, "Invalid username or password");
                }

                Debug.WriteLine($"Login failed - HTTP {response.StatusCode}");
                return (false, null, $"Login failed: {content}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login Error: {ex}");
                return (false, null, $"Database error: {ex.Message}");
            }
        }

        private async Task UpdateLastLoginAsync(Guid userId)
        {
            try
            {
                var updatePayload = new { last_login_at = DateTime.UtcNow };
                var jsonContent = JsonSerializer.Serialize(updatePayload);

                var response = await _httpClient.PatchAsync(
                    $"{_supabaseUrl}/rest/v1/users?id=eq.{userId}",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );

                Debug.WriteLine($"Update last_login_at response: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Failed to update last_login_at: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating last_login_at: {ex.Message}");
            }
        }

        public async Task<(bool success, string error)> CreateUserAsync(string username, string password, string email, string fullName)
        {
            try
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                var checkResponse = await _httpClient.PostAsync(
                    $"{_supabaseUrl}/rest/v1/rpc/fn_check_user_exists",
                    new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            p_username = username,
                            p_email = email
                        }),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

                if (checkResponse.IsSuccessStatusCode)
                {
                    var content = await checkResponse.Content.ReadAsStringAsync();
                    var userExists = JsonSerializer.Deserialize<bool>(content);

                    if (userExists)
                    {
                        return (false, "Username or email already exists");
                    }
                }
                else
                {
                    var errorContent = await checkResponse.Content.ReadAsStringAsync();
                    return (false, $"Duplicate check failed: {errorContent}");
                }

                var newUser = new
                {
                    username = username,
                    password_hash = hashedPassword,
                    email = email,
                    full_name = fullName ?? string.Empty
                };

                var response = await _httpClient.PostAsync(
                    $"{_supabaseUrl}/rest/v1/users",
                    new StringContent(
                        JsonSerializer.Serialize(newUser),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (false, $"Failed to create user: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
        }

        

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class LoginResult
    {
        public Guid? Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public int LoginStatus { get; set; }
    }

    public class UserData
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
    }

    
}
