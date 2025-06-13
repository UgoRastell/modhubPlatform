using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ModsService.Models;
using ModsService.Repositories;
using ModsService.Services.Download;
using ModsService.Settings;
using Xunit;

namespace ModsService.Tests.Services
{
    public class DownloadQuotaServiceTests
    {
        private readonly Mock<IDownloadQuotaRepository> _mockQuotaRepository;
        private readonly Mock<IDownloadHistoryRepository> _mockHistoryRepository;
        private readonly Mock<ILogger<DownloadService>> _mockLogger;
        private readonly Mock<IOptions<QuotaSettings>> _mockQuotaSettings;
        private readonly DownloadService _downloadService;
        private readonly QuotaSettings _quotaSettings;
        
        public DownloadQuotaServiceTests()
        {
            _mockQuotaRepository = new Mock<IDownloadQuotaRepository>();
            _mockHistoryRepository = new Mock<IDownloadHistoryRepository>();
            _mockLogger = new Mock<ILogger<DownloadService>>();
            
            // Configuration par défaut des quotas
            _quotaSettings = new QuotaSettings
            {
                AnonymousQuotaPerDay = 5,
                RegisteredQuotaPerDay = 20,
                PremiumQuotaPerDay = 100,
                IpRateLimitPerHour = 10
            };
            
            _mockQuotaSettings = new Mock<IOptions<QuotaSettings>>();
            _mockQuotaSettings.Setup(x => x.Value).Returns(_quotaSettings);
            
            _downloadService = new DownloadService(
                _mockQuotaRepository.Object,
                _mockHistoryRepository.Object,
                _mockQuotaSettings.Object,
                _mockLogger.Object);
        }
        
        [Fact]
        public async Task RecordDownload_AnonymousUser_ShouldCheckIpQuota()
        {
            // Arrange
            var modId = "mod123";
            var versionNumber = "1.0.0";
            string userId = null; // Utilisateur anonyme
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
            var ipString = "192.168.1.1";
            
            var today = DateTime.UtcNow.Date;
            var quota = new DownloadQuota
            {
                Identifier = ipString,
                Type = QuotaType.IpAddress,
                CurrentCount = 3, // 3 téléchargements déjà effectués
                LastReset = today
            };
            
            _mockQuotaRepository.Setup(r => r.GetQuotaByIpAsync(ipString))
                .ReturnsAsync(quota);
                
            _mockQuotaRepository.Setup(r => r.IncrementQuotaAsync(It.IsAny<string>(), It.IsAny<QuotaType>(), It.IsAny<DateTime>()))
                .ReturnsAsync(quota);
            
            // Act
            var result = await _downloadService.RecordDownloadAsync(modId, versionNumber, userId, httpContext);
            
            // Assert
            Assert.True(result.IsAllowed);
            Assert.False(result.QuotaExceeded);
            Assert.Equal(_quotaSettings.AnonymousQuotaPerDay - quota.CurrentCount, result.RemainingQuota);
            _mockQuotaRepository.Verify(r => r.GetQuotaByIpAsync(ipString), Times.Once);
            _mockQuotaRepository.Verify(r => r.IncrementQuotaAsync(ipString, QuotaType.IpAddress, It.IsAny<DateTime>()), Times.Once);
        }
        
        [Fact]
        public async Task RecordDownload_AnonymousUser_ExceedsQuota_ShouldDeny()
        {
            // Arrange
            var modId = "mod123";
            var versionNumber = "1.0.0";
            string userId = null; // Utilisateur anonyme
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
            var ipString = "192.168.1.1";
            
            var today = DateTime.UtcNow.Date;
            var quota = new DownloadQuota
            {
                Identifier = ipString,
                Type = QuotaType.IpAddress,
                CurrentCount = _quotaSettings.AnonymousQuotaPerDay, // Quota atteint
                LastReset = today
            };
            
            _mockQuotaRepository.Setup(r => r.GetQuotaByIpAsync(ipString))
                .ReturnsAsync(quota);
            
            // Act
            var result = await _downloadService.RecordDownloadAsync(modId, versionNumber, userId, httpContext);
            
            // Assert
            Assert.False(result.IsAllowed);
            Assert.True(result.QuotaExceeded);
            Assert.Equal(0, result.RemainingQuota);
            _mockQuotaRepository.Verify(r => r.GetQuotaByIpAsync(ipString), Times.Once);
            _mockQuotaRepository.Verify(r => r.IncrementQuotaAsync(It.IsAny<string>(), It.IsAny<QuotaType>(), It.IsAny<DateTime>()), Times.Never);
        }
        
