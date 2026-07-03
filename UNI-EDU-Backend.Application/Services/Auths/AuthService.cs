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
        private readonly IParentRepository _parentRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IGenericRepository<RefreshToken> _genericRepository;
        private readonly IMapper _autoMapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContext;
        private readonly Otp.IEmailOtpService _emailOtpService;

        public AuthService(
            IAuthRepository authRepository,
            IStudentRepository studentRepository,
            ITutorRepository tutorRepository,
            IParentRepository parentRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IMapper autoMapper,
            IGenericRepository<RefreshToken> genericRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IHttpContextAccessor httpContext,
            Otp.IEmailOtpService emailOtpService)
        {
            this._authRepository = authRepository;
            this._studentRepository = studentRepository;
            this._tutorRepository = tutorRepository;
            this._parentRepository = parentRepository;
            this._refreshTokenRepository = refreshTokenRepository;
            this._genericRepository = genericRepository;
            this._autoMapper = autoMapper;
            this._unitOfWork = unitOfWork;
            this._configuration = configuration;
            this._httpContext = httpContext;
            this._emailOtpService = emailOtpService;
        }

        // Registration requires a recently OTP-verified email. Toggle off via Otp:RequireForRegister=false.
        private async Task EnsureEmailVerifiedForRegisterAsync(string email)
        {
            // Default true; disable with Otp:RequireForRegister=false.
            if (bool.TryParse(_configuration["Otp:RequireForRegister"], out var require) && !require)
                return;
            var verified = await _emailOtpService.IsEmailVerifiedAsync(email, "register", CancellationToken.None);
            if (!verified)
                throw new Exceptions.BadRequestException("Vui lòng xác thực email bằng mã OTP trước khi đăng ký.");
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

            // Block suspended/rejected accounts from authenticating (Pending tutors may still log in).
            if (user.Status == Domain.Enums.UserStatus.Suspended || user.Status == Domain.Enums.UserStatus.Rejected)
            {
                throw new Exceptions.ForbiddenAccessException("Tài khoản của bạn đã bị khóa hoặc từ chối. Vui lòng liên hệ hỗ trợ.");
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

            await EnsureEmailVerifiedForRegisterAsync(registerDto.Email);

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

            await EnsureEmailVerifiedForRegisterAsync(registerDto.Email);

            var userEntity = _autoMapper.Map<User>(registerDto);
            userEntity.UserID = Guid.NewGuid();
            userEntity.HashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
            // New tutors await admin approval (login is NOT gated — see User.Status).
            userEntity.Status = Domain.Enums.UserStatus.Pending;

            var tutor = _autoMapper.Map<Tutor>(registerDto);
            tutor.TutorID = userEntity.UserID;

            tutor.User = userEntity;

            await _authRepository.AddAsync(userEntity);
            await _tutorRepository.AddAsync(tutor);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RegisterParentAsync(ParentRegister registerDto)
        {
            var existedUser = await _authRepository.IsPhonenumberOrEmailTakenAsync(registerDto.PhoneNumber, registerDto.Email);
            if (existedUser)
            {
                throw new Exceptions.BadRequestException("Phone number or email is already taken.");
            }
            await EnsureEmailVerifiedForRegisterAsync(registerDto.Email);

            var userEntity = _autoMapper.Map<User>(registerDto);
            userEntity.UserID = Guid.NewGuid();
            userEntity.HashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
            var parent = _autoMapper.Map<Parent>(registerDto);
            parent.ParentID = userEntity.UserID;
            parent.User = userEntity;
            await _authRepository.AddAsync(userEntity);
            await _parentRepository.AddAsync(parent);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<TokenResponse> RefreshTokenAsync(string request)
        {
            var existToken = await _refreshTokenRepository.GetFirstOrDefaultAsync(x => x.Token == request);

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
            // Source the signing key the same way Program.cs sources the validation key:
            // appsettings (Jwt:SecretKey) first, then the JWT_SecretKey env var (.env / secret store).
            var secretKey = _configuration["Jwt:SecretKey"]
                ?? Environment.GetEnvironmentVariable("JWT_SecretKey")
                ?? throw new InvalidOperationException("Jwt:SecretKey (or JWT_SecretKey env var) is not configured.");
            var secretKeyByte = Encoding.UTF8.GetBytes(secretKey);

            var jwtId = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                // Custom claim so clients can display the user's name without an
                // extra profile call. A bespoke name avoids the JwtSecurityTokenHandler
                // outbound claim-type remapping that applies to ClaimTypes.Name.
                new Claim("fullname", user.Fullname ?? string.Empty),
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
        private void SetRefreshTokenCookie(string refreshToken) =>
            _httpContext.HttpContext.Response.Cookies.Append(
                "refreshToken", refreshToken, BuildRefreshTokenCookieOptions(DateTime.UtcNow.AddDays(7)));

        public void ClearRefreshTokenCookie() =>
            _httpContext.HttpContext.Response.Cookies.Append(
                "refreshToken", "", BuildRefreshTokenCookieOptions(DateTime.UtcNow.AddDays(-1)));

        // Cookie must be Secure (HTTPS-only) to pair with SameSite=None for a genuinely
        // cross-site FE/API deployment (e.g. unieducation.net calling a separate API domain
        // in production — see the CORS origins in Program.cs). Locally the FE dev server
        // proxies /api through Vite (vite.config.ts), so the browser only ever talks to
        // http://localhost:8080 — a Secure cookie is silently DROPPED there (never stored),
        // which is exactly what broke the refresh flow: every silent refresh / 401 retry sent
        // no cookie, "Refresh token is missing", and the user got logged out constantly.
        //
        // Default: secure in Production, not secure otherwise. Override explicitly via
        // REFRESH_COOKIE_SECURE=true/false for deployments that don't fit that assumption
        // (e.g. the docker-compose "Production" profile run locally without TLS).
        private CookieOptions BuildRefreshTokenCookieOptions(DateTime expires)
        {
            var secure = ResolveRefreshCookieSecure();
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                // SameSite=None requires Secure=true (browsers reject None without Secure), so
                // the two flags always move together.
                SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = expires
            };
        }

        private bool ResolveRefreshCookieSecure()
        {
            if (bool.TryParse(_configuration["REFRESH_COOKIE_SECURE"], out var explicitValue))
                return explicitValue;

            return string.Equals(_configuration["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase);
        }
    }
}