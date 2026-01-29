 using Microsoft.AspNetCore.Mvc;
using MyMvcApp.Models;
using System.Text.Json;
using MyMvcApp.Data;
using Microsoft.EntityFrameworkCore;

namespace MyMvcApp.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ShopController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var products = _db.Products.Where(p => p.IsActive).ToList();
            return View(products);
        }

        public IActionResult Product(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var products = _db.Products.Where(p => p.IsActive).ToList();
            var product = products.FirstOrDefault(p => p.Id == id);
            
            if (product == null)
            {
                return RedirectToAction("Index");
            }

            // Get related products for the view
            var relatedProducts = products
                .Where(p => p.Category == product.Category && p.Id != product.Id)
                .Take(4)
                .ToList();
            
            ViewBag.RelatedProducts = relatedProducts;
            return View(product);
        }

        public IActionResult Category(string category)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var products = _db.Products.Where(p => p.IsActive).ToList();
            var filteredProducts = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            
            ViewBag.Category = category;
            return View(filteredProducts);
        }

        public IActionResult Search(string query)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var products = _db.Products.Where(p => p.IsActive).ToList();
            var searchResults = products.Where(p => 
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Manufacturer.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            ViewBag.SearchQuery = query;
            return View(searchResults);
        }

        public IActionResult Cart()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var uid = int.Parse(userId);
            var userCartItems = _db.CartItems.Where(c => c.UserId == uid)
                .Include(c => c.Product)
                .ToList();
            return View(userCartItems);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var userIdInt = int.Parse(userId);
            var existingItem = _db.CartItems.FirstOrDefault(c => c.UserId == userIdInt && c.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = userIdInt,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.Now
                });
            }
            _db.SaveChanges();
            return Json(new { success = true, message = "Product added to cart" });
        }

        [HttpPost]
        public IActionResult UpdateCart([FromBody] UpdateCartRequest request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var userIdInt = int.Parse(userId);
            var cartItem = _db.CartItems.FirstOrDefault(c => c.UserId == userIdInt && c.ProductId == request.ProductId);
            
            if (cartItem != null)
            {
                cartItem.Quantity = request.Quantity;
            }
            _db.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var userIdInt = int.Parse(userId);
            var cartItem = _db.CartItems.FirstOrDefault(c => c.UserId == userIdInt && c.ProductId == request.ProductId);
            
            if (cartItem != null)
            {
                _db.CartItems.Remove(cartItem);
            }
            _db.SaveChanges();
            return Ok();
        }

        public IActionResult Checkout()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userCartItems = _db.CartItems.Where(c => c.UserId == int.Parse(userId)).ToList();
            
            if (!userCartItems.Any())
            {
                return RedirectToAction("Cart");
            }

            // Populate product information
            var products = _db.Products.Where(p => p.IsActive).ToList();
            foreach (var item in userCartItems)
            {
                item.Product = products.FirstOrDefault(p => p.Id == item.ProductId) ?? new Product();
            }

            ViewBag.CartItems = userCartItems;
            return View(new Order());
        }

        [HttpPost]
        public IActionResult Checkout(Order order)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get cart items
            var cartItemsForOrder = _db.CartItems.Where(c => c.UserId == int.Parse(userId)).ToList();
            if (!cartItemsForOrder.Any())
            {
                return RedirectToAction("Cart");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(order.CustomerName) || 
                string.IsNullOrEmpty(order.CustomerPhone) || 
                string.IsNullOrEmpty(order.ShippingAddress) || 
                string.IsNullOrEmpty(order.City) || 
                string.IsNullOrEmpty(order.PostalCode))
            {
                var userCartItems = _db.CartItems.Where(c => c.UserId == int.Parse(userId)).ToList();
                var allProducts = _db.Products.Where(p => p.IsActive).ToList();
                foreach (var item in userCartItems)
                {
                    item.Product = allProducts.FirstOrDefault(p => p.Id == item.ProductId) ?? new Product();
                }
                ViewBag.CartItems = userCartItems;
                ModelState.AddModelError("", "Please fill in all required fields.");
                return View(order);
            }

            // Set order properties
            order.UserId = int.Parse(userId);
            order.OrderDate = DateTime.Now;
            order.Status = OrderStatus.Pending;
            order.PaymentMethod = "Cash on Delivery"; // Force COD only
            order.CustomerEmail = HttpContext.Session.GetString("UserEmail") ?? "";
            order.SpecialInstructions = order.SpecialInstructions ?? ""; // Ensure it's never null

            // Calculate totals
            var products = _db.Products.Where(p => p.IsActive).ToList();
            decimal subtotal = 0;
            
            foreach (var cartItem in cartItemsForOrder)
            {
                var product = products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (product != null)
                {
                    subtotal += product.Price * cartItem.Quantity;
                }
            }

            order.Subtotal = subtotal;
            order.TaxAmount = subtotal * 0.05m; // 5% tax
            order.ShippingCost = 5.00m; // Fixed shipping
            order.TotalAmount = order.Subtotal + order.TaxAmount + order.ShippingCost;

            // Save order first to get the ID
            _db.Orders.Add(order);
            _db.SaveChanges();

            // Now create order items with the correct OrderId
            foreach (var cartItem in cartItemsForOrder)
            {
                var product = products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (product != null)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id, // Now this will have the correct ID
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * cartItem.Quantity
                    };
                    _db.OrderItems.Add(orderItem);
                }
            }

            // Clear user's cart
            _db.CartItems.RemoveRange(cartItemsForOrder);
            _db.SaveChanges();

            // Set success message
            TempData["OrderSuccess"] = $"Order #{order.Id} placed successfully! You will receive a confirmation call shortly.";

            return RedirectToAction("OrderConfirmation", new { id = order.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var order = _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == id);
                
            if (order == null || order.UserId != int.Parse(userId))
            {
                return RedirectToAction("Index");
            }

            return View(order);
        }

    }

    public class UpdateCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveFromCartRequest
    {
        public int ProductId { get; set; }
    }
}
