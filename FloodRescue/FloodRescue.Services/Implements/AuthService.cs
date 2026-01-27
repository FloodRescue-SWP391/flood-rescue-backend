using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.Auth;
using FloodRescue.Services.DTO.Request.AuthRequest;
using FloodRescue.Services.DTO.Request.User;
using FloodRescue.Services.DTO.Response.AuthResponse;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using FloodRescue.Services.Interface;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, ITokenService tokenService) {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _tokenService = tokenService;
        }

        public async Task<(AuthResponseDTO? Data, string? ErrorMessage)> RefeshTokenAsync(RefreshTokenRequest request)
        {
            var result = await _tokenService.RefreshTokenFromAccessTokenAsync(request.AccessToken);

            if(result == null)
            {
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

            return (response, null);

        }

        public async Task<(RegisterResponseDTO? Data,string? ErrorMessage)> RegisterAsync(RegisterRequestDTO request)
        {
            // 1. Check if username already exists
            var existingUserName = await _unitOfWork.Users.GetAsync(u => u.Username == request.Username && !u.IsDeleted);
            if (existingUserName != null)
            {
                return (null, "Username already exists");
            }

            //  2. Check if phone number already exists
            var existingPhone = await _unitOfWork.Users.GetAsync(u => u.Phone == request.Phone && !u.IsDeleted);
            if (existingPhone != null)
            {
                return (null, "Phone number already exists");
            }
            // 3. Check if role exists in Roles table
            var role = await _unitOfWork.Roles.GetAsync(r => r.RoleID == request.RoleID);
            if (role == null)
            {
                return (null, "Invalid RoleID");
            }

            // 4. Can't register as admin
            // OrdinalIgnoreCase: So sánh trực tiếp, bỏ qua hoa/thường không có tạo string tạm thời
            if (string.Equals(request.RoleID, "AD", StringComparison.OrdinalIgnoreCase))
            {
                return (null, "Cannot register as admin");
            }



            // 5. Create new user and hash the password
            User newUser = _mapper.Map<User>(request);
            //var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            CreatePasswordHash(request.Password, out byte[] hashedPassword, out byte[] salt);
            newUser.Password = hashedPassword;
            newUser.Salt = salt;


            //// 6. Map DTO to Entity
            //User newUser = _mapper.Map<User>(user);

            // 6. Add new user to RAM
            await _unitOfWork.Users.AddAsync(newUser);
            // 7. Save to database
            int result = await _unitOfWork.SaveChangesAsync();

            // 8. Check if save was successful
            if (result <= 0) 
            {
                return (null, "Failed to create user");
            }
            // 11. Prepare response DTO
            //RegisterResponseDTO responseDTO = new RegisterResponseDTO
            //{
            //    UserID = newUser.UserID,
            //    Username = newUser.Username,
            //    Phone = newUser.Phone,
            //    FullName = newUser.FullName,
            //    RoleID = newUser.RoleID,
            //};

            var responseDTO = _mapper.Map<RegisterResponseDTO>(newUser);
            return (responseDTO, null);
        }

        private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            // Dùng out thay thế cho return nhiều giá trị
            using var hmac = new HMACSHA512();
            salt = hmac.Key;
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            // Tạo HMAC với CÙNG salt đã lưu
            // Sử dụng using để tự động Dipose()
            using var hmac = new HMACSHA512(storedSalt);
      
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Hash password user nhập - OneWay can't be reversed
            var computedHash = hmac.ComputeHash(passwordBytes);

            // So sánh từng byte
            return computedHash.SequenceEqual(storedHash);
        }


    }
}
