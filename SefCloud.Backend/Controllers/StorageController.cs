using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SefCloud.Backend.DTOs;
using SefCloud.Backend.Models;
using SefCloud.Backend.Services;
using SefCloud.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace SefCloud.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryptionService;
        private readonly AuthService _authService;


        public StorageController(TokenService tokenService, ApplicationDbContext context, EncryptionService encryptionService, AuthService authService)
        {
            _tokenService = tokenService;
            _context = context;
            _encryptionService = encryptionService;
            _authService = authService;
        }


        [HttpPost("create-container")]
        public async Task<IActionResult> CreateContainer([FromBody] CreateStorageContainerRequest createStorageContainerRequest)
        {
            var authHeaderCheck = _authService.ValidateAuthorizationHeader(createStorageContainerRequest.Authorization);

            if (authHeaderCheck.IsValid == false)
            {
                return BadRequest(new { success = false });
            }

            ApplicationUser user = authHeaderCheck.user;

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
            var authHeaderCheck = _authService.ValidateAuthorizationHeader(Request.Headers["Authorization"]);

            if (authHeaderCheck.IsValid == false)
            {
                return BadRequest(new { success = false });
            }

            ApplicationUser user = authHeaderCheck.user;

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


        [HttpPost("upload-files")]
        public async Task<IActionResult> UploadFiles([FromForm] UploadStorageContainerItemRequest uploadStorageContainerItemRequest)
        {
            var authHeaderCheck = _authService.ValidateAuthorizationHeader(uploadStorageContainerItemRequest.Authorization);

            if (authHeaderCheck.IsValid == false)
            {
                return BadRequest(new { success = false });
            }

            ApplicationUser user = authHeaderCheck.user;
            var storageContainer = await _context.StorageContainers
                .Where(container => container.Id == uploadStorageContainerItemRequest.StorageContainerId)
                .Select(container => new
                {
                    UserId = container.UserId,
                    Name = container.Name,
                    EncryptionKey = container.EncryptionKey,
                })
                .FirstOrDefaultAsync();

            if (storageContainer == null)
            {
                return BadRequest(new { success = false });
            }

            if (uploadStorageContainerItemRequest.Files == null || uploadStorageContainerItemRequest.Files.Count == 0)
            {
                return BadRequest(new { success = false });
            }

            foreach(IFormFile file in uploadStorageContainerItemRequest.Files)
            {
                var fileSize = file.Length;

                if(fileSize == 0)
                {
                    continue;
                }

                var folderName = uploadStorageContainerItemRequest.StorageContainerId + storageContainer.Name.Replace(" ", "") + user.Id;
                var encryptedFolderName = _encryptionService.Encrypt(folderName, storageContainer.EncryptionKey);

                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                string fileExtension = Path.GetExtension(file.FileName);
                string encryptedFileName = _encryptionService.Encrypt(fileName, storageContainer.EncryptionKey);
                string fullEncrypedFileName = encryptedFileName + fileExtension;

                var filePath = Path.Combine("Storage", encryptedFolderName, fullEncrypedFileName);

                if (System.IO.File.Exists(filePath))
                {
                    return Conflict(new { success = false, message = "One or multiple files with the same name already exist in this container." });
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                StorageContainerItem storageContainerItem = new StorageContainerItem
                {
                    ContainerId = uploadStorageContainerItemRequest.StorageContainerId,
                    FileName = fullEncrypedFileName,
                    FileSize = fileSize,
                };
                _context.StorageContainerItems.Add(storageContainerItem);
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }
    }
}
