using TravAi;
using TravAi.Data;
using TravAi.Middlewares;
using Microsoft.EntityFrameworkCore;
using TravAi.Repositories.GenericRepository;

using TravAi.Repositories.UserRepository;
using TravAi.Services;
using TravAi.Services.Auth;
using TravAi.Services.AI;
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

builder.Services.AddScoped<TravAi.Services.HotelService.IHotelService, TravAi.Services.HotelService.HotelService>();
builder.Services.AddScoped<TravAi.Services.HotelService.IHotelDashboardService, TravAi.Services.HotelService.HotelDashboardService>();
builder.Services.AddScoped<TravAi.Services.FileStorage.IFileService, TravAi.Services.FileStorage.FileService>();

// --- Airline Services ---
builder.Services.AddScoped<TravAi.Airline.Services.AirportService.IAirportService, TravAi.Airline.Services.AirportService.AirportService>();
builder.Services.AddScoped<TravAi.Airline.Services.BookingService.IBookingService, TravAi.Airline.Services.BookingService.BookingService>();

builder.Services.AddScoped<TravAi.Airline.Services.CompanionService.ICompanionService, TravAi.Airline.Services.CompanionService.CompanionService>();
builder.Services.AddScoped<TravAi.Airline.Services.DashboardService.IDashboardService, TravAi.Airline.Services.DashboardService.DashboardService>();
builder.Services.AddScoped<TravAi.Airline.Services.FlightService.IFlightService, TravAi.Airline.Services.FlightService.FlightService>();
builder.Services.AddScoped<TravAi.Airline.Services.PassengerService.IPassengerService, TravAi.Airline.Services.PassengerService.PassengerService>();
builder.Services.AddScoped<TravAi.Airline.Services.ReviewService.IReviewService, TravAi.Airline.Services.ReviewService.ReviewService>();

// --- TourGuide Services ---
builder.Services.AddScoped<TravAi.TourGuide.Services.ITourGuideService, TravAi.TourGuide.Services.TourGuideService>();
builder.Services.AddScoped<TravAi.TourGuide.Services.ITourService, TravAi.TourGuide.Services.TourService>();
builder.Services.AddScoped<TravAi.TourGuide.Services.IBookingService, TravAi.TourGuide.Services.BookingService>();
builder.Services.AddScoped<TravAi.TourGuide.Services.IUrgentRequestService, TravAi.TourGuide.Services.UrgentRequestService>();
builder.Services.AddScoped<TravAi.TourGuide.Services.IWithdrawRequestService, TravAi.TourGuide.Services.WithdrawRequestService>();

// --- AI Trip Planner Service ---
builder.Services.AddScoped<IAiTripPlannerService, AiTripPlannerService>();

// --- AI Chatbot Service (Python microservice proxy) ---
builder.Services.AddHttpClient<IAiChatbotService, AiChatbotService>(client =>
{
    var baseUrl = builder.Configuration["PythonAiService:BaseUrl"] ?? "http://localhost:8000";
    var timeout = int.Parse(builder.Configuration["PythonAiService:TimeoutSeconds"] ?? "60");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeout);
});

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
    c.SwaggerDoc("AI",   new OpenApiInfo { Title = "AI Trip Planner API", Version = "v1" });
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
        TravAi.Data.DbSeeder.Seed(context);
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
    c.SwaggerEndpoint("/swagger/AI/swagger.json",   "AI Trip Planner API");
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
 
