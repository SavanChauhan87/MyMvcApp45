using Microsoft.AspNetCore.Mvc;
using MyMvcApp.Models;
using MyMvcApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace MyMvcApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ApplicationDbContext _db;

        public AdminController(IWebHostEnvironment webHostEnvironment, ApplicationDbContext db)
        {
            _webHostEnvironment = webHostEnvironment;
            _db = db;
        }

        // Temporary data storage (in real app, use database)
        private static List<Product> _products = new List<Product>
        {
            new Product { Id = 1, Name = "Paracetamol 500mg", Description = "Pain relief tablets", Price = 5.99m, StockQuantity = 100, Category = "Pain Relief", Manufacturer = "ABC Pharma", DosageForm = "Tablet", Strength = "500mg", ImageUrl = "/images/products/paracetamol.jpg", IsActive = true },
            new Product { Id = 2, Name = "Amoxicillin 250mg", Description = "Antibiotic capsules", Price = 12.99m, StockQuantity = 50, Category = "Antibiotics", Manufacturer = "XYZ Pharma", DosageForm = "Capsule", Strength = "250mg", ImageUrl = "/images/products/amoxicillin.jpg", IsActive = true },
            new Product { Id = 3, Name = "Vitamin C 1000mg", Description = "Immune support tablets", Price = 8.99m, StockQuantity = 75, Category = "Vitamins", Manufacturer = "Health Plus", DosageForm = "Tablet", Strength = "1000mg", ImageUrl = "/images/products/vitamin-c.jpg", IsActive = true }
        };

        private static List<Order> _orders = new List<Order>
        {
            new Order { Id = 1, CustomerName = "John Doe", CustomerEmail = "john@example.com", CustomerPhone = "123-456-7890", ShippingAddress = "123 Main St", TotalAmount = 25.97m, Status = OrderStatus.Pending, OrderDate = DateTime.Now.AddDays(-1) },
            new Order { Id = 2, CustomerName = "Jane Smith", CustomerEmail = "jane@example.com", CustomerPhone = "098-765-4321", ShippingAddress = "456 Oak Ave", TotalAmount = 18.98m, Status = OrderStatus.Confirmed, OrderDate = DateTime.Now.AddDays(-2) }
        };

        public IActionResult Index()
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.TotalProducts = _db.Products.Count();
            ViewBag.TotalOrders = _db.Orders.Count();
            ViewBag.PendingOrders = _db.Orders.Count(o => o.Status == OrderStatus.Pending);
            ViewBag.LowStockProducts = _db.Products.Count(p => p.StockQuantity < 20);
            ViewBag.RecentOrders = _db.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToList();
            
            return View();
        }

        public IActionResult Products()
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var products = _db.Products.ToList();
            return View(products);
        }

        public IActionResult Orders()
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var orders = _db.Orders.Include(o => o.OrderItems).ToList();
            return View(orders);
        }

        public IActionResult AddProduct()
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile? imageFile)
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                product.CreatedAt = DateTime.Now;
                product.UpdatedAt = DateTime.Now;
                product.IsActive = true;

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    product.ImageUrl = "/images/products/" + uniqueFileName;
                }
                else if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    // Keep the provided URL if no file was uploaded
                    product.ImageUrl = product.ImageUrl;
                }
                else
                {
                    // Default image if no image provided
                    product.ImageUrl = "/images/products/default-medicine.jpg";
                }

                _db.Products.Add(product);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Product added successfully.";
                return RedirectToAction("Products");
            }
            return View(product);
        }

        public IActionResult EditProduct(int id)
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Product product, IFormFile? imageFile)
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                var existingProduct = _db.Products.FirstOrDefault(p => p.Id == product.Id);
                if (existingProduct != null)
                {
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.Category = product.Category;
                    existingProduct.Manufacturer = product.Manufacturer;
                    existingProduct.DosageForm = product.DosageForm;
                    existingProduct.Strength = product.Strength;
                    existingProduct.IsActive = product.IsActive;
                    existingProduct.UpdatedAt = DateTime.Now;

                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        existingProduct.ImageUrl = "/images/products/" + uniqueFileName;
                    }
                    else if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        existingProduct.ImageUrl = product.ImageUrl;
                    }
                }
                await _db.SaveChangesAsync();
                return RedirectToAction("Products");
            }
            return View(product);
        }

        public IActionResult DeleteProduct(int id)
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                _db.Products.Remove(product);
                _db.SaveChanges();
            }
            return RedirectToAction("Products");
        }

        public IActionResult UpdateOrderStatus(int id, OrderStatus status)
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                order.Status = status;
                if (status == OrderStatus.Shipped)
                {
                    order.ShippedDate = DateTime.Now;
                }
                else if (status == OrderStatus.Delivered)
                {
                    order.DeliveredDate = DateTime.Now;
                }
                _db.SaveChanges();
            }
            return RedirectToAction("Orders");
        }

        [HttpPost]
        public IActionResult AcceptOrder(int id)
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                order.Status = OrderStatus.Confirmed;
                _db.SaveChanges();
                return Json(new { success = true, message = "Order accepted successfully" });
            }
            return Json(new { success = false, message = "Order not found" });
        }

        [HttpPost]
        public IActionResult RejectOrder(int id)
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                order.Status = OrderStatus.Cancelled;
                _db.SaveChanges();
                return Json(new { success = true, message = "Order rejected successfully" });
            }
            return Json(new { success = false, message = "Order not found" });
        }

        public IActionResult OrderDetails(int id)
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Auth");
            }

            var order = _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public IActionResult UpdateOrderStatusAjax(int id, OrderStatus status)
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                order.Status = status;
                if (status == OrderStatus.Shipped)
                {
                    order.ShippedDate = DateTime.Now;
                }
                else if (status == OrderStatus.Delivered)
                {
                    order.DeliveredDate = DateTime.Now;
                }
                _db.SaveChanges();
                return Json(new { success = true, message = "Order status updated successfully" });
            }
            return Json(new { success = false, message = "Order not found" });
        }

        [HttpGet]
        public IActionResult GetPendingOrders()
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return Json(new List<object>());
            }

            var pendingOrders = _db.Orders
                .Where(o => o.Status == OrderStatus.Pending)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    id = o.Id,
                    customerName = o.CustomerName,
                    totalAmount = o.TotalAmount,
                    orderDate = o.OrderDate
                })
                .ToList();

            return Json(pendingOrders);
        }

        [HttpGet]
        public IActionResult GetOrderStatistics()
        {
            // Check if user is logged in and is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var stats = new
            {
                totalOrders = _db.Orders.Count(),
                pendingOrders = _db.Orders.Count(o => o.Status == OrderStatus.Pending),
                confirmedOrders = _db.Orders.Count(o => o.Status == OrderStatus.Confirmed),
                processingOrders = _db.Orders.Count(o => o.Status == OrderStatus.Processing),
                shippedOrders = _db.Orders.Count(o => o.Status == OrderStatus.Shipped),
                deliveredOrders = _db.Orders.Count(o => o.Status == OrderStatus.Delivered),
                cancelledOrders = _db.Orders.Count(o => o.Status == OrderStatus.Cancelled),
                totalRevenue = _db.Orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                todayOrders = _db.Orders.Count(o => o.OrderDate.Date == DateTime.Today)
            };

            return Json(stats);
        }

        // Static method to get products for other controllers
        public List<Product> GetProducts()
        {
            return _db.Products.Where(p => p.IsActive).ToList();
        }

        // Static method to add order
        public void AddOrder(Order order)
        {
            _db.Orders.Add(order);
            _db.SaveChanges();
        }

        // Static method to get order
        public Order? GetOrder(int orderId)
        {
            return _db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == orderId);
        }

        // Static method to get all orders
        public List<Order> GetAllOrders()
        {
            return _db.Orders.Include(o => o.OrderItems).ToList();
        }
    }
}


