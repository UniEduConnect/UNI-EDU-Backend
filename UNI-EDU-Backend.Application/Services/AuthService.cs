using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UNI_EDU_Backend.Application.DTOs.Request.Authentication;
using UNI_EDU_Backend.Application.DTOs.Response;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Domain.Interfaces;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IGenericRepository<RefreshToken> _genericRepository;
        private readonly IMapper _autoMapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContext;

        public AuthService(
            IAuthRepository authRepository,
            IMapper autoMapper,
            IGenericRepository<RefreshToken> genericRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IHttpContextAccessor httpContext)
        {
            this._authRepository = authRepository;
            this._genericRepository = genericRepository;
            this._autoMapper = autoMapper;
            this._unitOfWork = unitOfWork;
            this._configuration = configuration;
            this._httpContext = httpContext;
        }
        public async Task<TokenResponse> LoginAsync(LoginRequest loginRequest)
        {
            var user = await _authRepository.GetUserByEmailAsync(loginRequest.Email);
            if (user == null)
            {
                throw new System.UnauthorizedAccessException();
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.HashedPassword); 
            if (!isPasswordValid)
            {
                throw new System.UnauthorizedAccessException();
            }
            return await GenerateToken(user);
        }

        public Task<User> RegisterStudentAsync(StudentRegister registerDto)
        {
            throw new NotImplementedException();
        }

        public Task<User> RegisterTutorAsync(TutorRegister registerDto)
        {
            throw new NotImplementedException();
        }

        private async Task<TokenResponse> GenerateToken(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var secretKeyByte = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);

            var jwtId = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jwtId)
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKeyByte), SecurityAlgorithms.HmacSha256)
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var accessTokenString = jwtTokenHandler.WriteToken(token);
            var refreshTokenEntity = new RefreshToken
            {
                Token = GenerateRefreshToken(),
                JwtId = accessTokenString,
                UserID = user.UserID,
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _genericRepository.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            SetRefreshTokenCookie(refreshTokenEntity.Token);

            return new TokenResponse
            {
                AccessToken = accessTokenString,
                RefreshToken = refreshTokenEntity.Token
            };
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
        private void SetRefreshTokenCookie(string refreshToken)
        {
            // Set the refresh token in a secure cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.Strict
            };
            _httpContext.HttpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}