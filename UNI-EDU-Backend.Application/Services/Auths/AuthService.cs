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
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Services.Auths
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IGenericRepository<RefreshToken> _genericRepository;
        private readonly IMapper _autoMapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContext;

        public AuthService(
            IAuthRepository authRepository,
            IStudentRepository studentRepository,
            ITutorRepository tutorRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IMapper autoMapper,
            IGenericRepository<RefreshToken> genericRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IHttpContextAccessor httpContext)
        {
            this._authRepository = authRepository;
            this._studentRepository = studentRepository;
            this._tutorRepository = tutorRepository;
            this._refreshTokenRepository = refreshTokenRepository;
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
                throw new Exceptions.UnauthorizedAccessException("Invalid email or password.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.HashedPassword); 
            if (!isPasswordValid)
            {
                throw new Exceptions.UnauthorizedAccessException("Invalid email or password.");
            }
            return await GenerateToken(user);
        }

        public async Task<bool> RegisterStudentAsync(StudentRegister registerDto)
        {
            var existedUser = await _authRepository.IsPhonenumberOrEmailTakenAsync(registerDto.PhoneNumber, registerDto.Email);
            if (existedUser)
            {
                throw new Exceptions.BadRequestException("Phone number or email is already taken.");
            }

            var userEntity = _autoMapper.Map<User>(registerDto);
            userEntity.UserID = Guid.NewGuid();
            userEntity.HashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var student = _autoMapper.Map<Student>(registerDto);
            student.StudentID = userEntity.UserID;

            student.User = userEntity;

            await _authRepository.AddAsync(userEntity);
            await _studentRepository.AddAsync(student);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RegisterTutorAsync(TutorRegister registerDto)
        {
            var existedUser = await _authRepository.IsPhonenumberOrEmailTakenAsync(registerDto.PhoneNumber, registerDto.Email);
            if (existedUser)
            {
                throw new Exceptions.BadRequestException("Phone number or email is already taken.");
            }

            var userEntity = _autoMapper.Map<User>(registerDto);
            userEntity.UserID = Guid.NewGuid();
            userEntity.HashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var tutor = _autoMapper.Map<Tutor>(registerDto);
            tutor.TutorID = userEntity.UserID;

            tutor.User = userEntity;

            await _authRepository.AddAsync(userEntity);
            await _tutorRepository.AddAsync(tutor);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var existToken = await _refreshTokenRepository.GetFirstOrDefaultAsync(x => x.Token == request.RefreshToken);

            if(existToken == null || existToken.IsUsed || existToken.IsRevoked || existToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new Exceptions.UnauthorizedAccessException("Invalid refresh token.");
            }

            existToken.IsRevoked = true;
            existToken.IsUsed = true;
            _refreshTokenRepository.Update(existToken);
            await _unitOfWork.SaveChangesAsync();

            var user = await _authRepository.GetByIdAsync(existToken.UserID);
            if (user == null)
            {
                throw new Exceptions.UnauthorizedAccessException("User not found.");
            }

            return await GenerateToken(user);
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
                JwtId = jwtId,
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