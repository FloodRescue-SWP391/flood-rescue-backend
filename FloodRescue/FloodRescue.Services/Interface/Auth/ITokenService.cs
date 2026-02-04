using FloodRescue.Repositories.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.Auth
{
    public interface ITokenService
    {
        //Xài tuple để trả về cả token và refresh token
        /// <summary>
        /// Tạo ra cặp Access Token và Refresh Token
        /// </summary>
        /// <param name="user">User cần tạo token</param>
        /// <returns></returns>
        Task<(string accessToken, string refreshToken)> GenerateTokenAsync(User user);


        /// <summary>
        /// Nhận vào 1 refresh token và trả về cặp Access Token và Refresh Token mới
        /// </summary>
        /// <param name="refreshToken">Nhận vào refresh token cũ</param>
        /// <returns></returns>
        Task<(string accessToken, string refreshToken)?> RefreshTokenAsync(string accessToken, string refreshToken);

        /// <summary>
        /// Refresh token lại từ access token 
        /// </summary>
        /// <param name="accessToken">Truyền vào 1 access Token</param>
        /// <returns></returns>
        Task<(string accessToken, string refreshToken, User user)?> RefreshTokenFromAccessTokenAsync(string accessToken);

        /// <summary>
        /// Thu hồi tất cả Refresh Token của user (khi logout hay là đổi password,...)
        /// </summary>
        /// <param name="userID">ID của user</param>
        /// <returns></returns>
        Task RevokeAllTokensAsync(Guid userID);
    }
}
