using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Etmen_BLL.Repositories.Services;
using Etmen_BLL.DTOs.Nearby;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Etmen_Tests
{
    public class NearbyServiceTests
    {
        [Fact]
        public async Task BookAppointmentAsync_SuccessfulBooking_ReturnsSuccessResult()
        {
            // Arrange
            var uowMock = new Mock<IUnitOfWork>();
            var slotRepoMock = new Mock<IAvailableSlotRepository>();
            var patientRepoMock = new Mock<IPatientProfileRepository>();
            var doctorRepoMock = new Mock<IDoctorProfileRepository>();
            var appointmentRepoMock = new Mock<IAppointmentRepository>();

            var bookingDto = new BookingRequestDto
            {
                PatientProfileId = 1,
                DoctorId = 2,
                SlotId = 3,
                Date = DateTime.UtcNow.AddDays(1),
                StartTime = TimeSpan.Parse("10:00"),
                EndTime = TimeSpan.Parse("10:30"),
                Notes = "Routine Checkup"
            };

            var slot = new AvailableSlot { Id = 3, DoctorProfileId = 2, IsBooked = false };
            var patient = new PatientProfile { Id = 1 };
            var doctor = new DoctorProfile { Id = 2 };

            slotRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(slot);
            patientRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(patient);
            doctorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(doctor);

            uowMock.Setup(u => u.AvailableSlots).Returns(slotRepoMock.Object);
            uowMock.Setup(u => u.PatientProfiles).Returns(patientRepoMock.Object);
            uowMock.Setup(u => u.DoctorProfiles).Returns(doctorRepoMock.Object);
            uowMock.Setup(u => u.Appointments).Returns(appointmentRepoMock.Object);
            uowMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1); // Normal successful save

            var service = new NearbyService(uowMock.Object);

            // Act
            var result = await service.BookAppointmentAsync(bookingDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.True(slot.IsBooked); // Verify slot is marked as booked
        }

        [Fact]
        public async Task BookAppointmentAsync_DatabaseConcurrencyConflict_CatchesConflictAndReturnsFriendlyError()
        {
            // Arrange
            var uowMock = new Mock<IUnitOfWork>();
            var slotRepoMock = new Mock<IAvailableSlotRepository>();
            var patientRepoMock = new Mock<IPatientProfileRepository>();
            var doctorRepoMock = new Mock<IDoctorProfileRepository>();
            var appointmentRepoMock = new Mock<IAppointmentRepository>();

            var bookingDto = new BookingRequestDto
            {
                PatientProfileId = 1,
                DoctorId = 2,
                SlotId = 3,
                Date = DateTime.UtcNow.AddDays(1),
                StartTime = TimeSpan.Parse("10:00"),
                EndTime = TimeSpan.Parse("10:30")
            };

            var slot = new AvailableSlot { Id = 3, DoctorProfileId = 2, IsBooked = false };
            var patient = new PatientProfile { Id = 1 };
            var doctor = new DoctorProfile { Id = 2 };

            slotRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(slot);
            patientRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(patient);
            doctorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(doctor);

            uowMock.Setup(u => u.AvailableSlots).Returns(slotRepoMock.Object);
            uowMock.Setup(u => u.PatientProfiles).Returns(patientRepoMock.Object);
            uowMock.Setup(u => u.DoctorProfiles).Returns(doctorRepoMock.Object);
            uowMock.Setup(u => u.Appointments).Returns(appointmentRepoMock.Object);

            // Simulate database concurrency conflict
            uowMock
                .Setup(u => u.CompleteAsync())
                .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency conflict occurred."));

            var service = new NearbyService(uowMock.Object);

            // Act
            var result = await service.BookAppointmentAsync(bookingDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("عذراً، هذا الموعد تم حجزه للتو من قبل مريض آخر.", result.ErrorMessage);
        }
    }
}