        [Fact]
        public async Task RecordDownload_RegisteredUser_ShouldCheckUserQuota()
        {
            // Arrange
            var modId = "mod123";
            var versionNumber = "1.0.0";
            var userId = "user123";
            var httpContext = new DefaultHttpContext();
            
            var today = DateTime.UtcNow.Date;
            var quota = new DownloadQuota
            {
                Identifier = userId,
                Type = QuotaType.UserId,
                CurrentCount = 10, // 10 téléchargements déjà effectués
                LastReset = today
            };
            
            _mockQuotaRepository.Setup(r => r.GetQuotaByUserIdAsync(userId))
                .ReturnsAsync(quota);
                
            _mockQuotaRepository.Setup(r => r.IncrementQuotaAsync(It.IsAny<string>(), It.IsAny<QuotaType>(), It.IsAny<DateTime>()))
                .ReturnsAsync(quota);
            
            // Act
            var result = await _downloadService.RecordDownloadAsync(modId, versionNumber, userId, httpContext);
            
            // Assert
            Assert.True(result.IsAllowed);
            Assert.False(result.QuotaExceeded);
            Assert.Equal(_quotaSettings.RegisteredQuotaPerDay - quota.CurrentCount, result.RemainingQuota);
            _mockQuotaRepository.Verify(r => r.GetQuotaByUserIdAsync(userId), Times.Once);
            _mockQuotaRepository.Verify(r => r.IncrementQuotaAsync(userId, QuotaType.UserId, It.IsAny<DateTime>()), Times.Once);
        }
        
        [Fact]
        public async Task RecordDownload_PremiumUser_ShouldHaveHigherQuota()
        {
            // Arrange
            var modId = "mod123";
            var versionNumber = "1.0.0";
            var userId = "premium123";
            var httpContext = new DefaultHttpContext();
            
            var today = DateTime.UtcNow.Date;
            var quota = new DownloadQuota
            {
                Identifier = userId,
                Type = QuotaType.UserId,
                CurrentCount = 50, // 50 téléchargements déjà effectués
                LastReset = today,
                IsPremium = true // Utilisateur premium
            };
            
            _mockQuotaRepository.Setup(r => r.GetQuotaByUserIdAsync(userId))
                .ReturnsAsync(quota);
                
            _mockQuotaRepository.Setup(r => r.IncrementQuotaAsync(It.IsAny<string>(), It.IsAny<QuotaType>(), It.IsAny<DateTime>()))
                .ReturnsAsync(quota);
            
            // Act
            var result = await _downloadService.RecordDownloadAsync(modId, versionNumber, userId, httpContext);
            
            // Assert
            Assert.True(result.IsAllowed);
            Assert.False(result.QuotaExceeded);
            Assert.Equal(_quotaSettings.PremiumQuotaPerDay - quota.CurrentCount, result.RemainingQuota);
            _mockQuotaRepository.Verify(r => r.GetQuotaByUserIdAsync(userId), Times.Once);
            _mockQuotaRepository.Verify(r => r.IncrementQuotaAsync(userId, QuotaType.UserId, It.IsAny<DateTime>()), Times.Once);
        }
        
