using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModsService.Models;
using ModsService.Repositories;
using ModsService.Services;
using Moq;
using Xunit;
using MongoDB.Bson;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using Shared.Models;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace ModsService.Tests.Services
{
    public class ModServiceTests
    {
        private readonly Mock<IModRepository> _mockModRepository;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IBlobStorageService> _mockBlobStorageService;
        private readonly ModService _modService;

        public ModServiceTests()
        {
            _mockModRepository = new Mock<IModRepository>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockBlobStorageService = new Mock<IBlobStorageService>();
            
            _mockConfiguration.Setup(x => x["Storage:BlobContainerName"]).Returns("mods");
            _mockConfiguration.Setup(x => x["Storage:ModsImageContainer"]).Returns("mods-images");
            
            _modService = new ModService(_mockModRepository.Object, _mockBlobStorageService.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task GetAllMods_ReturnsAllMods()
        {
            // Arrange
            var mods = new List<Mod>
            {
                new Mod
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = "Test Mod 1",
                    Description = "Test Description 1",
                    GameId = "game1",
                    CreatorId = "creator1",
                    Version = "1.0.0",
                    DownloadCount = 100,
                    Rating = 4.5,
                    RatingCount = 20,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Mod
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = "Test Mod 2",
                    Description = "Test Description 2",
                    GameId = "game2",
                    CreatorId = "creator2",
                    Version = "2.0.0",
                    DownloadCount = 200,
                    Rating = 4.8,
                    RatingCount = 50,
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            _mockModRepository.Setup(repo => repo.GetAllModsAsync())
                .ReturnsAsync(mods);

            // Act
            var result = await _modService.GetAllModsAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Mods récupérés avec succès", result.Message);
            Assert.Equal(2, result.Data.Count());
            Assert.Equal("Test Mod 1", result.Data.ElementAt(0).Name);
            Assert.Equal("Test Mod 2", result.Data.ElementAt(1).Name);
        }

        [Fact]
        public async Task GetModById_WithValidId_ReturnsMod()
        {
            // Arrange
            var modId = ObjectId.GenerateNewId().ToString();
            var mod = new Mod
            {
                Id = modId,
                Name = "Test Mod",
                Description = "Test Description",
                GameId = "game1",
                CreatorId = "creator1",
                Version = "1.0.0"
            };

            _mockModRepository.Setup(repo => repo.GetModByIdAsync(modId))
                .ReturnsAsync(mod);

            // Act
            var result = await _modService.GetModByIdAsync(modId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Mod récupéré avec succès", result.Message);
            Assert.Equal(modId, result.Data.Id);
            Assert.Equal("Test Mod", result.Data.Name);
        }

        [Fact]
        public async Task GetModById_WithInvalidId_ReturnsError()
        {
            // Arrange
            var invalidModId = "invalid_id";

            _mockModRepository.Setup(repo => repo.GetModByIdAsync(invalidModId))
                .ReturnsAsync((Mod)null);

            // Act
            var result = await _modService.GetModByIdAsync(invalidModId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Mod non trouvé", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task CreateMod_WithValidData_ReturnsMod()
        {
            // Arrange
            var createModRequest = new CreateModRequest
            {
                Name = "New Mod",
                Description = "New Description",
                GameId = "game1",
                CreatorId = "creator1",
                Version = "1.0.0"
            };

            var createdMod = new Mod
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = createModRequest.Name,
                Description = createModRequest.Description,
                GameId = createModRequest.GameId,
                CreatorId = createModRequest.CreatorId,
                Version = createModRequest.Version,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockModRepository.Setup(repo => repo.CreateModAsync(It.IsAny<Mod>()))
                .ReturnsAsync(createdMod);

            // Act
            var result = await _modService.CreateModAsync(createModRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Mod créé avec succès", result.Message);
            Assert.NotNull(result.Data.Id);
            Assert.Equal("New Mod", result.Data.Name);
            Assert.Equal("New Description", result.Data.Description);
        }

        [Fact]
        public async Task UpdateMod_WithValidData_ReturnsUpdatedMod()
        {
            // Arrange
            var modId = ObjectId.GenerateNewId().ToString();
            var updateModRequest = new UpdateModRequest
            {
                Name = "Updated Mod",
                Description = "Updated Description",
                Version = "1.1.0"
            };

            var existingMod = new Mod
            {
                Id = modId,
                Name = "Original Mod",
                Description = "Original Description",
                GameId = "game1",
                CreatorId = "creator1",
                Version = "1.0.0",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var updatedMod = new Mod
            {
                Id = modId,
                Name = updateModRequest.Name,
                Description = updateModRequest.Description,
                GameId = existingMod.GameId,
                CreatorId = existingMod.CreatorId,
                Version = updateModRequest.Version,
                CreatedAt = existingMod.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _mockModRepository.Setup(repo => repo.GetModByIdAsync(modId))
                .ReturnsAsync(existingMod);

            _mockModRepository.Setup(repo => repo.UpdateModAsync(It.IsAny<Mod>()))
                .ReturnsAsync(updatedMod);

            // Act
            var result = await _modService.UpdateModAsync(modId, updateModRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Mod mis à jour avec succès", result.Message);
            Assert.Equal(modId, result.Data.Id);
            Assert.Equal("Updated Mod", result.Data.Name);
            Assert.Equal("Updated Description", result.Data.Description);
            Assert.Equal("1.1.0", result.Data.Version);
        }

        [Fact]
        public async Task UpdateMod_WithInvalidId_ReturnsError()
        {
            // Arrange
            var invalidModId = "invalid_id";
            var updateModRequest = new UpdateModRequest
            {
                Name = "Updated Mod",
                Description = "Updated Description"
            };

            _mockModRepository.Setup(repo => repo.GetModByIdAsync(invalidModId))
                .ReturnsAsync((Mod)null);

            // Act
            var result = await _modService.UpdateModAsync(invalidModId, updateModRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Mod non trouvé", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task DeleteMod_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var modId = ObjectId.GenerateNewId().ToString();
            var existingMod = new Mod
            {
                Id = modId,
                Name = "Mod to Delete",
                FileUrl = "https://storage.example.com/mods/file.zip",
                ImageUrl = "https://storage.example.com/mods-images/image.jpg"
            };

            _mockModRepository.Setup(repo => repo.GetModByIdAsync(modId))
                .ReturnsAsync(existingMod);

            _mockModRepository.Setup(repo => repo.DeleteModAsync(modId))
                .ReturnsAsync(true);
                
            _mockBlobStorageService.Setup(service => service.DeleteBlobAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _modService.DeleteModAsync(modId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Mod supprimé avec succès", result.Message);
            _mockBlobStorageService.Verify(service => service.DeleteBlobAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteMod_WithInvalidId_ReturnsError()
        {
            // Arrange
            var invalidModId = "invalid_id";

            _mockModRepository.Setup(repo => repo.GetModByIdAsync(invalidModId))
                .ReturnsAsync((Mod)null);

            // Act
            var result = await _modService.DeleteModAsync(invalidModId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Mod non trouvé", result.Message);
        }

        [Fact]
        public async Task UploadModFile_WithValidFile_ReturnsSuccess()
        {
            // Arrange
            var modId = ObjectId.GenerateNewId().ToString();
            var fileName = "test-mod.zip";
            var fileContent = Encoding.UTF8.GetBytes("test file content");
            var ms = new MemoryStream(fileContent);
            
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.FileName).Returns(fileName);
            formFile.Setup(f => f.Length).Returns(fileContent.Length);
            formFile.Setup(f => f.OpenReadStream()).Returns(ms);
            formFile.Setup(f => f.ContentType).Returns("application/zip");

            var existingMod = new Mod
            {
                Id = modId,
                Name = "Test Mod",
                GameId = "game1",
                CreatorId = "creator1"
            };

            var updatedMod = new Mod
            {
                Id = modId,
                Name = "Test Mod",
                GameId = "game1",
                CreatorId = "creator1",
                FileUrl = $"https://storage.example.com/mods/{modId}-{fileName}",
                FileSize = fileContent.Length,
                UpdatedAt = DateTime.UtcNow
            };

            _mockModRepository.Setup(repo => repo.GetModByIdAsync(modId))
                .ReturnsAsync(existingMod);

            _mockBlobStorageService
                .Setup(service => service.UploadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>()))
                .ReturnsAsync($"https://storage.example.com/mods/{modId}-{fileName}");

            _mockModRepository.Setup(repo => repo.UpdateModAsync(It.IsAny<Mod>()))
                .ReturnsAsync(updatedMod);

            // Act
            var result = await _modService.UploadModFileAsync(modId, formFile.Object);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Fichier mod uploadé avec succès", result.Message);
            Assert.Equal($"https://storage.example.com/mods/{modId}-{fileName}", result.Data.FileUrl);
            Assert.Equal(fileContent.Length, result.Data.FileSize);
        }
        
        [Fact]
        public async Task SearchMods_WithValidParameters_ReturnsFilteredMods()
        {
            // Arrange
            var searchParams = new ModSearchParams
            {
                GameId = "game1",
                CategoryId = "category1",
                SearchTerm = "test",
                SortBy = "downloads",
                PageNumber = 1,
                PageSize = 10
            };
            
            var mods = new List<Mod>
            {
                new Mod
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = "Test Mod 1",
                    Description = "Test Description 1",
                    GameId = "game1",
                    CategoryId = "category1",
                    CreatorId = "creator1",
                    DownloadCount = 200,
                    Tags = new List<string> { "test", "gameplay" }
                },
                new Mod
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = "Another Test Mod",
                    Description = "Another Description",
                    GameId = "game1",
                    CategoryId = "category1",
                    CreatorId = "creator2",
                    DownloadCount = 150,
                    Tags = new List<string> { "test", "graphics" }
                }
            };
            
            var totalCount = mods.Count;
            
            _mockModRepository.Setup(repo => repo.SearchModsAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync((mods, totalCount));
                
            // Act
            var result = await _modService.SearchModsAsync(searchParams);
            
            // Assert
            Assert.True(result.Success);
            Assert.Equal("Mods récupérés avec succès", result.Message);
            Assert.Equal(2, result.Data.Items.Count());
            Assert.Equal(2, result.Data.TotalCount);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Equal(10, result.Data.PageSize);
        }
    }
}
