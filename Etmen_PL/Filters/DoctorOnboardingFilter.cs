using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Etmen_BLL.Repositories.IServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Etmen_PL.Filters
{
    public class DoctorOnboardingFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true && user.IsInRole("Doctor"))
            {
                var doctorService = context.HttpContext.RequestServices.GetService<IDoctorService>();
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (doctorService != null && !string.IsNullOrEmpty(userId))
                {
                    bool? isOnboarded = null;
                    var session = context.HttpContext.Session;
                    string sessionKey = $"DoctorOnboarded_{userId}";

                    if (session != null)
                    {
                        var cachedVal = session.GetInt32(sessionKey);
                        if (cachedVal.HasValue)
                        {
                            isOnboarded = cachedVal.Value == 1;
                        }
                    }

                    if (!isOnboarded.HasValue)
                    {
                        var profileResult = await doctorService.GetProfileAsync(userId);
                        if (profileResult.IsSuccess && profileResult.Data != null)
                        {
                            isOnboarded = profileResult.Data.IsOnboarded;
                            if (session != null)
                            {
                                session.SetInt32(sessionKey, isOnboarded.Value ? 1 : 0);
                            }
                        }
                    }

                    if (isOnboarded.HasValue && !isOnboarded.Value)
                    {
                        var controller = context.RouteData.Values["controller"]?.ToString();
                        var action = context.RouteData.Values["action"]?.ToString();
                        
                        // Allow Onboarding page actions and Account Logout action
                        bool isAllowedAction = (controller == "DoctorDashboard" && (action == "Onboarding" || action == "SaveOnboarding"))
                                               || (controller == "Account" && action == "Logout");
                                               
                        if (!isAllowedAction)
                        {
                            context.Result = new RedirectToActionResult("Onboarding", "DoctorDashboard", null);
                            return;
                        }
                    }
                }
            }
            await next();
        }
    }
}
