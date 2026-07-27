using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Etmen_BLL.Repositories.IServices;
using Etmen_BLL.Repositories.Services;
using Etmen_BLL.Helpers;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etmen_Tests
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task ResendActivationEmailAsync_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            userManagerMock
                .Setup(m => m.FindByEmailAsync("notfound@example.com"))
                .ReturnsAsync((ApplicationUser)null);

            var uowMock = new Mock<IUnitOfWork>();
            var loggerMock = new Mock<ILogger<AuthService>>();
            var emailMock = new Mock<IEmailService>();
            var configMock = new Mock<IConfiguration>();
            var queueMock = new Mock<IBackgroundTaskQueue>();
            var httpContextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();

            var service = new AuthService(
                userManagerMock.Object,
                null, // signInManager
                uowMock.Object,
                loggerMock.Object,
                emailMock.Object,
                configMock.Object,
                queueMock.Object,
                httpContextAccessorMock.Object
            );

            // Act
            var result = await service.ResendActivationEmailAsync("notfound@example.com");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("المستخدم غير موجود.", result.ErrorMessage);
        }

        [Fact]
        public async Task ResendActivationEmailAsync_UserAlreadyConfirmed_ReturnsFailure()
        {
            // Arrange
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var confirmedUser = new ApplicationUser
            {
                Email = "confirmed@example.com",
                EmailConfirmed = true
            };

            userManagerMock
                .Setup(m => m.FindByEmailAsync("confirmed@example.com"))
                .ReturnsAsync(confirmedUser);

            var uowMock = new Mock<IUnitOfWork>();
            var loggerMock = new Mock<ILogger<AuthService>>();
            var emailMock = new Mock<IEmailService>();
            var configMock = new Mock<IConfiguration>();
            var queueMock = new Mock<IBackgroundTaskQueue>();
            var httpContextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();

            var service = new AuthService(
                userManagerMock.Object,
                null,
                uowMock.Object,
                loggerMock.Object,
                emailMock.Object,
                configMock.Object,
                queueMock.Object,
                httpContextAccessorMock.Object
            );

            // Act
            var result = await service.ResendActivationEmailAsync("confirmed@example.com");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("البريد الإلكتروني مؤكد بالفعل.", result.ErrorMessage);
        }
    }
}
