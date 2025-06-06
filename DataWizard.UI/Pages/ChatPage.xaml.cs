using DataWizard.Core.Services;
using DataWizard.UI.Services;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Text;
using IOPath = System.IO.Path;

namespace DataWizard.UI.Pages
{
    public sealed partial class ChatPage : Page
    {
        private string selectedFilePath = "";
        private readonly string outputTextPath = @"C:\DataSample\hasil_output.txt";
        private Stopwatch _processTimer;
        private readonly HistoryService _historyService;
        private readonly AuthenticationService _authService;
        private bool _isProcessing = false;

        public ChatPage()
        {
            this.InitializeComponent();
            PromptBox.TextChanged += PromptBox_TextChanged;
            LoadUserPreferences();
            _processTimer = new Stopwatch();
            _historyService = new HistoryService();
            _authService = new AuthenticationService();

            var outputDir = IOPath.GetDirectoryName(outputTextPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Validate user authentication
            if (!_authService.IsAuthenticated)
            {
                this.Frame.Navigate(typeof(LoginPage));
                return;
            }
        }

        private void LoadUserPreferences()
        {
            try
            {
                WordFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
                ExcelFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
                ExcelFormatButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
                OutputFormatBox.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user preferences: {ex.Message}");
                ExcelFormatButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
                OutputFormatBox.SelectedIndex = 1;
            }
        }

        private async Task ShowDialogAsync(string title, string content)
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

        private void PromptBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CharCountText.Text = $"{PromptBox.Text.Length}/1000";
        }

