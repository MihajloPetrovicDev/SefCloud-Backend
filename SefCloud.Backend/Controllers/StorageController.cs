using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SefCloud.Backend.DTOs;
using SefCloud.Backend.Models;
using SefCloud.Backend.Services;
using SefCloud.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace SefCloud.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryptionService;


        public StorageController(TokenService tokenService, ApplicationDbContext context, EncryptionService encryptionService)
        {
            _tokenService = tokenService;
            _context = context;
            _encryptionService = encryptionService;
        }


        [HttpPost("create-container")]
        public async Task<IActionResult> CreateContainer([FromBody] CreateStorageContainerRequest createStorageContainerRequest)
        {
            var tokenCheck = _tokenService.ValidateToken(createStorageContainerRequest.AuthToken);
            if (!tokenCheck.isValid)
            {
                return Unauthorized(new { success = false });
            }

            ApplicationUser user = tokenCheck.user;

            StorageContainer storageContainer = new StorageContainer
            {
                UserId = user.Id,
                Name = createStorageContainerRequest.StorageContainerName,
                EncryptionKey = _encryptionService.GenerateEncryptionKey(),
            };
            _context.StorageContainers.Add(storageContainer);
            await _context.SaveChangesAsync();

            var folderName = storageContainer.Id + storageContainer.Name.Replace(" ", "") + storageContainer.UserId;
            var encryptedFolderName = _encryptionService.Encrypt(folderName, storageContainer.EncryptionKey);
            var filePath = Path.Combine("Storage", encryptedFolderName);
            Directory.CreateDirectory(filePath);

            return Ok(new { success = true });
        }


        [HttpGet("get-user-containers")]
        public async Task<IActionResult> GetUserContainers()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if(string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { success = false });
            }

            string authtoken = authHeader["Bearer ".Length..].Trim();
            var tokenCheck = _tokenService.ValidateToken(authtoken);
            if (!tokenCheck.isValid)
            {
                return Unauthorized(new { success = false });
            }

            ApplicationUser user = tokenCheck.user;

            var storageContainers = await _context.StorageContainers
                .Where(container => container.UserId == user.Id)
                .Select(container => new
                {
                    Id = container.Id,
                    Name = container.Name,
                    CreatedAt = container.CreatedAt,
                    UpdatedAt = container.UpdatedAt,
                })
                .ToListAsync();

            return Ok(new { success = true, containers = storageContainers });
        }
    }
}
