using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Implements;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.Implements;
using FloodRescue.Services.Interface;
using FloodRescue.Infrastructure.Services;
using FloodRescue.Services.Mapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace FloodRescue.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddHttpContextAccessor();

            // ===== CẤU HÌNH SWAGGER ĐỂ HỖ TRỢ JWT =====
            // Cho phép test API có [Authorize] trong Swagger
            builder.Services.AddSwaggerGen(options =>
            {
                // Định nghĩa Security Scheme cho Bearer Token
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",           // Tên header
                    Type = SecuritySchemeType.Http,   // Loại: HTTP
                    Scheme = "Bearer",                // Scheme: Bearer
                    BearerFormat = "JWT",             // Format: JWT
                    In = ParameterLocation.Header,    // Vị trí: Header
                    Description = "Nhập JWT token vào đây. Ví dụ: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                });

                // Áp dụng Security Requirement cho tất cả API
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            //Lấy Connection String
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            //Đăng ký Unit Of Work
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            //Đăng ký AutoMapper
            builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

            //Đăng ký WarehouseService
            builder.Services.AddScoped<IWarehouseService, WarehouseService>();

            // Đăng ký AuthService
            builder.Services.AddScoped<IAuthService, AuthService>();

            //Đăng ký TokenService
            builder.Services.AddScoped<ITokenService, TokenService>();

            // Register new services
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IReliefItemService, ReliefItemService>();

            // Đăng ký FileStorageService
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();

            //Đăng ký DbContext
            builder.Services.AddDbContext<FloodRescueDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly("FloodRescue.Repositories")
                ));



            // ===== CẤU HÌNH JWT AUTHENTICATION =====
            // Đọc cấu hình từ appsettings.json
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;

            builder.Services.AddAuthentication(options =>
            {
                // Mặc định sử dụng JWT Bearer cho tất cả authentication
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Cấu hình cách validate JWT Token
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validate Issuer (ai phát hành token)
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],

                    // Validate Audience (ai được dùng token)
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],

                    // Validate Signing Key (chữ ký của token)
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

                    // Validate Lifetime (thời gian hết hạn)
                    ValidateLifetime = true,

                    // Không cho phép sai lệch thời gian (clock skew = 0)
                    ClockSkew = TimeSpan.Zero
                };
            });


            // Cấu hình CORS (Để React gọi được API sau này)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAlls",
                    policy => 
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
                //.WithOrigins("http://localhost:3000")
            });

            var app = builder.Build();

            //Đăng kí Migration để tự động kích hoạt các migration khi chạy ứng dụng
            //Auto chạy lại update-database migration cập nhật các migration mới nhất
            // Mở sql ra thêm vào rồi đóng cổng lại luôn
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<FloodRescueDbContext>();
                    var config = services.GetRequiredService<IConfiguration>();

                    // A. Lệnh thần thánh: Tự động chạy Migration (tương đương Update-Database)
                    context.Database.Migrate();
                    Console.WriteLine("--> Database Migration Applied Successfully!");

                    Console.WriteLine("--> Seed Data Executed Successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--> Error during migration/seeding: {ex.Message}");
                    // Không throw lỗi để App vẫn chạy tiếp (để còn debug)
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseStaticFiles();
            app.UseHttpsRedirection();

            // Use CORS
            app.UseCors("AllowAlls");

            app.UseAuthentication();
            app.UseAuthorization();



            app.MapControllers();

            app.Run();
        }
    }
}
