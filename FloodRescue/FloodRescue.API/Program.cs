using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Implements;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.Mapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;
using Serilog;
using Serilog.Sinks.Graylog;
using Serilog.Sinks.Graylog.Core.Transport;
using FloodRescue.Services.Interface.Auth;
using FloodRescue.Services.Interface.Warehouse;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.ReliefItem;
using FloodRescue.Services.Interface.Category;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Implements.Auth;
using FloodRescue.Services.Implements.Cache;
using FloodRescue.Services.Implements.Category;
using FloodRescue.Services.Implements.Kafka;
using FloodRescue.Services.Implements.ReliefItem;
using FloodRescue.Services.Implements.Warehouse;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.Implements.RealTimeNoti;
using System.Threading.Tasks;
using FloodRescue.Services.Hubs;
using FloodRescue.Services.Interface.BackgroundJob;
using Hangfire;
using Hangfire.Redis.StackExchange;
using FloodRescue.Services.Implements.BackgroundJob;
using Hangfire.Dashboard;

namespace FloodRescue.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

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

            //Lấy Connection String SQL
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            //Lấy Connection String Redis
            var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

            //Lấy cấu hình graylog
            IConfigurationSection gelfConfig = builder.Configuration.GetSection("GELF");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console()
                .WriteTo.Graylog(new GraylogSinkOptions
                {
                    HostnameOrAddress = gelfConfig["Host"]!,
                    Port = int.Parse(gelfConfig["Port"]!),
                    TransportType = TransportType.Udp,
                    Facility = gelfConfig["LogSource"],
                    MinimumLogEventLevel = Serilog.Events.LogEventLevel.Information
                })
                .CreateLogger(); //bắt đầy create log và gán vào biến toàn cục static logger

            //bảo host áp dụng cấu hình log mới 
            builder.Host.UseSerilog();

            // Cấu hình signalR
            builder.Services.AddSignalR();


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

            //Đăng ký các services
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IReliefItemService, ReliefItemService>();
            builder.Services.AddScoped<ICacheService, CacheService>();
            builder.Services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();
            builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

            // Đăng ký RescueRequestService
            builder.Services.AddScoped<IRescueRequestService, RescueRequestService>();

            //Đăng ký DbContext
            builder.Services.AddDbContext<FloodRescueDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly("FloodRescue.Repositories")
                ));

            //Đăng ký Kafka
            builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
            builder.Services.AddHostedService<KafkaConsumerService>();

            //Chỗ này sau này để đăng ký các IKafkaHandler implementation - hiện giờ chưa tạo - addScoped


            // Đăng ký Redis Cache để inject được vào Cache Service
            // - đăng ký đồng thời handshake với redis server
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString!));

            //Đăng ký Redis vào IDistributedCache vào để cache thay vì lưu vào ram máy mình thì sẽ lưu thằng vào Redis
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                //định nghĩa tên chung cho các cache bên trong dự án nằm ở redis
                options.InstanceName = "FloodRescue:"; 
            });


            //Cấu hình Hangfire với Redis - lưu trữ/để job vào redis thay vì sql server 
            builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer() //gọn lại tên rồi lưu trữ trên redis
            .UseRecommendedSerializerSettings()
            .UseRedisStorage(redisConnectionString, new RedisStorageOptions
            {
                Prefix = "hangfire", //đặt tên key trong redis
                Db = 1, //lưu trong db 1 ở redis. Trong redis có 0 - 16 db
                InvisibilityTimeout = TimeSpan.FromMinutes(30),  //30p là hủy job để worker khác vào làm
                ExpiryCheckInterval = TimeSpan.FromHours(1) // cứ mỗi 1 tiếng là dọn dẹp các job hết hạn, đã chạy xong

            })
            );

            //Đăng kí Hangfire server để xử lí job

            builder.Services.AddHangfireServer(options =>
            {
                options.ServerName = $"FloodRescue-{Environment.MachineName}";

                options.WorkerCount = Environment.ProcessorCount * 2; //số worker xử lí job đồng thời   

                options.Queues = new[] { "critical", "default", "low" }; //tên queue để phân loại job  

            });

           


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
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyOrigin()
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

            //cấu hình format log lại cho http request
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // /hangfire lấy dữ liệu được lưu trữ trên redis rồi vẽ lên giao diện
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                //cho phép truy cập dashboard hangfire mà không cần auth
                IsReadOnlyFunc = (DashboardContext context) => false,
                DashboardTitle = "FloodRescue Hangfire Dashboard"
            });



            // Job 1: Dọn dẹp Refresh Tokens - Chạy lúc 2:00 AM mỗi ngày
            RecurringJob.AddOrUpdate<IBackgroundJobService>(
                recurringJobId: "cleanup-expired-refresh-tokens",        // ID duy nhất của job
                methodCall: job => job.CleanUpExpiredRefreshTokenAsyncs(), // Method cần gọi
                cronExpression: Cron.Daily(2, 0),                         // 2:00 AM UTC
                options: new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc //định nghĩa múi giờ
                }
            );

            // Job 2: Báo cáo tổng hợp hàng ngày - Chạy lúc 8:00 AM mỗi ngày
            RecurringJob.AddOrUpdate<IBackgroundJobService>(
                recurringJobId: "daily-summary-report",
                methodCall: job => job.SendDailySummaryReportAsync(),
                cronExpression: Cron.Daily(8, 0),                         // 8:00 AM UTC
                options: new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc //định nghĩa múi giờ
                }
            );

            app.MapHub<NotificationHub>("/hubs/notification");

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
