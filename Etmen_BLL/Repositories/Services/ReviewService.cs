using Etmen_BLL.DTOs.Review;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etmen_BLL.Repositories.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _uow;

        public ReviewService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<ReviewDto>> AddReviewAsync(string userId, CreateReviewDto dto)
        {
            try
            {
                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<ReviewDto>.Failure("Patient profile not found.");

                if (dto.DoctorProfileId == null && dto.HealthcareProviderId == null)
                    return ServiceResult<ReviewDto>.Failure("Either Doctor ID or Healthcare Provider ID must be specified.");

                // Validate rating
                if (dto.Rating < 1 || dto.Rating > 5)
                    return ServiceResult<ReviewDto>.Failure("Rating must be between 1 and 5.");

                // Check completed appointment constraints
                if (dto.DoctorProfileId.HasValue)
                {
                    var doctorId = dto.DoctorProfileId.Value;
                    var hasCompletedAppointment = await _uow.Appointments.AnyAsync(a =>
                        a.PatientProfileId == patient.Id &&
                        a.DoctorProfileId == doctorId &&
                        a.Status == AppointmentStatus.Completed);

                    if (!hasCompletedAppointment)
                        return ServiceResult<ReviewDto>.Failure("You can only review doctors with whom you have a completed appointment.");
                }

                if (dto.HealthcareProviderId.HasValue)
                {
                    var providerId = dto.HealthcareProviderId.Value;
                    var affiliations = await _uow.DoctorProviders.GetByProviderIdAsync(providerId);
                    var affiliatedDoctorIds = affiliations.Select(a => a.DoctorProfileId).ToList();

                    var hasCompletedAppointment = await _uow.Appointments.AnyAsync(a =>
                        a.PatientProfileId == patient.Id &&
                        a.DoctorProfileId.HasValue &&
                        affiliatedDoctorIds.Contains(a.DoctorProfileId.Value) &&
                        a.Status == AppointmentStatus.Completed);

                    if (!hasCompletedAppointment)
                        return ServiceResult<ReviewDto>.Failure("You can only review healthcare providers where you have had a completed appointment with one of their doctors.");
                }

                // Create review
                var review = new Review
                {
                    PatientProfileId = patient.Id,
                    DoctorProfileId = dto.DoctorProfileId,
                    HealthcareProviderId = dto.HealthcareProviderId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Reviews.AddAsync(review);
                await _uow.CompleteAsync();

                // Build DTO manually or using Mapster to include PatientName
                var reviewDto = new ReviewDto
                {
                    Id = review.Id,
                    PatientProfileId = review.PatientProfileId,
                    PatientName = patient.FullName ?? string.Empty,
                    DoctorProfileId = review.DoctorProfileId,
                    HealthcareProviderId = review.HealthcareProviderId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt
                };

                return ServiceResult<ReviewDto>.Success(reviewDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<ReviewDto>.Failure($"Failed to add review: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ReviewDto>>> GetDoctorReviewsAsync(int doctorId)
        {
            try
            {
                var reviews = await _uow.Reviews.GetByDoctorIdAsync(doctorId);
                
                // Let's load patient names
                var reviewDtos = new List<ReviewDto>();
                foreach (var review in reviews)
                {
                    var patient = await _uow.PatientProfiles.GetByIdAsync(review.PatientProfileId);
                    reviewDtos.Add(new ReviewDto
                    {
                        Id = review.Id,
                        PatientProfileId = review.PatientProfileId,
                        PatientName = patient?.FullName ?? "مريض",
                        DoctorProfileId = review.DoctorProfileId,
                        HealthcareProviderId = review.HealthcareProviderId,
                        Rating = review.Rating,
                        Comment = review.Comment,
                        CreatedAt = review.CreatedAt
                    });
                }

                return ServiceResult<List<ReviewDto>>.Success(reviewDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ReviewDto>>.Failure($"Failed to get doctor reviews: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ReviewDto>>> GetProviderReviewsAsync(int providerId)
        {
            try
            {
                var reviews = await _uow.Reviews.GetByProviderIdAsync(providerId);
                var reviewDtos = new List<ReviewDto>();
                foreach (var review in reviews)
                {
                    var patient = await _uow.PatientProfiles.GetByIdAsync(review.PatientProfileId);
                    reviewDtos.Add(new ReviewDto
                    {
                        Id = review.Id,
                        PatientProfileId = review.PatientProfileId,
                        PatientName = patient?.FullName ?? "مريض",
                        DoctorProfileId = review.DoctorProfileId,
                        HealthcareProviderId = review.HealthcareProviderId,
                        Rating = review.Rating,
                        Comment = review.Comment,
                        CreatedAt = review.CreatedAt
                    });
                }

                return ServiceResult<List<ReviewDto>>.Success(reviewDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ReviewDto>>.Failure($"Failed to get provider reviews: {ex.Message}");
            }
        }

        public async Task<ServiceResult<double>> GetDoctorAverageRatingAsync(int doctorId)
        {
            try
            {
                var avg = await _uow.Reviews.GetAverageDoctorRatingAsync(doctorId);
                return ServiceResult<double>.Success(avg);
            }
            catch (Exception ex)
            {
                return ServiceResult<double>.Failure($"Failed to get doctor average rating: {ex.Message}");
            }
        }

        public async Task<ServiceResult<double>> GetProviderAverageRatingAsync(int providerId)
        {
            try
            {
                var avg = await _uow.Reviews.GetAverageProviderRatingAsync(providerId);
                return ServiceResult<double>.Success(avg);
            }
            catch (Exception ex)
            {
                return ServiceResult<double>.Failure($"Failed to get provider average rating: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CanPatientReviewDoctorAsync(string userId, int doctorId)
        {
            try
            {
                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<bool>.Success(false);

                var hasCompletedAppointment = await _uow.Appointments.AnyAsync(a =>
                    a.PatientProfileId == patient.Id &&
                    a.DoctorProfileId == doctorId &&
                    a.Status == AppointmentStatus.Completed);

                return ServiceResult<bool>.Success(hasCompletedAppointment);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Failed to verify eligibility: {ex.Message}");
            }
        }
    }
}
