using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace FloodRescue.Services.Implements
{
    public class TokenService : ITokenService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpirationMinutes; //15 phút
        private readonly int _refreshTokenExpirationDays; //7 ngày

        public TokenService(IConfiguration configuration, IUnitOfWork unitOfWork)
        {
            _configuration = configuration; 
            _unitOfWork =  unitOfWork;

            _secretKey = _configuration.GetSection("JwtSettings")["SecretKey"]!;
            _issuer = _configuration.GetSection("JwtSettings")["Issuer"]!;
            _audience = _configuration.GetSection("JwtSettings")["Audience"]!;
            _accessTokenExpirationMinutes = int.Parse(_configuration.GetSection("JwtSettings")["AccessTokenExpirationMinutes"]!);
            _refreshTokenExpirationDays = int.Parse(_configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"]!);

        }

        /// <summary>
        /// Tạo access token và refresh token cho user  
        /// </summary>
        /// <param name="user">Truyền vào user để tạo token</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<(string accessToken, string refreshToken)> GenerateTokenAsync(User user)
        {
            // Mỗi lần login hay refresh token thì tạo một jwtID mới    
            string jwtID = Guid.NewGuid().ToString();

            string accessToken = GenerateAccessToken(user, jwtID); 
            string refreshTokenGenerated = GenerateRefreshToken();

            RefreshToken refreshToken = new RefreshToken
            {
                Token = refreshTokenGenerated,
                JwtID = jwtID, //liên kết với accessToken
                IsUsed = false, //refresh token chưa sử dụng
                IsRevoked = false, //refresh token chưa bị thu hồi  
                CreatedAt = DateTime.UtcNow, //thời điểm tạo
                ExpiredAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays), //hết hạn sau 7 ngày đã cấu hình bên trong appsettings.json
                UserID = user.UserID //thuộc về user nào
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();

            return (accessToken, refreshTokenGenerated);

        }

        /// <summary>
        /// Gỉải mã access token đã hết hạn để xác thực hồi mới cấp lại refresh token lại được cho đúng người
        /// </summary>
        /// <param name="token">Đưa vào token cũ để qua các bước kiểm ta</param>
        /// <returns></returns>
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            //tạo ra 1 bộ luật kiểm tra
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false, //không kiểm tra thời gian hết hạn
                ValidIssuer = _issuer,
                ValidAudience = _audience,  
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey))
            };

            try
            {
                //instance dùng để kiểm tra token
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                //validate cái token đó coi hợp lệ hay không
                ClaimsPrincipal principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if(securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                //1 format bao gồm các thông tin trong claims cũ
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Dùng refresh token cũ để lấy cặp token mới
        /// </summary>
        /// <param name="accessToken">Access token (có thể đã hết hạn)</param>
        /// <param name="refreshToken">Refresh token cần kiểm tra</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<(string accessToken, string refreshToken)?> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            ClaimsPrincipal? principal = GetPrincipalFromExpiredToken(accessToken);

            if (principal == null)
            {
                return null;
            }

            //Lấy jwtID từ access token
            string? jwtID = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jwtID))
            {
                return null;
            }

            //Tìm refresh token bên trong database bằng jwtID đã đăng kí mapping với access token
            RefreshToken? storedToken = await _unitOfWork.RefreshTokens.GetAsync(rt => rt.Token == refreshToken && rt.JwtID == jwtID);

            //Kiểm tra refresh token còn hợp lệ hay không
            if (storedToken == null || storedToken.IsUsed || storedToken.IsRevoked || storedToken.ExpiredAt < DateTime.UtcNow)
            {
                return null;
            }


            storedToken.IsUsed = true; //đánh dấu refresh token đã sử dụng
            _unitOfWork.RefreshTokens.Update(storedToken);  
            await _unitOfWork.SaveChangesAsync();

            // Lấy UserID từ claims - Kiểm tra cả "sub" và ClaimTypes.NameIdentifier
            // Vì .NET có thể map "sub" thành ClaimTypes.NameIdentifier khi validate token
            var userIdClaim = principal.Claims.FirstOrDefault(c => 
                c.Type == JwtRegisteredClaimNames.Sub || 
                c.Type == ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userID))
            {
                return null;
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userID);
            if(user == null)
            {
                return null;
            }

            // Ensure a return value for all code paths
            return await GenerateTokenAsync(user);
        }


        public async Task RevokeAllTokensAsync(Guid userID)
        {
            var tokens = await _unitOfWork.RefreshTokens.GetAllAsync(rt => rt.UserID == userID && !rt.IsRevoked);

            foreach (var token in tokens)
            {
                token.IsRevoked = true; 
                _unitOfWork.RefreshTokens.Update(token);
            }

            await _unitOfWork.SaveChangesAsync();
        }

     

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user">Truyền vào user ứng với cái token đó</param>
        /// <param name="jwtID">Truyền vào mã developer tự tạo để hồi gán cho access token</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">User hoặc jwtID có thể không được đưa vào</exception>
        /// <exception cref="InvalidOperationException">Secret key hoặc là User không thể lấy được Role</exception>
        private string GenerateAccessToken(User user, string jwtID)
        {
            if(user == null) throw new ArgumentException("User không tồn tại");
            if (user.Role == null) throw new InvalidOperationException("User tồn tại nhưng không lấy được Role");
            if (_secretKey == null) throw new InvalidOperationException("Chưa cấu hình SecretKey trong appsettings.json");  
            if(jwtID == null) throw new ArgumentException("jwtID không được để trống");

            //--------- HEADER ----------//
            //đầu tiên lấy chuỗi bí mật bên trong secret.json ra
            //biến thành mảng byte arraay
            //SymmetricSecurityKey có nghĩa là dùng thuật toán ["alg": "HS256"]
            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            //-------- SIGNATURE -------//
            //3. Chỉ định dùng thuật toán HmacSha256 để ký HMACSHA256()
            SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            //----- PAYLOAD -------//
            Claim[] claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),

                //cái này sẽ đi kèm với refresh token tức là khi hacker gửi chính access token này
                //kèm theo cái refresh token giả thì hệ thống sẽ check lại coi cái refresh token trong db
                //có khớp với lại cái Jti này không
                new Claim(JwtRegisteredClaimNames.Jti, jwtID),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("username", user.Username),
                new Claim("fullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role.RoleName)

            };

            //----- CREATE JWT TOKEN -----// 
            JwtSecurityToken tokenFormat = new JwtSecurityToken(
               issuer: _issuer,
               audience: _audience,
               claims: claims,
               expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
               signingCredentials: credentials
            );

            string token = new JwtSecurityTokenHandler().WriteToken(tokenFormat);

            return token;
        }

        /// <summary>
        /// Tạo ra chuỗi refresh token thông qua byte ngẫu nhiên
        /// </summary>
        /// <returns></returns>
        private string GenerateRefreshToken()
        {
            byte[] randomBytes = new byte[64];

            RandomNumberGenerator.Fill(randomBytes);

            // Chuyển sang Base64 string để dễ truyền qua HTTP
            // Ví dụ: "abc123xyz789..." (86 ký tự)
            return Convert.ToBase64String(randomBytes);

        }

    }
}