        [Fact]
        public async Task RecordDownload_ShouldResetQuota_WhenLastResetNotToday()
        {
            // Arrange
            var modId = "mod123";
            var versionNumber = "1.0.0";
            var userId = "user123";
            var httpContext = new DefaultHttpContext();
            
            // Quota from yesterday
            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            var oldQuota = new DownloadQuota
            {
                Identifier = userId,
                Type = QuotaType.UserId,
                CurrentCount = 15,
                LastReset = yesterday
            };
            
            // New reset quota
            var today = DateTime.UtcNow.Date;
            var newQuota = new DownloadQuota
            {
                Identifier = userId,
                Type = QuotaType.UserId,
                CurrentCount = 1, // Reset to 1 after first download today
                LastReset = today
            };
            
            _mockQuotaRepository.Setup(r => r.GetQuotaByUserIdAsync(userId))
                .ReturnsAsync(oldQuota);
                
            _mockQuotaRepository.Setup(r => r.ResetQuotaAsync(userId, QuotaType.UserId, today))
                .ReturnsAsync(newQuota);
            
            // Act
            var result = await _downloadService.RecordDownloadAsync(modId, versionNumber, userId, httpContext);
            
            // Assert
            Assert.True(result.IsAllowed);
            Assert.False(result.QuotaExceeded);
            Assert.Equal(_quotaSettings.RegisteredQuotaPerDay - newQuota.CurrentCount, result.RemainingQuota);
            _mockQuotaRepository.Verify(r => r.GetQuotaByUserIdAsync(userId), Times.Once);
            _mockQuotaRepository.Verify(r => r.ResetQuotaAsync(userId, QuotaType.UserId, It.IsAny<DateTime>()), Times.Once);
        }
        
        [Fact]
        public async Task RecordDownload_ShouldCreateNewQuota_WhenNotExists()
        {
            // Arrange
            var modId = "mod123";
            var versionNumber = "1.0.0";
            var userId = "newuser123";
            var httpContext = new DefaultHttpContext();
            
            // Nouveau quota
            var today = DateTime.UtcNow.Date;
            var newQuota = new DownloadQuota
            {
                Identifier = userId,
                Type = QuotaType.UserId,
                CurrentCount = 1,
                LastReset = today
            };
            
            _mockQuotaRepository.Setup(r => r.GetQuotaByUserIdAsync(userId))
                .ReturnsAsync((DownloadQuota)null); // Aucun quota existant
                
            _mockQuotaRepository.Setup(r => r.CreateQuotaAsync(userId, QuotaType.UserId))
                .ReturnsAsync(newQuota);
            
            // Act
            var result = await _downloadService.RecordDownloadAsync(modId, versionNumber, userId, httpContext);
            
            // Assert
            Assert.True(result.IsAllowed);
            Assert.False(result.QuotaExceeded);
            Assert.Equal(_quotaSettings.RegisteredQuotaPerDay - newQuota.CurrentCount, result.RemainingQuota);
            _mockQuotaRepository.Verify(r => r.GetQuotaByUserIdAsync(userId), Times.Once);
            _mockQuotaRepository.Verify(r => r.CreateQuotaAsync(userId, QuotaType.UserId), Times.Once);
        }
        
        [Fact]
        public async Task GetModDownloadStatistics_ShouldReturnCorrectStats()
        {
            // Arrange
            var modId = "mod123";
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;
            
            var downloadHistory = new List<DownloadHistory>
            {
                new DownloadHistory { ModId = modId, VersionNumber = "1.0.0", DownloadedAt = DateTime.UtcNow.AddDays(-25) },
                new DownloadHistory { ModId = modId, VersionNumber = "1.0.0", DownloadedAt = DateTime.UtcNow.AddDays(-20) },
                new DownloadHistory { ModId = modId, VersionNumber = "1.1.0", DownloadedAt = DateTime.UtcNow.AddDays(-15) },
                new DownloadHistory { ModId = modId, VersionNumber = "1.1.0", DownloadedAt = DateTime.UtcNow.AddDays(-10) },
                new DownloadHistory { ModId = modId, VersionNumber = "1.1.0", DownloadedAt = DateTime.UtcNow.AddDays(-5) }
            };
            
            _mockHistoryRepository.Setup(r => r.GetDownloadHistoryAsync(modId, startDate, endDate))
                .ReturnsAsync(downloadHistory);
            
            // Act
            var result = await _downloadService.GetModDownloadStatisticsAsync(modId, startDate, endDate);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalDownloads);
            Assert.Equal(2, result.VersionDownloads.Count);
            Assert.Equal(2, result.VersionDownloads["1.0.0"]);
            Assert.Equal(3, result.VersionDownloads["1.1.0"]);
            Assert.Equal(5, result.DailyDownloads.Count); // 5 jours différents
            _mockHistoryRepository.Verify(r => r.GetDownloadHistoryAsync(modId, startDate, endDate), Times.Once);
        }
    }
}
