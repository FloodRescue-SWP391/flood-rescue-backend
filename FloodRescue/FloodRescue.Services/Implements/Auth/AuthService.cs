using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.Auth;
using FloodRescue.Services.DTO.Request.AuthRequest;
using FloodRescue.Services.DTO.Request.User;
using FloodRescue.Services.DTO.Response.AuthResponse;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using FloodRescue.Services.Interface.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, ITokenService tokenService, ILogger<AuthService> logger) {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<(AuthResponseDTO? Data, string? ErrorMessage)> RefeshTokenAsync(RefreshTokenRequest request)
        {
            _logger.LogInformation("[AuthService] Refreshing token for user.");
            var result = await _tokenService.RefreshTokenFromAccessTokenAsync(request.AccessToken);

            if(result == null)
            {
                _logger.LogWarning("[AuthService] Refresh token failed. Token invalid or expired.");
                return (null, "Invalid or expired token. Please login again");            
            
            }

            //phân rã cấu trúc ra để xài
            var (newAcessToken, newRefreshToken, user) = result.Value;

            var expireTimeInMinutes = int.Parse(_configuration.GetSection("JwtSettings")["AccessTokenExpirationMinutes"]!);

            AuthResponseDTO response = new AuthResponseDTO
            {
                AccessToken = newAcessToken,
                TokenType = "Bearer",   
                ExpiresIn = expireTimeInMinutes * 60, // seconds    
                UserID = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role?.RoleName ?? ""
            };

            _logger.LogInformation("[AuthService] Token refreshed successfully for UserID: {UserID}", user.UserID);
            return (response, null);

        }

        public async Task<(RegisterResponseDTO? Data,string? ErrorMessage)> RegisterAsync(RegisterRequestDTO request)
        {
            _logger.LogInformation("[AuthService] Registering new user. Username: {Username}", request.Username);
            // 1. Check if username already exists
            var existingUserName = await _unitOfWork.Users.GetAsync(u => u.Username == request.Username && !u.IsDeleted);
            if (existingUserName != null)
            {
                _logger.LogWarning("[AuthService - Sql Server] Register failed. Username '{Username}' already exists.", request.Username);
                return (null, "Username already exists");
            }

            //  2. Check if phone number already exists
            var existingPhone = await _unitOfWork.Users.GetAsync(u => u.Phone == request.Phone && !u.IsDeleted);
            if (existingPhone != null)
            {
                _logger.LogWarning("[AuthService - Sql Server] Register failed. Phone '{Phone}' already exists.", request.Phone);
                return (null, "Phone number already exists");
            }
            // 3. Check if role exists in Roles table
            Role? role = await _unitOfWork.Roles.GetAsync(r => r.RoleID == request.RoleID);

            if (role == null)
            {
                _logger.LogWarning("[AuthService - Sql Server] Register failed. Invalid RoleID: {RoleID}", request.RoleID);
                return (null, "Invalid RoleID");
            }

            // 4. Can't register as admin
            // OrdinalIgnoreCase: So sánh trực tiếp, bỏ qua hoa/thường không có tạo string tạm thời
            // if (string.Equals(request.RoleID, "AD", StringComparison.OrdinalIgnoreCase))
            // {
            //     return (null, "Cannot register as admin");
            // }

            // 5. Create new user and hash the password
            User newUser = _mapper.Map<User>(request);

            //map Role - navigate property manually
            newUser.Role = role;

            //var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            CreatePasswordHash(request.Password, out byte[] hashedPassword, out byte[] salt);
            newUser.Password = hashedPassword;
            newUser.Salt = salt;


            //User newUser = _mapper.Map<User>(user);

            // 6. Add new user to RAM
            await _unitOfWork.Users.AddAsync(newUser);

            // 7. Save to database
            int result = await _unitOfWork.SaveChangesAsync();

            // 8. Check if save was successful
            if (result <= 0) 
            {
                _logger.LogError("[AuthService - Error] Failed to save new user to database. Username: {Username}", request.Username);
                return (null, "Failed to create user");
            }
            
            var responseDTO = _mapper.Map<RegisterResponseDTO>(newUser);
            _logger.LogInformation("[AuthService - Sql Server] User registered successfully. UserID: {UserID}, Username: {Username}", newUser.UserID, newUser.Username);
            return (responseDTO, null);
        }

        public async Task<(AuthResponseDTO? Data, string? ErrorMessage)> LoginAsync(LoginRequestDTO request)
        {
            _logger.LogInformation("[AuthService] Login attempt for Username: {Username}", request.Username);
            // 1. Tìm user theo username (không lấy user đã xóa)
            var user = await _unitOfWork.Users.GetAsync(u => u.Username == request.Username && !u.IsDeleted, u => u.Role!);
            if (user == null)
            {
                _logger.LogWarning("[AuthService - Sql Server] Login failed. Username '{Username}' not found.", request.Username);
                return (null, "Invalid username or password");
            }

            // 2. Kiểm tra mật khẩu 
            var isPasswordValid = VerifyPasswordHash(request.Password, user.Password, user.Salt);
            if (!isPasswordValid)
            {
                _logger.LogWarning("[AuthService] Login failed. Invalid password for Username: {Username}", request.Username);
                return (null, "Invalid username or password");
            }

            // 3. Tạo cặp token (access + refresh) và lưu refresh token trong TokenService
            var (accessToken, refreshToken) = await _tokenService.GenerateTokenAsync(user);

            // 4. Chuẩn bị AuthResponseDTO giống cách Register trả về
            var expireTimeInMinutes = int.Parse(_configuration.GetSection("JwtSettings")["AccessTokenExpirationMinutes"]!);

            var response = new AuthResponseDTO
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = expireTimeInMinutes * 60,
                UserID = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role?.RoleName ?? string.Empty
            };

            _logger.LogInformation("[AuthService] Login successful. UserID: {UserID}, Username: {Username}, Role: {Role}", user.UserID, user.Username, user.Role?.RoleName);
            return (response, null);
        }
        private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            // Dùng out thay thế cho return nhiều giá trị
            using var hmac = new HMACSHA512();
            //lấy salt ra để đem đi cất vào database thông qua biến out
            salt = hmac.Key;
            //salt là dùng để trộn với thuật toán hmac để sinh ra pass hashing
            //gọi thẳng mà không cần qua tinh gọn
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password">Truyền vào request password</param>
        /// <param name="storedHash">Lấy ra cái store hash password của user trong database</param>
        /// <param name="storedSalt">Lấy ra cái store salt của user trong database</param>
        /// <returns></returns>
        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            // Tạo HMAC với CÙNG salt đã lưu
            // Sử dụng using để tự động Dipose()

            //truyền lại cái salt cũ để trộn ra lại để sinh ra được cái password map salt cũ
            //password có thể giống nhau nhưng nó phải đi kèm với salt
            using var hmac = new HMACSHA512(storedSalt);
      
            //tinh gọn vào 1 biến passwordBytes
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Hash password user nhập - OneWay can't be reversed
            //truyền cái biến tinh gọn vào
            var computedHash = hmac.ComputeHash(passwordBytes);

            // So sánh từng byte
            return computedHash.SequenceEqual(storedHash);
        }


    }
}