        private async Task<bool> SelectFileAsync()
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".xls");
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".docx");
            picker.FileTypeFilter.Add(".pdf");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                selectedFilePath = file.Path;
                OutputBox.Text = $"File dipilih: {selectedFilePath}";
                return true;
            }
            return false;
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            await SelectFileAsync();
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
            {
                await ShowDialogAsync("Info", "Mohon tunggu, proses sedang berjalan...");
                return;
            }

            string prompt = PromptBox.Text.Trim();
            string outputFormat = (OutputFormatBox.SelectedItem as ComboBoxItem)?.Content?.ToString().ToLower() ?? "txt";
            string mode = (ModeBox.SelectedItem as ComboBoxItem)?.Content?.ToString().ToLower() ?? "file";

            if ((mode != "prompt-only" && string.IsNullOrWhiteSpace(selectedFilePath)) || string.IsNullOrWhiteSpace(prompt))
            {
                await ShowDialogAsync("Validation Error", "Harap pilih file (kecuali prompt-only) dan masukkan prompt terlebih dahulu.");
                return;
            }

            if (mode == "ocr" && !string.IsNullOrEmpty(selectedFilePath))
            {
                string[] validImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };
                string fileExtension = IOPath.GetExtension(selectedFilePath).ToLower();

                if (!validImageExtensions.Contains(fileExtension))
                {
                    await ShowDialogAsync("Validation Error",
                        $"File yang dipilih bukan format gambar yang didukung.\n" +
                        $"Format yang didukung: JPG, JPEG, PNG, BMP, TIFF\n" +
                        $"File Anda: {fileExtension}");
                    return;
                }
            }

            try
            {
                _isProcessing = true;
                _processTimer.Restart();

                WelcomePanel.Visibility = Visibility.Collapsed;
                AnswerBox.Visibility = Visibility.Visible;
                OutputBox.Text = "Memproses data... Mohon tunggu.";

                string result = await PythonRunner.RunPythonScriptAsync(
                    mode == "prompt-only" ? "none" : selectedFilePath,
                    outputTextPath,
                    prompt,
                    outputFormat,
                    mode
                );

                _processTimer.Stop();
                int processingTimeMs = (int)_processTimer.ElapsedMilliseconds;

                var userId = _authService.GetCurrentUserId();
                if (!userId.HasValue)
                {
                    await ShowDialogAsync("Error", "Sesi login telah berakhir. Silakan login kembali.");
                    this.Frame.Navigate(typeof(LoginPage));
                    return;
                }

                var (historySuccess, historyId) = await _historyService.AddProcessingHistoryAsync(
                    userId.Value,
                    selectedFilePath,
                    outputFormat,
                    mode,
                    prompt,
                    processingTimeMs,
                    result == "Success"
                );

                if (result == "Success" && File.Exists(outputTextPath))
                {
                    string hasil = File.ReadAllText(outputTextPath);

                    if (hasil.StartsWith("[ERROR]") || hasil.StartsWith("[GAGAL]"))
                    {
                        OutputBox.Text = $"Proses gagal: {hasil}";
                        return;
                    }

                    OutputBox.Text = hasil;

                    if (historySuccess && historyId.HasValue)
                    {
                        await ProcessOutputFiles(outputFormat, processingTimeMs, historyId.Value);
                    }
                }
                else
                {
                    OutputBox.Text = $"❌ Gagal: {result}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RunButton_Click: {ex}");
                string errorMessage = $"Terjadi kesalahan aplikasi:\n{ex.Message}";
                OutputBox.Text = errorMessage;
                await ShowDialogAsync("Application Error", errorMessage);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task ProcessOutputFiles(string outputFormat, int processingTimeMs, Guid historyId)
        {
            try
            {
                string parsedExcelPath = PythonRunner.GetParsedExcelPath(outputTextPath);
                string outputFileName = string.Empty;
                string outputFilePath = string.Empty;

                if (outputFormat == "excel")
                {
                    await Task.Delay(2000);

                    if (File.Exists(parsedExcelPath))
                    {
                        outputFilePath = parsedExcelPath;
                        outputFileName = IOPath.GetFileName(parsedExcelPath);
                        ResultFileText.Text = outputFileName;
                        OutputBox.Text += $"\n\n✅ File hasil parsing tersimpan di:\n{parsedExcelPath}";

                        await _historyService.AddOutputFileAsync(historyId, outputFilePath, outputFileName);
                    }
                    else
                    {
                        OutputBox.Text += "\n\n⚠️ File hasil parsing Excel tidak ditemukan.";
                    }
                }
                else if (outputFormat == "word")
                {
                    string basePath = IOPath.GetDirectoryName(outputTextPath);
                    string baseName = IOPath.GetFileNameWithoutExtension(outputTextPath);
                    string wordPath = IOPath.Combine(basePath, $"{baseName}_output.docx");

                    await Task.Delay(2000);

                    if (File.Exists(wordPath))
                    {
                        outputFilePath = wordPath;
                        outputFileName = IOPath.GetFileName(wordPath);
                        ResultFileText.Text = outputFileName;
                        OutputBox.Text += $"\n\n✅ File Word berhasil dibuat: {outputFileName}";

                        await _historyService.AddOutputFileAsync(historyId, outputFilePath, outputFileName);
                    }
                    else
                    {
                        OutputBox.Text += "\n\n⚠️ File Word tidak ditemukan";
                    }
                }

                if (!string.IsNullOrEmpty(outputFilePath) && File.Exists(outputFilePath))
                {
                    FileInfo fileInfo = new FileInfo(outputFilePath);
                    Debug.WriteLine($"Output file created: {outputFileName}, Size: {fileInfo.Length} bytes, Processing time: {processingTimeMs}ms");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing output files: {ex.Message}");
            }
        }

        private async void FileToFileButton_Click(object sender, RoutedEventArgs e)
        {
            ModeBox.SelectedIndex = 0;
            await SelectFileAsync();
        }

        private async void PromptToFileButton_Click(object sender, RoutedEventArgs e)
        {
            ModeBox.SelectedIndex = 2;
            await ShowDialogAsync("Reminder", "Please select your output format (Word or Excel) before proceeding.");
            PromptBox.Focus(FocusState.Programmatic);
        }

        private async void OcrToFileButton_Click(object sender, RoutedEventArgs e)
        {
            ModeBox.SelectedIndex = 1;
            await SelectFileAsync();
        }

        private async void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowDialogAsync("History", "Fitur riwayat sementara dinonaktifkan.\nSemua proses berjalan tanpa menyimpan riwayat.");
        }

        private void OutputFormatButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string format = clickedButton.Tag.ToString();

            WordFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
            ExcelFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;

            clickedButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
            OutputFormatBox.SelectedIndex = format == "word" ? 2 : 1;

            Debug.WriteLine($"User selected format: {format}");
        }

        private void RefreshPromptButton_Click(object sender, RoutedEventArgs e)
        {
            PromptBox.Text = "";
            selectedFilePath = "";
            OutputBox.Text = "";
            OutputFormatBox.SelectedIndex = 0;
            ModeBox.SelectedIndex = 0;

            WordFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
            ExcelFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;

            WelcomePanel.Visibility = Visibility.Visible;
            AnswerBox.Visibility = Visibility.Collapsed;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(DataWizard.UI.Pages.HomePage));
        }

        private async void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            await SelectFileAsync();
        }

        private async void UseImageButton_Click(object sender, RoutedEventArgs e)
        {
            ModeBox.SelectedIndex = 1;
            await SelectFileAsync();
        }

        private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Excel Files", new List<string>() { ".xlsx" });
            savePicker.FileTypeChoices.Add("Word Documents", new List<string>() { ".docx" });
            savePicker.SuggestedFileName = ResultFileText.Text;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    OutputBox.Text = $"File saved to: {file.Path}";
                }
                catch (Exception ex)
                {
                    await ShowDialogAsync("Error", $"Error saving file: {ex.Message}");
                }
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            _historyService?.Dispose();
            _authService?.Dispose();
            base.OnNavigatedFrom(e);
        }
    }
}