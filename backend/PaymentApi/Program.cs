using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using PaymentApi.Options;
using PaymentApi.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.Configure<IyzicoOptions>(
    builder.Configuration.GetSection("Iyzico")
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IInstallmentService, InstallmentService>();


builder.Services.AddScoped<IBinService, BinService>();





builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseCors("Frontend");

app.MapControllers();

app.Run();
