using Etmen_BLL.DTOs.Emergency;
using Etmen_BLL.DTOs.Lab;
using Etmen_BLL.DTOs.Medical;
using Etmen_BLL.DTOs.Nearby;
using Etmen_BLL.DTOs.Patient;
using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Repositories.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Etmen_PL.Controllers
{
   
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly IPatientService      _patientService;
        private readonly IAppointmentService  _appointmentService;
        private readonly IAlertService        _alertService;
        private readonly ILabService          _labService;
        private readonly INearbyService       _nearbyService;
        private readonly IEmergencyService    _emergencyService;
        private readonly IFamilyService       _familyService;
        private readonly ILogger<PatientController> _logger;

        public PatientController(
            IPatientService      patientService,
            IAppointmentService  appointmentService,
            IAlertService        alertService,
            ILabService          labService,
            INearbyService       nearbyService,
            IEmergencyService    emergencyService,
            IFamilyService       familyService,
            ILogger<PatientController> logger)
        {
            _patientService     = patientService;
            _appointmentService = appointmentService;
            _alertService       = alertService;
            _labService         = labService;
            _nearbyService      = nearbyService;
            _emergencyService   = emergencyService;
            _familyService      = familyService;
            _logger             = logger;
        }

      
        private string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private void SetError(string msg) =>
            TempData["Error"] = msg;

        private void SetSuccess(string msg) =>
            TempData["Success"] = msg;

        
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

       
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileDto dto)
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        
        [HttpGet]
        public async Task<IActionResult> Appointments()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            throw new NotImplementedException("Not implemented yet.");
        }

      
        [HttpGet]
        public async Task<IActionResult> MedicalRecords()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicalRecord(MedicalRecordCreateDto dto)
        {
            throw new NotImplementedException("Not implemented yet.");
        }

      
        [HttpGet]
        public async Task<IActionResult> LabResults()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLab(LabUploadDto dto)
        {
            throw new NotImplementedException("Not implemented yet.");
        }

       
        [HttpGet]
        public async Task<IActionResult> RiskAssessment()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RiskAssessment(RiskInputDto dto)
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        [HttpGet]
        public IActionResult RiskResult()
        {
            if (TempData["RiskResult"] is not string json)
                return RedirectToAction(nameof(RiskAssessment));

            var dto = System.Text.Json.JsonSerializer.Deserialize<RiskResultDto>(json);
            return View(dto);
        }

      
        [HttpGet]
        public async Task<IActionResult> Alerts()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAlertRead(int id)
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAlertsRead()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

       
        [HttpGet]
        public IActionResult Nearby() => View(new NearbySearchDto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nearby(NearbySearchDto dto)
        {
            throw new NotImplementedException("Not implemented yet.");
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Emergency(EmergencyRequestDto dto)
        {
            throw new NotImplementedException("Not implemented yet.");
        }
    }
}
