using UserAuthorizationandAuthentication;
using UserAuthorizationandAuthentication.Data;
using UserAuthorizationandAuthentication.Middlewares;
using Microsoft.EntityFrameworkCore;
using UserAuthorizationandAuthentication.Repositories.GenericRepository;

using UserAuthorizationandAuthentication.Repositories.UserRepository;
using UserAuthorizationandAuthentication.Services;
using UserAuthorizationandAuthentication.Services.Auth;
// Final sync after manual drop
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();

builder.Services.AddScoped<UserAuthorizationandAuthentication.Services.HotelService.IHotelService, UserAuthorizationandAuthentication.Services.HotelService.HotelService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.Services.FileStorage.IFileService, UserAuthorizationandAuthentication.Services.FileStorage.FileService>();

// --- Airline Services ---
builder.Services.AddScoped<UserAuthorizationandAuthentication.Airline.Services.AirportService.IAirportService, UserAuthorizationandAuthentication.Airline.Services.AirportService.AirportService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.Airline.Services.BookingService.IBookingService, UserAuthorizationandAuthentication.Airline.Services.BookingService.BookingService>();

builder.Services.AddScoped<UserAuthorizationandAuthentication.Airline.Services.CompanionService.ICompanionService, UserAuthorizationandAuthentication.Airline.Services.CompanionService.CompanionService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.Airline.Services.DashboardService.IDashboardService, UserAuthorizationandAuthentication.Airline.Services.DashboardService.DashboardService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.Airline.Services.FlightService.IFlightService, UserAuthorizationandAuthentication.Airline.Services.FlightService.FlightService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.Airline.Services.PassengerService.IPassengerService, UserAuthorizationandAuthentication.Airline.Services.PassengerService.PassengerService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.Airline.Services.ReviewService.IReviewService, UserAuthorizationandAuthentication.Airline.Services.ReviewService.ReviewService>();

// --- TourGuide Services ---
builder.Services.AddScoped<UserAuthorizationandAuthentication.TourGuide.Services.ITourGuideService, UserAuthorizationandAuthentication.TourGuide.Services.TourGuideService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.TourGuide.Services.ITourService, UserAuthorizationandAuthentication.TourGuide.Services.TourService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.TourGuide.Services.IBookingService, UserAuthorizationandAuthentication.TourGuide.Services.BookingService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.TourGuide.Services.IUrgentRequestService, UserAuthorizationandAuthentication.TourGuide.Services.UrgentRequestService>();
builder.Services.AddScoped<UserAuthorizationandAuthentication.TourGuide.Services.IWithdrawRequestService, UserAuthorizationandAuthentication.TourGuide.Services.WithdrawRequestService>();

// Add CORS
builder.Services.AddCors(options =>
{
    // We use SetIsOriginAllowed(origin => true) and AllowCredentials() 
    // to allow any origin (including any localhost and LAN IPs) with credentials,
    // which is the most reliable setup for local development with mobile apps and web frontends.
    options.AddPolicy("AllowAll",
        builder => builder
        .SetIsOriginAllowed(origin => true) // Allows any origin
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUser", policy => policy.RequireRole("User"));
    // Add more policies as needed
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please insert JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer", // <-- Required for Bearer Authentication
        BearerFormat = "JWT" // Optional, for documentation clarity
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    c.SwaggerDoc("Hotel", new OpenApiInfo { Title = "Hotel API", Version = "v1" });
    c.SwaggerDoc("Airline", new OpenApiInfo { Title = "Airline API", Version = "v1" });
    c.SwaggerDoc("TourGuide", new OpenApiInfo { Title = "TourGuide API", Version = "v1" });
    c.SwaggerDoc("Auth", new OpenApiInfo { Title = "Auth API", Version = "v1" });
});

var app = builder.Build();

// Custom Seed Command: dotnet run --seed
if (args.Contains("--seed"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        Console.WriteLine("Command: Starting Database Seeding...");
        UserAuthorizationandAuthentication.Data.DbSeeder.Seed(context);
        Console.WriteLine("Command: Seeding Completed. Exiting.");
    }
    return;
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/Hotel/swagger.json", "Hotel API");
    c.SwaggerEndpoint("/swagger/Airline/swagger.json", "Airline API");
    c.SwaggerEndpoint("/swagger/TourGuide/swagger.json", "TourGuide API");
    c.SwaggerEndpoint("/swagger/Auth/swagger.json", "Auth API");
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

// Enable serving static files (for uploaded images)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database seeding is now disabled on startup.

// Restart trigger
app.Run();
 
