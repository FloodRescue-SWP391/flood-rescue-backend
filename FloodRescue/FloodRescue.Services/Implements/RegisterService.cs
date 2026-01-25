using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RegisterRequest;
using FloodRescue.Services.DTO.Request.User;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using FloodRescue.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements
{
    public class RegisterService : IRegisterService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RegisterService(IUnitOfWork unitOfWork, IMapper mapper) {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            newUser.Password = hashedPassword;


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
    }
}
