using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models;
using Etmen_PL.Models.Home;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
   
    public class HomeController : Controller
    {
        private readonly ICrisisService _crisisService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ICrisisService crisisService,
            ILogger<HomeController> logger)
        {
            _crisisService = crisisService;
            _logger = logger;
        }

        
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleDashboard();

            var vm = new LandingPageViewModel();

            try
            {
                var crisisResult = await _crisisService.GetActiveCrisisAsync();
                if (crisisResult.IsSuccess && crisisResult.Data is not null)
                {
                    vm.HasActiveCrisis = true;
                    vm.ActiveCrisisName = crisisResult.Data.CrisisName;
                    vm.ActiveCrisisType = crisisResult.Data.CrisisType.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch active crisis for landing page.");
            }

            return View(vm);
        }

       
        [HttpGet]
        public IActionResult Privacy() => View();

       
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode)
        {
            var vm = new ErrorViewModel
            {
                StatusCode = statusCode ?? 500,
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(vm);
        }

       
        private IActionResult RedirectToRoleDashboard()
        {
            if (User.IsInRole("Admin") || User.IsInRole("CrisisAdmin"))
                return RedirectToAction("Index", "AdminDashboard");

            if (User.IsInRole("Doctor"))
                return RedirectToAction("Index", "DoctorDashboard");

            if (User.IsInRole("HospitalStaff"))
                return RedirectToAction("Index", "HospitalQueue");

            return RedirectToAction("Index", "PatientDashboard");
        }
    }
}
