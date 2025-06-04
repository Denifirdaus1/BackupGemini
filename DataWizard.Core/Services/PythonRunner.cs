
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DataWizard.Core.Services
{
    public static class PythonRunner
    {
        private static readonly string pythonExePath = @"C:\Users\Lenovo\AppData\Local\Programs\Python\Python313\python.exe";
        private static readonly string scriptPath = @"C:\Project PBTGM\PythonEngine\main.py";

        public static async Task<string> RunPythonScriptAsync(string filePath, string outputTxtPath, string prompt, string outputFormat, string mode)
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = $"\"{scriptPath}\" \"{filePath}\" \"{outputTxtPath}\" \"{prompt}\" \"{outputFormat}\" \"{mode}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string result = "";

            using (var process = new Process())
            {
                process.StartInfo = psi;
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                process.WaitForExit();

                if (output.Contains("OK"))
                {
                    result = "Success";
                }
                else
                {
                    result = $"Error: {error}\nOutput: {output}";
                }
            }

            return result;
        }


        public static string GetParsedExcelPath(string outputTxtPath)
        {
            return outputTxtPath.Replace(".txt", "_parsed.xlsx");
        }
    }
}
