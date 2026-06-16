using Etmen_BLL.DTOs.Review;
using Etmen_BLL.Repositories.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Etmen_PL.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(CreateReviewDto model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "بيانات التقييم غير صالحة." });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "يجب تسجيل الدخول كعلامة مريض." });
                }

                var result = await _reviewService.AddReviewAsync(userId, model);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Review added successfully by user {UserId}", userId);
                    return Json(new { success = true, message = "تمت إضافة التقييم بنجاح!", data = result.Data });
                }

                return Json(new { success = false, message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding review");
                return Json(new { success = false, message = "حدث خطأ غير متوقع أثناء إضافة التقييم." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorReviews(int doctorId)
        {
            try
            {
                var result = await _reviewService.GetDoctorReviewsAsync(doctorId);
                if (result.IsSuccess)
                {
                    return Json(new { success = true, data = result.Data });
                }
                return Json(new { success = false, message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor reviews for {DoctorId}", doctorId);
                return Json(new { success = false, message = "Failed to load reviews." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProviderReviews(int providerId)
        {
            try
            {
                var result = await _reviewService.GetProviderReviewsAsync(providerId);
                if (result.IsSuccess)
                {
                    return Json(new { success = true, data = result.Data });
                }
                return Json(new { success = false, message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider reviews for {ProviderId}", providerId);
                return Json(new { success = false, message = "Failed to load reviews." });
            }
        }
    }
}
