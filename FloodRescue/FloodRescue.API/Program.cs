using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Implements;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.Mapper;
using Microsoft.EntityFrameworkCore;

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
            builder.Services.AddSwaggerGen();

            //Lấy Connection String
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            //Đăng ký Unit Of Work
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            //Đăng ký AutoMapper
            builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);


            //Đăng ký DbContext
            builder.Services.AddDbContext<FloodRescueDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly("FloodRescue.Repositories")
                ));

            // Cấu hình CORS (Để React gọi được API sau này)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp",
                    policy => 
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
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

            app.UseHttpsRedirection();

            // Use CORS
            app.UseCors("AllowReactApp");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
