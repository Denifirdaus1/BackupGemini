using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataWizard.Core.Models;
using System.IO;

namespace DataWizard.Core.Services
{
    public class HistoryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;

        public HistoryService()
        {
            _supabaseUrl = "https://rrlmejrtlqnfaavyrrtf.supabase.co";
            _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InJybG1lanJ0bHFuZmFhdnlycnRmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDgyMzI5NzUsImV4cCI6MjA2MzgwODk3NX0.8uC7og_bfk2C-Ok6KNGAY5Ej-nz_wBz07-94BG1rUZY";

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }

        public async Task<(bool success, Guid? historyId)> AddProcessingHistoryAsync(
            Guid userId,
            string inputFilePath,
            string outputFormat,
            string mode,
            string promptText,
            int processingTime,
            bool isSuccess)
        {
            try
            {
                // Determine input file type
                int inputFileTypeId = DetermineFileType(inputFilePath, mode);
                
                // Determine output format ID
                int outputFormatId = outputFormat.ToLower() == "excel" ? 1 : 2;

                var history = new ProcessingHistory
                {
                    UserId = userId,
                    ProcessDate = DateTime.UtcNow,
                    InputFileTypeId = inputFileTypeId,
                    OutputFormatId = outputFormatId,
                    ProcessingTime = processingTime,
                    PromptText = promptText,
                    ProcessType = mode,
                    IsSuccess = isSuccess
                };

                var jsonContent = JsonSerializer.Serialize(history);
                var response = await _httpClient.PostAsync(
                    $"{_supabaseUrl}/rest/v1/history",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ProcessingHistory>(content);
                    return (true, result.Id);
                }

                return (false, null);
            }
            catch (Exception)
            {
                return (false, null);
            }
        }

        public async Task<bool> AddOutputFileAsync(
            Guid historyId,
            string filePath,
            string fileName)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var outputFile = new OutputFile
                {
                    HistoryId = historyId,
                    Name = fileName,
                    Path = filePath,
                    Size = fileInfo.Length,
                    CreatedAt = DateTime.UtcNow
                };

                var jsonContent = JsonSerializer.Serialize(outputFile);
                var response = await _httpClient.PostAsync(
                    $"{_supabaseUrl}/rest/v1/output_files",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json")
                );

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private int DetermineFileType(string filePath, string mode)
        {
            if (mode == "prompt-only")
                return (int)FileType.PROMPT;

            if (string.IsNullOrEmpty(filePath))
                return (int)FileType.OTHER;

            string ext = Path.GetExtension(filePath).ToLower();
            return ext switch
            {
                ".pdf" => (int)FileType.PDF,
                ".docx" => (int)FileType.DOCX,
                ".xlsx" or ".xls" => (int)FileType.XLSX,
                ".png" => (int)FileType.PNG,
                ".jpg" or ".jpeg" => (int)FileType.JPG,
                _ => (int)FileType.OTHER
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}