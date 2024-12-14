using Microsoft.AspNetCore.Mvc;
using SefCloud.Backend.Models;

namespace SefCloud.Backend.Services
{
    public class AuthService
    {
        private readonly TokenService _tokenService;

        public AuthService(TokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public (ApplicationUser user, bool IsValid) ValidateAuthorizationHeader(string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return (null, false);
            }

            string authToken = authHeader["Bearer ".Length..].Trim();

            var tokenCheck = _tokenService.ValidateToken(authToken);
            if (!tokenCheck.isValid)
            {
                return (null, false);
            }

            return (tokenCheck.user, true);
        }
    }
}
