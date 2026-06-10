namespace Etmen_BLL.Repositories.IServices
{
    /// <summary>
    /// Generates PDF reports for lab results, risk assessments, and appointments.
    /// </summary>
    public interface IPdfReportService
    {
        /// <summary>Generates a PDF report for a lab/radiology result.</summary>
        Task<byte[]> GenerateLabReportPdfAsync(
            string patientName, string testName, DateTime testDate,
            string? results, string? ocrData);

        /// <summary>Generates a PDF report for a risk assessment.</summary>
        Task<byte[]> GenerateRiskReportPdfAsync(
            string patientName, string riskLevel, decimal riskScore,
            List<string> recommendations, List<string> triggeredSymptoms,
            DateTime assessmentDate, bool isEmergency);

        /// <summary>Generates a PDF confirmation for an appointment.</summary>
        Task<byte[]> GenerateAppointmentPdfAsync(
            string patientName, string doctorName, string? specialization,
            DateTime appointmentDate, TimeSpan startTime, TimeSpan endTime,
            string? notes);
    }
}
