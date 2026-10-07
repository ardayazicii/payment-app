using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using PaymentApi.Options;
using PaymentApi.Services;

var builder = WebApplication.CreateBuilder(args);


var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls(
        $"http://0.0.0.0:{port}"
    );
}

// Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// iyzico configuration
builder.Services.Configure<IyzicoOptions>(
    builder.Configuration.GetSection("Iyzico")
);

// Frontend URL
// Local: http://localhost:5173

var frontendUrl =
    builder.Configuration["FrontendUrl"]
    ?? "http://localhost:5173";

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                frontendUrl.TrimEnd('/')
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Services
builder.Services.AddScoped<IBinService, BinService>();

builder.Services.AddScoped<
    IInstallmentService,
    InstallmentService
>();

builder.Services.AddScoped<
    IPaymentService,
    PaymentService
>();

// Database
builder.Services.AddDbContext<AppDbContext>(
    options =>
        options.UseSqlite(
            builder.Configuration
                .GetConnectionString(
                    "DefaultConnection"
                )
        )
);

var app = builder.Build();

// OpenAPI sadece development ortamında
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}



app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    db.Database.Migrate();
}

app.Run();