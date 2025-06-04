using System;

namespace DataWizard.Core.Models
{
    public class ProcessingHistory
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime ProcessDate { get; set; }
        public int InputFileTypeId { get; set; }
        public int OutputFormatId { get; set; }
        public int? ProcessingTime { get; set; }
        public string PromptText { get; set; }
        public string ProcessType { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class OutputFile
    {
        public Guid Id { get; set; }
        public Guid HistoryId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public long? Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? FolderId { get; set; }
    }

    public enum FileType
    {
        PDF = 1,
        DOCX = 2,
        XLSX = 3,
        PNG = 4,
        JPG = 5,
        PROMPT = 6,
        OTHER = 7
    }

    public enum OutputFormat
    {
        Excel = 1,
        Word = 2
    }
}