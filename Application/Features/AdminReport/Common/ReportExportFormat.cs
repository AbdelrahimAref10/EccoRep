namespace Application.Features.AdminReport.Common
{
    public enum ReportExportFormat
    {
        Excel = 1,
        Pdf = 2
    }

    public class ReportFileResult
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
