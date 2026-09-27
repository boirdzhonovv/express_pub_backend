using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using express_backend.Models;

namespace express_backend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. ПОДКЛЮЧЕНИЕ КОНТРОЛЛЕРОВ (Критично для AuthController и OrdersController!)
        builder.Services.AddControllers();

        builder.Services.AddAuthorization();

        // DbContext (PostgreSQL)
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // 2. НАСТРОЙКА JWT АУТЕНТИФИКАЦИИ
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = builder.Configuration.GetSection("Jwt");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
                };
            });

        // CORS: разрешаем телефону слать запросы на компьютер
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        app.UseCors("AllowAll");

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // HTTPS редирект отключен, чтобы не было падений на 0.0.0.0
        // app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        // 3. ПОДКЛЮЧЕНИЕ МАРШРУТИЗАЦИИ КОНТРОЛЛЕРОВ
        app.MapControllers();


        // 4.
      using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // ЭТА СТРОКА АВТОМАТИЧЕСКИ СОЗДАСТ ВСЕ ТАБЛИЦЫ В POSTGRESQL, ЕСЛИ ИХ НЕТ!
        context.Database.EnsureCreated(); 
       
        // Проверяем, есть ли пользователи в базе данных
        if (!context.Users.Any())
        {
            context.Users.Add(new express_backend.Models.User 
            { 
                PhoneNumber = "+992900000001", 
                FullName = "Тест Клиент",
                CurrentRole = express_backend.Models.UserRole.Client,
                PreferredLang = express_backend.Models.AppLanguage.Ru,
                IsActive = true
            });

            context.Users.Add(new express_backend.Models.User 
            { 
                PhoneNumber = "+992900000002", 
                FullName = "Тест Курьер",
                CurrentRole = express_backend.Models.UserRole.Courier,
                PreferredLang = express_backend.Models.AppLanguage.Ru,
                IsActive = true
            });

            context.SaveChanges();
            Console.WriteLine("=== [БАЗА ДАННЫХ] Таблицы созданы и пользователи сидированы! ===");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при инициализации БД: {ex.Message}");
    }
}


        app.Run();
    }
}
