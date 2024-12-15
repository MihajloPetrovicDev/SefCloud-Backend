using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SefCloud.Backend.DTOs;
using SefCloud.Backend.Models;
using SefCloud.Backend.Services;
using SefCloud.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.ComponentModel;

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


        public StorageController(TokenService tokenService, 
                                ApplicationDbContext context, 
                                EncryptionService encryptionService, 
                                AuthService authService)
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
                return Unauthorized(new { success = false });
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
                return Unauthorized(new { success = false });
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
                return Unauthorized(new { success = false });
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


        [HttpPost("get-container-items")]
        public async Task<IActionResult> GetContainerItems(GetContainerItemsRequest getContainerItemsRequest)
        {
            var authHeaderCheck = _authService.ValidateAuthorizationHeader(getContainerItemsRequest.Authorization);

            if (authHeaderCheck.IsValid == false)
            {
                return Unauthorized(new { success = false });
            }

            ApplicationUser user = authHeaderCheck.user;

            var storageContainer = await _context.StorageContainers
                .Where(container => container.Id == getContainerItemsRequest.StorageContainerId)
                .Select(container => new
                {
                    UserId = container.UserId,
                    EncryptionKey = container.EncryptionKey,
                    Name = container.Name,
                    CreatedAt = container.CreatedAt,
                    UpdatedAt = container.UpdatedAt,
                })
                .FirstOrDefaultAsync();

            if(storageContainer == null)
            {
                return BadRequest(new { success = false });
            }

            if(storageContainer.UserId != user.Id)
            {
                return Unauthorized(new { success = false });
            }

            var storageContainerItems = await _context.StorageContainerItems
                .Where(containerItem => containerItem.ContainerId == getContainerItemsRequest.StorageContainerId)
                .Select(containerItem => new
                {
                    Id = containerItem.Id,
                    FileName = _encryptionService.DecryptFileName(containerItem.FileName, storageContainer.EncryptionKey),
                    FileSize = containerItem.FileSize,
                    createdAt = containerItem.CreatedAt,
                })
                .ToListAsync();

            var storageItemToSend = new
            {
                Name = storageContainer.Name,
                CreatedAt = storageContainer.CreatedAt,
                UpdatedAt = storageContainer.UpdatedAt
            };

            return Ok(new { success = true, containerItems = storageContainerItems, storageContainer = storageItemToSend});
        }


        [HttpPost("download-file")]
        public async Task<IActionResult> DownloadStorageContainerItem(DownloadStorageContainerItemRequest downloadStorageContainerItemRequest)
        {
            var authHeaderCheck = _authService.ValidateAuthorizationHeader(downloadStorageContainerItemRequest.Authorization);

            if (authHeaderCheck.IsValid == false)
            {
                return Unauthorized(new { success = false });
            }

            ApplicationUser user = authHeaderCheck.user;

            var storageContainerItem = await _context.StorageContainerItems
                .Where(containerItem => containerItem.Id == downloadStorageContainerItemRequest.StorageContainerItemId)
                .Select(containerItem => new
                {
                    FileName = containerItem.FileName,
                    ContainerId = containerItem.ContainerId,
                })
                .FirstOrDefaultAsync();

            if (storageContainerItem == null)
            {
                return BadRequest(new { success = false });
            }

            var storageContainer = await _context.StorageContainers
                .Where(container => container.Id == storageContainerItem.ContainerId)
                .Select(container => new
                {
                    Id = container.Id,
                    UserId = container.UserId,
                    EncryptionKey = container.EncryptionKey,
                    Name = container.Name,
                })
                .FirstOrDefaultAsync();

            if (storageContainer == null) 
            {
                return BadRequest(new { success = false });
            }

            if(storageContainer.UserId != user.Id)
            {
                return Unauthorized(new { success = false });
            }

            var folderName = storageContainer.Id + storageContainer.Name.Replace(" ", "") + storageContainer.UserId;
            var encryptedFolderName = _encryptionService.Encrypt(folderName, storageContainer.EncryptionKey);
            var filePath = Path.Combine("Storage", encryptedFolderName, storageContainerItem.FileName);

            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest(new { success = false, message = "File doesn't exist." });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = _encryptionService.DecryptFileName(storageContainerItem.FileName, storageContainer.EncryptionKey);

            return File(fileBytes, "application/octet-stream", fileName);
        }


        [HttpPost("delete-file")]
        public async Task<IActionResult> DeleteStorageContainerItem(DeleteStorageContainerItemRequest deleteStorageContainerItemRequest)
        {
            var authHeaderCheck = _authService.ValidateAuthorizationHeader(deleteStorageContainerItemRequest.Authorization);

            if (authHeaderCheck.IsValid == false)
            {
                return Unauthorized(new { success = false });
            }

            ApplicationUser user = authHeaderCheck.user;

            var storageContainerItemToDelete = await _context.StorageContainerItems
                .Where(containerItem => containerItem.Id == deleteStorageContainerItemRequest.StorageContainerItemId)
                .FirstOrDefaultAsync();

            var storageContainerItem = new
            {
                FileName = storageContainerItemToDelete.FileName,
                ContainerId = storageContainerItemToDelete.ContainerId,
            };

            if (storageContainerItem == null)
            {
                return BadRequest(new { success = false });
            }

            var storageContainer = await _context.StorageContainers
                .Where(container => container.Id == storageContainerItem.ContainerId)
                .Select(container => new
                {
                    Id = container.Id,
                    UserId = container.UserId,
                    EncryptionKey = container.EncryptionKey,
                    Name = container.Name,
                })
                .FirstOrDefaultAsync();

            if (storageContainer == null)
            {
                return BadRequest(new { success = false });
            }

            if (storageContainer.UserId != user.Id)
            {
                return Unauthorized(new { success = false });
            }

            var folderName = storageContainer.Id + storageContainer.Name.Replace(" ", "") + storageContainer.UserId;
            var encryptedFolderName = _encryptionService.Encrypt(folderName, storageContainer.EncryptionKey);
            var filePath = Path.Combine("Storage", encryptedFolderName, storageContainerItem.FileName);

            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest(new { success = false, message = "File doesn't exist." });
            }

            System.IO.File.Delete(filePath);

            _context.StorageContainerItems.Remove(storageContainerItemToDelete);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
