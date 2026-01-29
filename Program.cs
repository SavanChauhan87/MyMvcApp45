using Microsoft.EntityFrameworkCore;
using MyMvcApp.Data;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Ensure database folder exists
var dbFolder = Path.Combine(app.Environment.ContentRootPath, "AppData");
if (!Directory.Exists(dbFolder))
{
    Directory.CreateDirectory(dbFolder);
}

// Seed initial data (admin user and sample products) if database is empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!db.Users.Any())
    {
        db.Users.Add(new MyMvcApp.Models.User
        {
            Username = "admin",
            Password = "admin123",
            FullName = "Admin User",
            Email = "admin@pharmacy.com",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.Now,
            LastLogin = DateTime.Now
        });
        db.SaveChanges();
    }

    if (!db.Products.Any())
    {
        db.Products.AddRange(new[]
        {
            new MyMvcApp.Models.Product { Name = "Paracetamol 500mg", Description = "Pain relief tablets", Price = 5.99m, StockQuantity = 100, Category = "Pain Relief", Manufacturer = "ABC Pharma", DosageForm = "Tablet", Strength = "500mg", ImageUrl = "/images/products/paracetamol.jpg", IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new MyMvcApp.Models.Product { Name = "Amoxicillin 250mg", Description = "Antibiotic capsules", Price = 12.99m, StockQuantity = 50, Category = "Antibiotics", Manufacturer = "XYZ Pharma", DosageForm = "Capsule", Strength = "250mg", ImageUrl = "/images/products/amoxicillin.jpg", IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new MyMvcApp.Models.Product { Name = "Vitamin C 1000mg", Description = "Immune support tablets", Price = 8.99m, StockQuantity = 75, Category = "Vitamins", Manufacturer = "Health Plus", DosageForm = "Tablet", Strength = "1000mg", ImageUrl = "/images/products/vitamin-c.jpg", IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
        });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Keep HTTP during development to avoid auto-redirects to HTTPS
// app.UseHttpsRedirection();
app.UseStaticFiles(); // Add this line to serve static files
app.UseRouting();
app.UseSession(); // Add session middleware

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
