using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace DataWizard.UI.Pages
{
    public sealed partial class HomePage : Page
    {
        private ObservableCollection<OutputFile> _recentFiles;
        private ObservableCollection<Folder> _folders;
        private ObservableCollection<ChartData> _chartData;
        private ObservableCollection<HistoryItem> _historyItems;

        public HomePage()
        {
            InitializeComponent();
            _recentFiles = new ObservableCollection<OutputFile>();
            _folders = new ObservableCollection<Folder>();
            _chartData = new ObservableCollection<ChartData>();
            _historyItems = new ObservableCollection<HistoryItem>();

            // Initialize with sample data
            InitializeSampleData();
            LoadData();
        }

        private void InitializeSampleData()
        {
            // Sample history items
            _historyItems.Add(new HistoryItem
            {
                InputType = "CSV",
                OutputFormat = "Excel",
                ProcessDate = DateTime.Now.AddHours(-2)
            });
            _historyItems.Add(new HistoryItem
            {
                InputType = "JSON",
                OutputFormat = "Word",
                ProcessDate = DateTime.Now.AddDays(-1)
            });
            _historyItems.Add(new HistoryItem
            {
                InputType = "XML",
                OutputFormat = "Excel",
                ProcessDate = DateTime.Now.AddDays(-3)
            });

            // Sample folders
            _folders.Add(new Folder { FolderName = "Project Documents" });
            _folders.Add(new Folder { FolderName = "Data Analysis" });
            _folders.Add(new Folder { FolderName = "Reports" });

            // Sample recent files
            _recentFiles.Add(new OutputFile
            {
                FileName = "Sales_Report.xlsx",
                FileType = "Excel",
                CreatedDate = DateTime.Now.AddHours(-1)
            });
            _recentFiles.Add(new OutputFile
            {
                FileName = "Customer_Data.docx",
                FileType = "Word",
                CreatedDate = DateTime.Now.AddDays(-2)
            });

            // Sample chart data
            _chartData.Add(new ChartData { Label = "Excel", Value = 45 });
            _chartData.Add(new ChartData { Label = "Word", Value = 30 });
            _chartData.Add(new ChartData { Label = "PDF", Value = 25 });
        }

        private async void LoadData()
        {
            try
            {
                // Simulate loading delay
                await Task.Delay(500);

                UpdateRecentFiles(_historyItems.ToList());
                UpdateFolders();
                UpdateChart();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Error", $"Failed to load data: {ex.Message}");
            }
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void UpdateRecentFiles(List<HistoryItem> historyItems)
        {
            RecentFilesPanel.Children.Clear();
            foreach (var item in historyItems)
            {
                var historyControl = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    Margin = new Thickness(0, 0, 0, 16)
                };

                // Add icon based on output format
                var icon = new Image
                {
                    Width = 24,
                    Height = 24,
                    Source = GetFormatIcon(item.OutputFormat)
                };

                // Add text information
                var textPanel = new StackPanel();
                textPanel.Children.Add(new TextBlock
                {
                    Text = $"{item.InputType} → {item.OutputFormat}",
                    FontWeight = FontWeights.SemiBold
                });
                textPanel.Children.Add(new TextBlock
                {
                    Text = FormatTime(item.ProcessDate),
                    Foreground = new SolidColorBrush(Colors.Gray),
                    FontSize = 12
                });

                historyControl.Children.Add(icon);
                historyControl.Children.Add(textPanel);

                RecentFilesPanel.Children.Add(historyControl);
            }
        }

        private BitmapImage GetFormatIcon(string format)
        {
            string iconPath = format.ToLower() switch
            {
                "word" => "ms-appx:///Assets/Microsoft Word 2024.png",
                "excel" => "ms-appx:///Assets/Microsoft Excel 2025.png",
                _ => "ms-appx:///Assets/File.png"
            };
            return new BitmapImage(new Uri(iconPath));
        }

        private string FormatTime(DateTime date)
        {
            TimeSpan diff = DateTime.Now - date;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
            return date.ToString("dd MMM yyyy");
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void UpdateFolders()
        {
            FoldersPanel.Children.Clear();
            foreach (var folder in _folders)
            {
                FoldersPanel.Children.Add(new FolderItem
                {
                    FolderName = folder.FolderName
                });
            }
        }

        private void UpdateChart()
        {
            // Update chart visualization based on _chartData
            // Implementation depends on your charting library
            Debug.WriteLine($"Chart data updated with {_chartData.Count} items");
        }

        private string GetFileType(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName).ToLower();
            switch (extension)
            {
                case ".xlsx":
                    return "Excel";
                case ".docx":
                    return "Word";
                default:
                    return "Unknown";
            }
        }

        // Method to add new history item programmatically
        public void AddHistoryItem(string inputType, string outputFormat)
        {
            var newItem = new HistoryItem
            {
                InputType = inputType,
                OutputFormat = outputFormat,
                ProcessDate = DateTime.Now
            };
            _historyItems.Insert(0, newItem); // Add to beginning
            UpdateRecentFiles(_historyItems.ToList());
        }

        // Method to add new folder programmatically
        public void AddFolder(string folderName)
        {
            _folders.Add(new Folder { FolderName = folderName });
            UpdateFolders();
        }

        #region Event Handlers
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle Add button click
            Debug.WriteLine("Add button clicked");
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle Search button click
            Debug.WriteLine("Search button clicked");
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Already on home page
            Debug.WriteLine("Home button clicked - already on home page");
        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to folders page
            Debug.WriteLine("Folder button clicked");
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to history page
            Debug.WriteLine("History button clicked");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to settings page
            Debug.WriteLine("Settings button clicked");
        }

        private void UserProfileButton_Click(object sender, RoutedEventArgs e)
        {
            // Show user profile menu or navigate to profile page
            Debug.WriteLine("User profile button clicked");

            // Example: Show a flyout or dialog
            /*
            var flyout = new MenuFlyout();
            flyout.Items.Add(new MenuFlyoutItem { Text = "My Profile" });
            flyout.Items.Add(new MenuFlyoutItem { Text = "Settings" });
            flyout.Items.Add(new MenuFlyoutItem { Text = "Sign Out" });
            flyout.ShowAt(UserProfileButton);
            */
        }

        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            // Start new project
            this.Frame.Navigate(typeof(DataWizard.UI.Pages.ChatPage));
        }
        #endregion
    }

    // Data models
    public class HistoryItem
    {
        public string InputType { get; set; }
        public string OutputFormat { get; set; }
        public DateTime ProcessDate { get; set; }
    }

    public class OutputFile
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class Folder
    {
        public string FolderName { get; set; }
    }

    public class ChartData
    {
        public string Label { get; set; }
        public double Value { get; set; }
    }

    // UI Components
    public sealed class FileItem : Grid
    {
        public FileItem()
        {
            this.Background = new SolidColorBrush(Colors.LightBlue);
            this.CornerRadius = new CornerRadius(8);
            this.Padding = new Thickness(24, 12, 24, 12);

            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var fileNameText = new TextBlock
            {
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            fileNameText.SetBinding(TextBlock.TextProperty,
                new Microsoft.UI.Xaml.Data.Binding() { Path = new PropertyPath("FileName") });

            var rightContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(16, 0, 0, 0)
            };

            var separator = new Border
            {
                Width = 1,
                Height = 24,
                Background = new SolidColorBrush(Colors.Black)
            };

            var fileIcon = new Image
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(16, 0, 0, 0)
            };

            fileIcon.SetBinding(Image.SourceProperty,
                new Microsoft.UI.Xaml.Data.Binding()
                {
                    Path = new PropertyPath("FileType"),
                    Converter = new FileTypeToIconConverter()
                });

            rightContent.Children.Add(separator);
            rightContent.Children.Add(fileIcon);

            sp.Children.Add(fileNameText);
            sp.Children.Add(rightContent);

            this.Children.Add(sp);
        }

        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }

        public string FileType
        {
            get => (string)GetValue(FileTypeProperty);
            set => SetValue(FileTypeProperty, value);
        }

        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register(nameof(FileName), typeof(string),
                typeof(FileItem), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FileTypeProperty =
            DependencyProperty.Register(nameof(FileType), typeof(string),
                typeof(FileItem), new PropertyMetadata(string.Empty));
    }

    public sealed class FolderItem : Grid
    {
        public FolderItem()
        {
            this.Background = new SolidColorBrush(Colors.LightBlue);
            this.CornerRadius = new CornerRadius(8);
            this.Padding = new Thickness(24, 12, 24, 12);

            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var folderNameText = new TextBlock
            {
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            folderNameText.SetBinding(TextBlock.TextProperty,
                new Microsoft.UI.Xaml.Data.Binding() { Path = new PropertyPath("FolderName") });

            var rightContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(16, 0, 0, 0)
            };

            var separator = new Border
            {
                Width = 1,
                Height = 24,
                Background = new SolidColorBrush(Colors.Black)
            };

            var folderIcon = new FontIcon
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uE8B7",
                FontSize = 20,
                Margin = new Thickness(16, 0, 0, 0)
            };

            rightContent.Children.Add(separator);
            rightContent.Children.Add(folderIcon);

            sp.Children.Add(folderNameText);
            sp.Children.Add(rightContent);

            this.Children.Add(sp);
        }

        public string FolderName
        {
            get => (string)GetValue(FolderNameProperty);
            set => SetValue(FolderNameProperty, value);
        }

        public static readonly DependencyProperty FolderNameProperty =
            DependencyProperty.Register(nameof(FolderName), typeof(string),
                typeof(FolderItem), new PropertyMetadata(string.Empty));
    }

    public class FileTypeToIconConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, string language)
        {
            string fileType = value as string;
            return new BitmapImage(
                new Uri($"ms-appx:///Assets/{fileType.ToLower()}.png"));
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
