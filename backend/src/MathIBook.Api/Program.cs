using System.Net;
using System.Text;
using MathIBook.Application.Interfaces;
using MathIBook.Application.Services;
using MathIBook.Domain.Interfaces;
using MathIBook.Infrastructure.Data;
using MathIBook.Infrastructure.Services;
using MathIBook.Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using MathIBook.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://*:5000");
}

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
// DbContext scoped theo request và dùng Npgsql để giao tiếp PostgreSQL.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// UnitOfWork
// Mỗi request nhận một UnitOfWork dùng chung DbContext với các repository.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
// Các service dưới đây chứa nghiệp vụ quiz, progress, badge, reward và profile.
builder.Services.AddScoped<IQuizScoringService, QuizScoringService>();
builder.Services.AddScoped<IProgressSyncService, ProgressSyncService>();
builder.Services.AddScoped<IBadgeCheckService, BadgeCheckService>();
builder.Services.AddScoped<ICoinCalculationService, CoinCalculationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IQuizRewardService, QuizRewardService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IContentValidationService, ContentValidationService>();
builder.Services.AddHttpClient<IQuestionGeneratorService, QuestionGeneratorService>();

// JWT Authentication
// Thiếu Jwt:Key phải dừng startup thay vì chạy với xác thực không an toàn.
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Kiểm tra đầy đủ nơi phát hành, đối tượng nhận, thời hạn và chữ ký token.
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        // Không cộng thời gian dung sai: token hết hạn bị từ chối ngay.
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Client gửi token mã hóa trong Bearer; middleware giải mã trước khi JwtBearer validate.
            var authorization = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var encryptedToken = authorization.Substring("Bearer ".Length).Trim();
                try
                {
                    var decryptedToken = EncryptionHelper.Decrypt(encryptedToken);
                    context.Token = decryptedToken;
                }
                catch
                {
                    // Let validation fail if decryption fails
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// CORS - allow all origins for Flutter dev (web, mobile, desktop)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterDev", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    // Startup tự áp migration rồi chạy seed idempotent, gồm 5 Student bảng xếp hạng.
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    await SeedData.SeedAsync(context);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MathIBook API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors("AllowFlutterDev");

var webRootPath = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(webRootPath))
{
    Directory.CreateDirectory(webRootPath);
}
app.Environment.WebRootPath = webRootPath;

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRootPath),
    RequestPath = ""
});
// Thứ tự middleware quan trọng: xác thực trước kiểm tra active user và phân quyền.
app.UseAuthentication();
app.UseMiddleware<ActiveUserMiddleware>();
app.UseAuthorization();

// Map attribute routes như /api/coins, /api/leaderboard và /api/sync.
app.MapControllers();

app.Run();
