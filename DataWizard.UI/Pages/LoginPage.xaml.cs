using Microsoft.UI.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using DataWizard.UI.Services;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DataWizard.UI.Pages
{
    public sealed partial class LoginPage : Page
    {
        private string _currentMode = "signin";
        private readonly AuthenticationService _authService;
        private bool _isProcessing = false;

        // Store credentials after successful registration
        private string _registeredUsername = string.Empty;
        private string _registeredPassword = string.Empty;

        // Email handling
        private TextBox _emailTextBox;
        private bool _isEmailAutoCompleting = false;

        public LoginPage()
        {
            this.InitializeComponent();
            _authService = new AuthenticationService();
            UpdateButtonStates();
        }

        private async Task ShowDialogAsync(string title, string content)
        {
            try
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing dialog: {ex.Message}");
            }
        }

        private void SetLoadingState(bool isLoading, string message = "Processing...")
        {
            _isProcessing = isLoading;
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            LoadingText.Text = message;

            if (isLoading)
            {
                SubmitButton.Style = (Style)Resources["LoadingButtonStyle"];
                SubmitButton.IsEnabled = false;
            }
            else
            {
                SubmitButton.Style = (Style)Resources["PrimaryButtonStyle"];
                SubmitButton.IsEnabled = true;
            }
        }

        #region Event Handlers

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isProcessing)
                SwitchToSignIn();
        }

        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isProcessing)
                SwitchToSignUp();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == UsernameTextBox)
            {
                UsernameCheckmark.Visibility = string.IsNullOrWhiteSpace(textBox.Text) ?
                    Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                // Check if this is the email field
                if (textBox == _emailTextBox)
                {
                    HandleEmailTextChanged(textBox);
                }
                else
                {
                    UpdateAdditionalFieldsCheckmarks(textBox);
                }
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            PasswordCheckmark.Visibility = string.IsNullOrWhiteSpace(passwordBox.Password) ?
                Visibility.Collapsed : Visibility.Visible;
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            string username = UsernameTextBox.Text?.Trim();
            string password = PasswordBox.Password?.Trim();

            // Basic validation
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await ShowDialogAsync("Validation Error", "Please fill in all required fields.");
                return;
            }

            try
            {
                if (_currentMode == "signin")
                {
                    await HandleSignInAsync(username, password);
                }
                else
                {
                    await HandleSignUpAsync(username, password);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SubmitButton_Click: {ex.Message}");
                await ShowDialogAsync("Error", "An unexpected error occurred. Please try again.");
                SetLoadingState(false);
            }
        }

        #endregion

        #region Email Auto-Complete Handler

        private void HandleEmailTextChanged(TextBox emailTextBox)
        {
            if (_isEmailAutoCompleting) return;

            string currentText = emailTextBox.Text;

            // If user is typing and hasn't reached @ yet, auto-complete with @gmail.com
            if (!string.IsNullOrEmpty(currentText) && !currentText.Contains("@"))
            {
                _isEmailAutoCompleting = true;

                // Set the full email with @gmail.com
                string fullEmail = currentText + "@gmail.com";
                emailTextBox.Text = fullEmail;

                // Set cursor position right after the username part (before @gmail.com)
                emailTextBox.SelectionStart = currentText.Length;
                emailTextBox.SelectionLength = "@gmail.com".Length;

                _isEmailAutoCompleting = false;
            }
            // Handle backspace - if user deletes the @ symbol, remove the auto-completed part
            else if (!string.IsNullOrEmpty(currentText) && currentText.Contains("@gmail.com"))
            {
                int atIndex = currentText.IndexOf("@gmail.com");
                if (atIndex > 0)
                {
                    string usernamePart = currentText.Substring(0, atIndex);
                    // If the part before @gmail.com is what we want to keep
                    if (currentText == usernamePart + "@gmail.com")
                    {
                        // Keep the selection for easy editing
                        UpdateAdditionalFieldsCheckmarks(emailTextBox);
                        return;
                    }
                }
            }

            UpdateAdditionalFieldsCheckmarks(emailTextBox);
        }

        #endregion

        #region Authentication Handlers

        private async Task HandleSignInAsync(string username, string password)
        {
            SetLoadingState(true, "Signing in...");

            try
            {
                var (success, error) = await _authService.SignInAsync(username, password);

                SetLoadingState(false);

                if (success)
                {
                    Debug.WriteLine($"Sign in successful for user: {username}");

                    // Navigate to main page
                    Frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
                }
                else
                {
                    await ShowDialogAsync("Sign In Failed", error ?? "Invalid username or password. Please check your credentials and try again.");
                }
            }
            catch (Exception ex)
            {
                SetLoadingState(false);
                Debug.WriteLine($"Sign in error: {ex.Message}");
                await ShowDialogAsync("Connection Error", "Unable to connect to the server. Please check your internet connection and try again.");
            }
        }

        private async Task HandleSignUpAsync(string username, string password)
        {
            // Get additional fields data
            var additionalData = GetAdditionalFieldsData();

            if (!additionalData.IsValid)
            {
                await ShowDialogAsync("Validation Error", additionalData.ValidationMessage);
                return;
            }

            // Validate password match
            if (password != additionalData.ConfirmPassword)
            {
                await ShowDialogAsync("Validation Error", "Passwords do not match. Please ensure both password fields are identical.");
                return;
            }

            SetLoadingState(true, "Creating account...");

            try
            {
                var (success, error) = await _authService.SignUpAsync(
                    username,
                    password,
                    additionalData.Email,
                    additionalData.FullName);

                SetLoadingState(false);

                if (success)
                {
                    // Store credentials for auto-fill
                    _registeredUsername = username;
                    _registeredPassword = password;

                    await ShowDialogAsync("Success", "Account created successfully! Your login credentials have been filled automatically. Just click Sign In to continue.");

                    // Switch to sign in mode and auto-fill credentials
                    SwitchToSignIn();
                    AutoFillCredentials();
                }
                else
                {
                    await ShowDialogAsync("Registration Failed", error ?? "Failed to create account. Please try again.");
                }
            }
            catch (Exception ex)
            {
                SetLoadingState(false);
                Debug.WriteLine($"Sign up error: {ex.Message}");
                await ShowDialogAsync("Connection Error", "Unable to connect to the server. Please check your internet connection and try again.");
            }
        }

        #endregion

        #region UI State Management

        private void SwitchToSignIn()
        {
            if (_currentMode == "signin") return;

            FormTitle.Text = "Welcome Back";
            FormSubtitle.Text = "Welcome Back, Please Enter your details";
            SubmitButton.Content = "Sign In";

            AdditionalFieldsPanel.Children.Clear();
            _emailTextBox = null; // Reset email textbox reference

            _currentMode = "signin";
            UpdateButtonStates();
            AnimateContentChange();
        }

        private void SwitchToSignUp()
        {
            if (_currentMode == "signup") return;

            FormTitle.Text = "Create Account";
            FormSubtitle.Text = "Please fill in your details to create an account";
            SubmitButton.Content = "Sign Up";

            AdditionalFieldsPanel.Children.Clear();

            // Add Email field with auto-complete
            Grid emailField = CreateInputField(
                "Email Address",
                "Enter your email username",
                "/Assets/email.png",
                true,
                "email");

            // Add Full Name field
            Grid fullNameField = CreateInputField(
                "Full Name",
                "Enter your full name (optional)",
                "/Assets/User.png",
                true,
                "fullname");

            // Add Confirm Password field
            Grid confirmPasswordField = CreateInputField(
                "Confirm Password",
                "Confirm your password",
                "/Assets/lock.png",
                false,
                "confirmpassword");

            AdditionalFieldsPanel.Children.Add(emailField);
            AdditionalFieldsPanel.Children.Add(fullNameField);
            AdditionalFieldsPanel.Children.Add(confirmPasswordField);

            _currentMode = "signup";
            UpdateButtonStates();
            AnimateContentChange();
        }

        private void UpdateButtonStates()
        {
            if (_currentMode == "signin")
            {
                SignInButton.Background = new SolidColorBrush(Colors.White);
                SignUpButton.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 229, 231, 235));
            }
            else
            {
                SignInButton.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 229, 231, 235));
                SignUpButton.Background = new SolidColorBrush(Colors.White);
            }
        }

        private void AnimateContentChange()
        {
            try
            {
                var storyboard = new Storyboard();

                var fadeAnimation = new DoubleAnimation
                {
                    From = 0.5,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300))
                };

                Storyboard.SetTarget(fadeAnimation, LoginFormPanel);
                Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error: {ex.Message}");
            }
        }

        private void ClearFormFields()
        {
            UsernameTextBox.Text = string.Empty;
            PasswordBox.Password = string.Empty;

            // Clear additional fields if they exist
            foreach (var child in AdditionalFieldsPanel.Children)
            {
                if (child is Grid grid)
                {
                    ClearFieldInGrid(grid);
                }
            }
        }

        private void ClearFieldInGrid(Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is StackPanel stackPanel)
                {
                    foreach (var innerChild in stackPanel.Children)
                    {
                        if (innerChild is TextBox textBox)
                        {
                            textBox.Text = string.Empty;
                        }
                        else if (innerChild is PasswordBox passwordBox)
                        {
                            passwordBox.Password = string.Empty;
                        }
                    }
                }
            }
        }

        // New method to auto-fill credentials after successful registration
        private void AutoFillCredentials()
        {
            if (!string.IsNullOrEmpty(_registeredUsername) && !string.IsNullOrEmpty(_registeredPassword))
            {
                UsernameTextBox.Text = _registeredUsername;
                PasswordBox.Password = _registeredPassword;

                // Update checkmarks to show fields are filled
                UsernameCheckmark.Visibility = Visibility.Visible;
                PasswordCheckmark.Visibility = Visibility.Visible;

                Debug.WriteLine($"Auto-filled credentials for user: {_registeredUsername}");
            }
        }

        #endregion

        #region Helper Methods

        private (bool IsValid, string ValidationMessage, string Email, string FullName, string ConfirmPassword) GetAdditionalFieldsData()
        {
            string email = string.Empty;
            string fullName = string.Empty;
            string confirmPassword = string.Empty;

            try
            {
                foreach (var child in AdditionalFieldsPanel.Children)
                {
                    if (child is Grid grid && grid.Tag != null)
                    {
                        string fieldType = grid.Tag.ToString();
                        string fieldValue = GetFieldValueFromGrid(grid);

                        switch (fieldType)
                        {
                            case "email":
                                email = fieldValue;
                                break;
                            case "fullname":
                                fullName = fieldValue;
                                break;
                            case "confirmpassword":
                                confirmPassword = fieldValue;
                                break;
                        }
                    }
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(email))
                {
                    return (false, "Email address is required.", string.Empty, string.Empty, string.Empty);
                }

                // Basic email validation - now expecting @gmail.com format
                if (!IsValidGmailEmail(email))
                {
                    return (false, "Please enter a valid Gmail address.", string.Empty, string.Empty, string.Empty);
                }

                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    return (false, "Please confirm your password.", string.Empty, string.Empty, string.Empty);
                }

                return (true, string.Empty, email.Trim(), fullName?.Trim(), confirmPassword);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting additional fields data: {ex.Message}");
                return (false, "Error reading form data.", string.Empty, string.Empty, string.Empty);
            }
        }

        private string GetFieldValueFromGrid(Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is StackPanel stackPanel)
                {
                    foreach (var innerChild in stackPanel.Children)
                    {
                        if (innerChild is TextBox textBox)
                        {
                            return textBox.Text;
                        }
                        else if (innerChild is PasswordBox passwordBox)
                        {
                            return passwordBox.Password;
                        }
                    }
                }
            }
            return string.Empty;
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

        // New method specifically for Gmail validation
        private bool IsValidGmailEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email)) return false;

                // Check if it ends with @gmail.com
                if (!email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Validate using standard email validation
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private Grid CreateInputField(string label, string placeholder, string iconPath, bool isTextField, string fieldType)
        {
            Grid fieldGrid = new Grid
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 229, 231, 235)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12),
                Width = 320,
                Height = 60,
                Tag = fieldType // Store field type for identification
            };

            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            Image icon = new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri($"ms-appx://{iconPath}")),
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(icon, 0);
            fieldGrid.Children.Add(icon);

            // Content
            StackPanel content = new StackPanel();
            Grid.SetColumn(content, 1);

            TextBlock labelText = new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 156, 163, 175))
            };
            content.Children.Add(labelText);

            if (isTextField)
            {
                TextBox inputField = new TextBox
                {
                    PlaceholderText = placeholder,
                    Width = 230,
                    BorderThickness = new Thickness(0),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Background = new SolidColorBrush(Colors.Transparent)
                };
                inputField.TextChanged += TextBox_TextChanged;

                // Store reference to email textbox for auto-complete functionality
                if (fieldType == "email")
                {
                    _emailTextBox = inputField;
                }

                content.Children.Add(inputField);
            }
            else
            {
                PasswordBox inputField = new PasswordBox
                {
                    PlaceholderText = placeholder,
                    Width = 230,
                    BorderThickness = new Thickness(0),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Background = new SolidColorBrush(Colors.Transparent)
                };
                inputField.PasswordChanged += PasswordBox_PasswordChanged;
                content.Children.Add(inputField);
            }

            fieldGrid.Children.Add(content);

            // Checkmark
            Grid checkmark = new Grid
            {
                Visibility = Visibility.Collapsed,
                Width = 20,
                Height = 20
            };

            Ellipse ellipse = new Ellipse
            {
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 126, 217, 192))
            };

            Path checkPath = new Path
            {
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1.5,
                StrokeLineJoin = PenLineJoin.Round,
                Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M6,10.5 L8.5,13 L14,7")
            };

            checkmark.Children.Add(ellipse);
            checkmark.Children.Add(checkPath);

            Grid.SetColumn(checkmark, 2);
            fieldGrid.Children.Add(checkmark);

            return fieldGrid;
        }

        private void UpdateAdditionalFieldsCheckmarks(TextBox textBox)
        {
            try
            {
                if (textBox.Parent is StackPanel stackPanel &&
                    stackPanel.Parent is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is Grid checkmarkGrid && checkmarkGrid.Children.Count > 0)
                        {
                            checkmarkGrid.Visibility = string.IsNullOrWhiteSpace(textBox.Text) ?
                                Visibility.Collapsed : Visibility.Visible;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating checkmarks: {ex.Message}");
            }
        }

        #endregion

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            // Clean up when leaving the page
            _authService?.Dispose();
            base.OnNavigatedFrom(e);
        }
    }
}
