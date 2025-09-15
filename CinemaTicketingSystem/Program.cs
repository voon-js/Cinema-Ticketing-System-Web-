using CinemaTicketingSystem.Models;
using CinemaTicketingSystem.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSession(); // Add this line
//builder.Services.AddControllersWithViews();

// Add services to the container.
builder.Services.AddControllersWithViews();

// +++ ADD THIS LINE TO REGISTER THE DBCONTEXT +++
//builder.Services.AddDbContext<CinemaDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("CinemaConnection")));
builder.Services.AddSqlServer<CinemaDbContext>($@"
    Server=(localdb)\mssqllocaldb;
    AttachDbFilename={builder.Environment.ContentRootPath}\Database\CinemaDB.mdf;
");

// Add services to the container.
//builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IEmailService, EmailService>();
//builder.Services.AddScoped<EmailService>();
// Add this to your Program.cs
builder.Services.AddScoped<IQRService, QRService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();