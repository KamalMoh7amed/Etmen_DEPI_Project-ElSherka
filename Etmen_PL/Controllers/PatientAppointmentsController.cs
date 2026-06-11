using Etmen_BLL.Repositories.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Patient Appointments Controller
    /// Exposes appointment history, ticket details, cancellation, and PDF confirmation downloads
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class PatientAppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPatientService _patientService;
        private readonly IPdfReportService _pdfReportService;
        private readonly ILogger<PatientAppointmentsController> _logger;

        public PatientAppointmentsController(
            IAppointmentService appointmentService,
            IPatientService patientService,
            IPdfReportService pdfReportService,
            ILogger<PatientAppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _patientService = patientService;
            _pdfReportService = pdfReportService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /PatientAppointments
        /// Displays upcoming and past appointments
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var result = await _appointmentService.GetPatientAppointmentsAsync(userId);
                var appointments = result.IsSuccess ? result.Data : new List<Etmen_BLL.DTOs.Nearby.AppointmentDto>();

                _logger.LogInformation("Patient appointments history accessed by user {UserId}", userId);
                return View(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patient appointments");
                TempData["Error"] = "حدث خطأ أثناء تحميل سجل المواعيد";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }

        /// <summary>
        /// GET: /PatientAppointments/Details/{id}
        /// Shows ticket details for a specific appointment
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid appointment ID");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var result = await _appointmentService.GetAppointmentByIdAsync(userId, id);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "لم يتم العثور على الموعد المطلوب";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Patient appointment details accessed for appointment {AppointmentId} by user {UserId}", id, userId);
                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying appointment details");
                TempData["Error"] = "حدث خطأ أثناء تحميل تفاصيل الموعد";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /PatientAppointments/Cancel/{id}
        /// Cancels a scheduled appointment
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid appointment ID");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _appointmentService.CancelAppointmentAsync(userId, id);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Appointment {AppointmentId} cancelled successfully by user {UserId}", id, userId);
                    TempData["Success"] = "تم إلغاء الموعد بنجاح وإعادة فتح الفترة للآخرين وإخطار الطبيب.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = result.ErrorMessage ?? "فشل إلغاء الموعد المجدول";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", id);
                TempData["Error"] = "حدث خطأ أثناء محاولة إلغاء الموعد";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /PatientAppointments/DownloadConfirmation/{id}
        /// Generates and downloads the PDF receipt for a booking
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadConfirmation(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid appointment ID");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Get appointment
                var apptResult = await _appointmentService.GetAppointmentByIdAsync(userId, id);
                if (!apptResult.IsSuccess || apptResult.Data == null)
                {
                    TempData["Error"] = "الموعد غير موجود أو لا تملك صلاحية الوصول إليه.";
                    return RedirectToAction(nameof(Index));
                }

                // Get patient profile for full name
                var profileResult = await _patientService.GetProfileAsync(userId);
                var patientName = profileResult.IsSuccess ? (profileResult.Data?.FullName ?? "المريض") : "المريض";

                var appt = apptResult.Data;
                var endTime = appt.StartTime.Add(TimeSpan.FromMinutes(30)); // Assume 30-min duration standard

                var pdfBytes = await _pdfReportService.GenerateAppointmentPdfAsync(
                    patientName,
                    appt.DoctorName ?? "الطبيب",
                    appt.DoctorSpecialization ?? "أخصائي",
                    appt.Date,
                    appt.StartTime,
                    endTime,
                    appt.Notes
                );

                var fileName = $"Appointment_Confirmation_{id}.pdf";
                _logger.LogInformation("Appointment PDF download triggered for appointment {AppointmentId} by user {UserId}", id, userId);
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating appointment confirmation PDF");
                TempData["Error"] = "حدث خطأ أثناء توليد ملف الـ PDF";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
