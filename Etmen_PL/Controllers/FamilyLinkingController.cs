using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Etmen_Domain.Entities;
using Etmen_DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Family Linking Controller
    /// Invitation flows and viewer permission adjustments
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class FamilyLinkingController : Controller
    {
        private readonly IFamilyService _familyService;
        private readonly IPatientService _patientService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<FamilyLinkingController> _logger;

        public FamilyLinkingController(
            IFamilyService familyService,
            IPatientService patientService,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork uow,
            ILogger<FamilyLinkingController> logger)
        {
            _familyService = familyService;
            _patientService = patientService;
            _userManager = userManager;
            _uow = uow;
            _logger = logger;
        }

        /// <summary>
        /// GET: /FamilyLinking/Index
        /// Lists family links and status
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    TempData["Error"] = "فشل تحميل ملف المريض";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                var myPatientId = profileResult.Data.Id;

                // 1. Accepted family members (both where I am primary or linked)
                var acceptedLinks = await _uow.FamilyLinks.Table
                    .Include(f => f.PrimaryPatient).ThenInclude(p => p.ApplicationUser)
                    .Include(f => f.LinkedPatient).ThenInclude(p => p.ApplicationUser)
                    .Where(f => (f.PrimaryPatientId == myPatientId || f.LinkedPatientId == myPatientId) && f.IsAccepted)
                    .ToListAsync();

                var acceptedDtos = acceptedLinks.Select(f => new Etmen_BLL.DTOs.Family.FamilyDto
                {
                    Id = f.Id,
                    Relationship = f.Relationship,
                    IsAccepted = f.IsAccepted,
                    CanViewRecords = f.CanViewRecords,
                    CanViewRisk = f.CanViewRisk,
                    CanBookAppointments = f.CanBookAppointments,
                    LinkedPatientName = f.PrimaryPatientId == myPatientId 
                        ? (f.LinkedPatient != null ? $"{f.LinkedPatient.ApplicationUser.FirstName} {f.LinkedPatient.ApplicationUser.LastName}".Trim() : "") 
                        : (f.PrimaryPatient != null ? $"{f.PrimaryPatient.ApplicationUser.FirstName} {f.PrimaryPatient.ApplicationUser.LastName}".Trim() : ""),
                    CreatedAt = f.CreatedAt
                }).ToList();

                // 2. Incoming pending requests (where I am linked, but not accepted yet)
                var incomingInvites = await _uow.FamilyLinks.Table
                    .Include(f => f.PrimaryPatient).ThenInclude(p => p.ApplicationUser)
                    .Where(f => f.LinkedPatientId == myPatientId && !f.IsAccepted)
                    .ToListAsync();

                var incomingDtos = incomingInvites.Select(f => new Etmen_BLL.DTOs.Family.FamilyDto
                {
                    Id = f.Id,
                    Relationship = f.Relationship,
                    IsAccepted = f.IsAccepted,
                    CanViewRecords = f.CanViewRecords,
                    CanViewRisk = f.CanViewRisk,
                    CanBookAppointments = f.CanBookAppointments,
                    LinkedPatientName = f.PrimaryPatient != null ? $"{f.PrimaryPatient.ApplicationUser.FirstName} {f.PrimaryPatient.ApplicationUser.LastName}".Trim() : "",
                    CreatedAt = f.CreatedAt
                }).ToList();

                // 3. Outgoing pending invites (where I sent them, but not accepted yet)
                var outgoingInvites = await _uow.FamilyLinks.Table
                    .Include(f => f.LinkedPatient).ThenInclude(p => p.ApplicationUser)
                    .Where(f => f.PrimaryPatientId == myPatientId && !f.IsAccepted)
                    .ToListAsync();

                var outgoingDtos = outgoingInvites.Select(f => new Etmen_BLL.DTOs.Family.FamilyDto
                {
                    Id = f.Id,
                    Relationship = f.Relationship,
                    IsAccepted = f.IsAccepted,
                    CanViewRecords = f.CanViewRecords,
                    CanViewRisk = f.CanViewRisk,
                    CanBookAppointments = f.CanBookAppointments,
                    LinkedPatientName = f.LinkedPatient != null ? $"{f.LinkedPatient.ApplicationUser.FirstName} {f.LinkedPatient.ApplicationUser.LastName}".Trim() : "",
                    CreatedAt = f.CreatedAt
                }).ToList();

                ViewBag.FamilyInvite = new FamilyInviteViewModel();
                ViewBag.IncomingInvites = incomingDtos;
                ViewBag.OutgoingInvites = outgoingDtos;

                return View(acceptedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving family members");
                TempData["Error"] = "خطأ في تحميل أفراد الأسرة";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }

        /// <summary>
        /// POST: /FamilyLinking/Invite
        /// Sends link invite
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(FamilyInviteViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "البيانات المدخلة غير صالحة";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    TempData["Error"] = "فشل تحميل ملفك الطبي";
                    return RedirectToAction(nameof(Index));
                }

                // Find recipient user by email
                var recipientUser = await _userManager.FindByEmailAsync(viewModel.Email);
                if (recipientUser == null)
                {
                    TempData["Error"] = "هذا البريد الإلكتروني غير مسجل في النظام";
                    return RedirectToAction(nameof(Index));
                }

                // Get recipient's patient profile
                var recipientProfileResult = await _patientService.GetProfileAsync(recipientUser.Id);
                if (!recipientProfileResult.IsSuccess || recipientProfileResult.Data == null)
                {
                    TempData["Error"] = "المستخدم المدعو ليس لديه ملف طبي نشط";
                    return RedirectToAction(nameof(Index));
                }

                // Check that they aren't inviting themselves
                if (recipientUser.Id == userId)
                {
                    TempData["Error"] = "لا يمكنك إرسال دعوة ارتباط لنفسك";
                    return RedirectToAction(nameof(Index));
                }

                var inviteDto = new Etmen_BLL.DTOs.Family.FamilyInviteDto
                {
                    PrimaryPatientId = profileResult.Data.Id,
                    LinkedPatientId = recipientProfileResult.Data.Id,
                    Relationship = viewModel.Relationship,
                    CanViewRecords = viewModel.CanViewRecords,
                    CanViewRisk = viewModel.CanViewRisk,
                    CanBookAppointments = viewModel.CanBookAppointments
                };

                var result = await _familyService.InviteFamilyMemberAsync(inviteDto);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Family invite sent from patient of user {UserId} to user {RecipientId}", userId, recipientUser.Id);
                    TempData["Success"] = "تم إرسال دعوة الارتباط العائلي بنجاح";
                }
                else
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل إرسال الدعوة";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending family invite");
                TempData["Error"] = "خطأ في إرسال الدعوة";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /FamilyLinking/Accept
        /// Completes link from token parameter
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Accept(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "رابط الدعوة غير صالح";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var result = await _familyService.AcceptFamilyInviteAsync(token);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Family invite accepted with token: {Token}", token);
                    TempData["Success"] = "تم قبول دعوة الارتباط العائلي بنجاح";
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        return RedirectToAction("Login", "Account");
                    }
                }

                TempData["Error"] = result.ErrorMessage ?? "فشل قبول الدعوة. قد يكون الرابط منتهياً أو تم استخدامه بالفعل.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting family invite");
                TempData["Error"] = "خطأ في قبول الدعوة";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// POST: /FamilyLinking/Remove
        /// Deletes family link
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var currentProfileResult = await _patientService.GetProfileAsync(userId);
                if (!currentProfileResult.IsSuccess || currentProfileResult.Data == null)
                {
                    TempData["Error"] = "فشل تحميل ملف المريض";
                    return RedirectToAction("Index");
                }

                var link = await _uow.FamilyLinks.Table.FirstOrDefaultAsync(f => f.Id == id 
                    && (f.PrimaryPatientId == currentProfileResult.Data.Id || f.LinkedPatientId == currentProfileResult.Data.Id));

                if (link == null)
                {
                    TempData["Error"] = "الارتباط العائلي غير موجود أو غير مصرح لك بحذفه أو رفضه.";
                    return RedirectToAction("Index");
                }

                _uow.FamilyLinks.Remove(link);
                await _uow.CompleteAsync();

                TempData["Success"] = "تم إلغاء الارتباط العائلي/الدعوة بنجاح.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing family member");
                TempData["Error"] = "خطأ في حذف الرابط";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptInvite(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var currentProfileResult = await _patientService.GetProfileAsync(userId);
                if (!currentProfileResult.IsSuccess || currentProfileResult.Data == null)
                {
                    TempData["Error"] = "فشل تحميل ملف المريض";
                    return RedirectToAction("Index");
                }

                var link = await _uow.FamilyLinks.Table
                    .FirstOrDefaultAsync(f => f.Id == id && f.LinkedPatientId == currentProfileResult.Data.Id);

                if (link == null)
                {
                    TempData["Error"] = "طلب الارتباط غير موجود أو غير مصرح لك بقبوله.";
                    return RedirectToAction("Index");
                }

                link.IsAccepted = true;
                link.AcceptedAt = DateTime.UtcNow;

                _uow.FamilyLinks.Update(link);
                await _uow.CompleteAsync();

                TempData["Success"] = "تم قبول طلب الارتباط العائلي بنجاح.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting family invite");
                TempData["Error"] = "حدث خطأ أثناء قبول طلب الارتباط.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// POST: /FamilyLinking/UpdatePermissions
        /// Adjusts record view settings
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissions(int id, FamilyInviteViewModel viewModel)
        {
            try
            {
                var dto = new Etmen_BLL.DTOs.Family.FamilyDto
                {
                    CanViewRecords = viewModel.CanViewRecords,
                    CanViewRisk = viewModel.CanViewRisk,
                    CanBookAppointments = viewModel.CanBookAppointments
                };

                var result = await _familyService.UpdateFamilyPermissionsAsync(id, dto);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Family permissions updated for link {LinkId}", id);
                    TempData["Success"] = "تم تحديث الصلاحيات بنجاح";
                }
                else
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل تحديث الصلاحيات";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating family permissions");
                TempData["Error"] = "خطأ في تحديث الصلاحيات";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("FamilyLinking/Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var currentProfileResult = await _patientService.GetProfileAsync(userId);
                if (!currentProfileResult.IsSuccess || currentProfileResult.Data == null)
                {
                    TempData["Error"] = "فشل تحميل ملف المريض";
                    return RedirectToAction("Index");
                }

                // Retrieve the FamilyLink by id, ensuring it belongs to the current user
                var familyLink = await _uow.FamilyLinks.Table
                    .Include(f => f.PrimaryPatient).ThenInclude(p => p.ApplicationUser)
                    .Include(f => f.LinkedPatient).ThenInclude(p => p.ApplicationUser)
                    .FirstOrDefaultAsync(f => f.Id == id && (f.PrimaryPatientId == currentProfileResult.Data.Id || f.LinkedPatientId == currentProfileResult.Data.Id));

                if (familyLink == null || !familyLink.IsAccepted)
                {
                    TempData["Error"] = "الالتحاق العائلي غير موجود أو لم يتم قبوله بعد.";
                    return RedirectToAction("Index");
                }

                // The target patient profile we want to view is the OTHER person in the link
                var targetPatientId = familyLink.PrimaryPatientId == currentProfileResult.Data.Id 
                    ? familyLink.LinkedPatientId 
                    : familyLink.PrimaryPatientId;

                var targetPatient = await _uow.PatientProfiles.Table
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.MedicalRecords)
                    .Include(p => p.RiskAssessments)
                    .Include(p => p.EmergencyRequests).ThenInclude(r => r.HealthcareProvider)
                    .Include(p => p.EmergencyRequests).ThenInclude(r => r.AssignedDoctor)
                    .Include(p => p.LabResults)
                    .FirstOrDefaultAsync(p => p.Id == targetPatientId);

                if (targetPatient == null)
                {
                    TempData["Error"] = "ملف القريب الطبي غير موجود.";
                    return RedirectToAction("Index");
                }

                // Construct relationship Arabic translation
                ViewBag.Relationship = familyLink.Relationship switch {
                    "Father" => "أب",
                    "Mother" => "أم",
                    "Spouse" => "زوج / زوجة",
                    "Son" => "ابن",
                    "Daughter" => "ابنة",
                    "Brother" => "أخ",
                    "Sister" => "أخت",
                    _ => familyLink.Relationship
                };

                // Get list of all accepted family links for tabs
                var familyLinks = await _uow.FamilyLinks.Table
                    .Include(f => f.PrimaryPatient).ThenInclude(p => p.ApplicationUser)
                    .Include(f => f.LinkedPatient).ThenInclude(p => p.ApplicationUser)
                    .Where(f => (f.PrimaryPatientId == currentProfileResult.Data.Id || f.LinkedPatientId == currentProfileResult.Data.Id) && f.IsAccepted)
                    .ToListAsync();

                ViewBag.FamilyTabs = familyLinks;
                ViewBag.CurrentLinkId = id;

                return View(targetPatient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading family member details");
                TempData["Error"] = "حدث خطأ في تحميل تفاصيل القريب";
                return RedirectToAction("Index");
            }
        }
    }
}
