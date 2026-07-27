using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Etmen_BLL.Helpers;
using System.Threading.Tasks;

namespace Etmen_PL.Filters
{
    public class MaintenanceFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            if (controller == "Account" && action == "Logout")
            {
                await next();
                return;
            }

            if (controller == "Home" && action == "Maintenance")
            {
                await next();
                return;
            }

            if (user.Identity?.IsAuthenticated == true)
            {
                if (user.IsInRole("Patient") && MaintenanceSettingsHelper.IsPatientMaintenanceActive)
                {
                    context.Result = new RedirectToActionResult("Maintenance", "Home", null);
                    return;
                }

                if (user.IsInRole("HospitalStaff") && MaintenanceSettingsHelper.IsStaffMaintenanceActive)
                {
                    context.Result = new RedirectToActionResult("Maintenance", "Home", null);
                    return;
                }
            }

            await next();
        }
    }
}
