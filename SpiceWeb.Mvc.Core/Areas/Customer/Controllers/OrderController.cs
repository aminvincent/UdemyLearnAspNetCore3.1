using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceWeb.Mvc.Core.Data;
using SpiceWeb.Mvc.Core.Models;
using SpiceWeb.Mvc.Core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SpiceWeb.Mvc.Core.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Authorize] //login to access this method
        public async Task<IActionResult> Confirm(int Id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel OrderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(x => x.ApplicationUser).FirstOrDefaultAsync(x => x.Id == Id && x.UserId == claim.Value), //hanya menampilkan order dari masing2 user
                OrderDetails = await _db.OrderDetails.Where(x => x.OrderId ==Id).ToListAsync()
            };

            return View(OrderDetailsViewModel);
        }
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            List<OrderDetailsViewModel> orderList = new List<OrderDetailsViewModel>();

            List<OrderHeader> OrderHeaderList = await _db.OrderHeader.Include(x => x.ApplicationUser).Where(x => x.UserId == claim.Value).ToListAsync();

            foreach (var item in OrderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(x => x.OrderId == item.Id).ToListAsync()
                };
                orderList.Add(individual);
            }

            return View(orderList);
        }

        public async Task<IActionResult> GetOrderDetails(int Id)
        {
            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.FirstOrDefaultAsync(x => x.Id == Id),
                OrderDetails = await _db.OrderDetails.Where(x => x.OrderId == Id).ToListAsync()
            };
            orderDetailsViewModel.OrderHeader.ApplicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(x => x.Id == orderDetailsViewModel.OrderHeader.UserId);

            return PartialView("_IndividualOrderDetails", orderDetailsViewModel);
        }
    }
}
