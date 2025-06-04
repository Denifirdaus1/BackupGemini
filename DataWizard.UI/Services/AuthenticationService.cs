using System;
using System.Threading.Tasks;

namespace DataWizard.UI.Services
{
    public class AuthenticationService
    {
        private readonly DatabaseService _dbService;
        private UserData _currentUser;

        public AuthenticationService()
        {
            _dbService = new DatabaseService();
        }

        public UserData CurrentUser => _currentUser;

        public event EventHandler<AuthenticationStateChangedEventArgs> AuthenticationStateChanged;

        public async Task<(bool success, string error)> SignInAsync(string username, string password)
        {
            try
            {
                var result = await _dbService.ValidateUserCredentialsAsync(username, password);

                if (result.success && result.user != null)
                {
                    _currentUser = result.user;
                    OnAuthenticationStateChanged(new AuthenticationStateChangedEventArgs(true, _currentUser));
                    return (true, null);
                }

                return (false, result.error);
            }
            catch (Exception ex)
            {
                return (false, $"Sign in failed: {ex.Message}");
            }
        }

        public async Task<(bool success, string error)> SignUpAsync(string username, string password, string email, string fullName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return (false, "Username is required");

                if (string.IsNullOrWhiteSpace(password))
                    return (false, "Password is required");

                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email is required");

                if (!IsValidEmail(email))
                    return (false, "Invalid email format");

                if (password.Length < 6)
                    return (false, "Password must be at least 6 characters long");

                var result = await _dbService.CreateUserAsync(username, password, email, fullName);
                return result;
            }
            catch (Exception ex)
            {
                return (false, $"Sign up failed: {ex.Message}");
            }
        }

        public void SignOut()
        {
            _currentUser = null;
            OnAuthenticationStateChanged(new AuthenticationStateChangedEventArgs(false, null));
        }

        public bool IsAuthenticated => _currentUser != null;

        public Guid? GetCurrentUserId()
        {
            return _currentUser?.UserId;
        }

        public string GetCurrentUsername()
        {
            return _currentUser?.Username;
        }

        public string GetCurrentUserEmail()
        {
            return _currentUser?.Email;
        }

        public string GetCurrentUserFullName()
        {
            return _currentUser?.FullName;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RefreshCurrentUserAsync()
        {
            if (_currentUser == null)
                return false;

            try
            {
                var result = await _dbService.ValidateUserCredentialsAsync(_currentUser.Username, "");
                if (result.success && result.user != null)
                {
                    _currentUser = result.user;
                    OnAuthenticationStateChanged(new AuthenticationStateChangedEventArgs(true, _currentUser));
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing user data: {ex.Message}");
            }

            return false;
        }

        public void UpdateCurrentUser(UserData updatedUser)
        {
            if (updatedUser != null && _currentUser != null && _currentUser.UserId == updatedUser.UserId)
            {
                _currentUser = updatedUser;
                OnAuthenticationStateChanged(new AuthenticationStateChangedEventArgs(true, _currentUser));
            }
        }

        protected virtual void OnAuthenticationStateChanged(AuthenticationStateChangedEventArgs e)
        {
            AuthenticationStateChanged?.Invoke(this, e);
        }

        public async Task<bool> ValidateCurrentSessionAsync()
        {
            if (_currentUser == null)
                return false;

            try
            {
                return true;
            }
            catch
            {
                SignOut();
                return false;
            }
        }

        public void Dispose()
        {
            _dbService?.Dispose();
        }
    }

    public class AuthenticationStateChangedEventArgs : EventArgs
    {
        public bool IsAuthenticated { get; }
        public UserData User { get; }

        public AuthenticationStateChangedEventArgs(bool isAuthenticated, UserData user)
        {
            IsAuthenticated = isAuthenticated;
            User = user;
        }
    }

    public static class UserDataExtensions
    {
        public static string GetDisplayName(this UserData user)
        {
            if (user == null) return string.Empty;

            return !string.IsNullOrWhiteSpace(user.FullName)
                ? user.FullName
                : user.Username;
        }

        public static string GetInitials(this UserData user)
        {
            if (user == null) return string.Empty;

            var displayName = user.GetDisplayName();
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
        }
    }
}
