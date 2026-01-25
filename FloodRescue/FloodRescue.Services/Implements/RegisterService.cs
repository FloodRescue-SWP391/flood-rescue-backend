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

        public async Task<ApiResponse<RegisterResponseDTO>> RegisterAsync(RegisterRequestDTO request)
        {
            // 1. Check if username already exists
            var existingUserName = await _unitOfWork.Users.GetAsync(u => u.Username == request.Username && !u.IsDeleted);
            if (existingUserName != null)
            {
                return ApiResponse<RegisterResponseDTO>.Fail("Username already exists", 400);
            }

            //  2. Check if phone number already exists
            var existingPhone = await _unitOfWork.Users.GetAsync(u => u.Phone == request.Phone && !u.IsDeleted);
            if (existingPhone != null)
            {
                return ApiResponse<RegisterResponseDTO>.Fail("Phone number already exists", 400);
            }
            // 3. Check if role exists
            var role = await _unitOfWork.Roles.GetAsync(r => r.RoleID == request.RoleID);
            if (role == null)
            {
                return ApiResponse<RegisterResponseDTO>.Fail("Invalid RoleID", 400);
            }

            // 4. Can't register as admin
            if (request.RoleID.ToUpper() == "AD")
            {
                return ApiResponse<RegisterResponseDTO>.Fail("Cannot register as admin", 400);
            }

            // 5. Hash the password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 6. Create new user
            User newUser = _mapper.Map<User>(request);
            newUser.Password = hashedPassword;
            //var user = new CreateUserRequestDTO
            //{
            //    Username = request.Username,
            //    Password = hashedPassword,
            //    Phone = request.Phone,
            //    FullName = request.FullName,
            //    RoleID = request.RoleID,
            //};

            //// 7. Map DTO to Entity
            //User newUser = _mapper.Map<User>(user);

            // 8. Add new user to RAM
            await _unitOfWork.Users.AddAsync(newUser);
            // 9. Save to database
            int result = await _unitOfWork.SaveChangesAsync();

            // 10. Check if save was successful
            if (result <= 0) 
            {
                return ApiResponse<RegisterResponseDTO>.Fail("Failed to create user", 500);
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

            return ApiResponse<RegisterResponseDTO>.Ok(responseDTO, "Register successfully", 201);
        }
    }
}
