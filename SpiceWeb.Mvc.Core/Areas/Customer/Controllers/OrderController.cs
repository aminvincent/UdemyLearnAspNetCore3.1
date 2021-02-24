using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceWeb.Mvc.Core.Data;
using SpiceWeb.Mvc.Core.Models;
using SpiceWeb.Mvc.Core.Models.ViewModels;
using SpiceWeb.Mvc.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SpiceWeb.Mvc.Core.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _db;
        int PageSize = 2; //menampilkan 2 record di setiap page (paging)

        public OrderController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        [Authorize] //login to access this method
        public async Task<IActionResult> Confirm(int Id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel OrderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(x => x.ApplicationUser).FirstOrDefaultAsync(x => x.Id == Id && x.UserId == claim.Value), //hanya menampilkan order dari masing2 user
                OrderDetails = await _db.OrderDetails.Where(x => x.OrderId == Id).ToListAsync()
            };

            return View(OrderDetailsViewModel);
        }
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage = 1)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            //List<OrderDetailsViewModel> orderList = new List<OrderDetailsViewModel>();

            List<OrderHeader> OrderHeaderList = await _db.OrderHeader.Include(x => x.ApplicationUser).Where(x => x.UserId == claim.Value).ToListAsync();

            foreach (var item in OrderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(x => x.OrderId == item.Id).ToListAsync()
                };
                //orderList.Add(individual);
                orderListVM.Orders.Add(individual); //digunakan untuk paging
            }

            //digunakan untuk paging
            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(x => x.OrderHeader.Id)
                                .Skip((productPage - 1) * PageSize)
                                .Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo()
            {
                CurrentPage = productPage,
                ItemPerPage = PageSize,
                TotalItem = count,
                urlParam = "/Customer/Order/OrderHistory?productPage=:"
            };

            //return View(orderList);
            return View(orderListVM); //digunakan untuk paging
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            List<OrderDetailsViewModel> OrderDetailVM = new List<OrderDetailsViewModel>();

            List<OrderHeader> OrderHeaderList = await _db.OrderHeader.Where(x => x.Status == SD.StatusSubmitted || x.Status == SD.StatusInProcess)
                                                    .OrderByDescending(x => x.PickupTime).ToListAsync();

            foreach (var item in OrderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(x => x.OrderId == item.Id).ToListAsync()
                };
                OrderDetailVM.Add(individual);
            }

            return View(OrderDetailVM.OrderBy(x => x.OrderHeader.PickupTime));
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

        public IActionResult GetOrderStatus(int Id)
        {
            return PartialView("_OrderStatus", _db.OrderHeader.FirstOrDefault(x => x.Id == Id).Status);
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusInProcess;
            await _db.SaveChangesAsync();

            return RedirectToAction("ManageOrder", "Order"); //redirect to Manage Order
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderReady(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusReady;
            await _db.SaveChangesAsync();

            //email logic to notify user that order is ready pickup
            //send email menggunakan send grid -> karena belum mendaptkan key send grid maka tidak bisa mengirimkan email
            //await _emailSender.SendEmailAsync(_db.Users.Where(x => x.Id == orderHeader.UserId).FirstOrDefault().Email, "SPice - Order Ready for Pickup " + orderHeader.Id.ToString(), "Order is ready for pikcup");


            return RedirectToAction("ManageOrder", "Order"); //redirect to Manage Order
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusCancelled;
            await _db.SaveChangesAsync();

            //send email menggunakan send grid -> karena belum mendaptkan key send grid maka tidak bisa mengirimkan email
            //await _emailSender.SendEmailAsync(_db.Users.Where(x => x.Id == orderHeader.UserId).FirstOrDefault().Email, "SPice - Order cancelled " + orderHeader.Id.ToString(), "Order Has been cancelled successfully");

            return RedirectToAction("ManageOrder", "Order"); //redirect to Manage Order
        }

        [Authorize]
        public async Task<IActionResult> OrderPickup(int productPage = 1, string searchName = null, string searchPhone = null, string searchEmail = null)
        {
            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            //adding append string builder to searchName, searchEmail, searchPhone
            StringBuilder param = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?productPage=:");
            param.Append("&searchName=");
            if (searchName != null)
                param.Append(searchName);
            param.Append("&searchEmail=");
            if (searchEmail != null)
                param.Append(searchEmail);
            param.Append("&searchPhone=");
            if (searchPhone != null)
                param.Append(searchPhone);

            List<OrderHeader> OrderHeaderList = new List<OrderHeader>();

            //giving search condition
            if (searchName != null || searchEmail != null || searchPhone != null)
            {
                var user = new ApplicationUser();

                if (searchName != null)
                {
                    OrderHeaderList = await _db.OrderHeader.Include(x => x.ApplicationUser)
                        .Where(x => x.PickupName.ToLower().Contains(searchName.ToLower()))
                        .OrderByDescending(x => x.OrderDate).ToListAsync();
                }
                else
                {
                    if (searchEmail != null)
                    {
                        //user = await _db.ApplicationUser.Where(x => x.Email.ToLower().Contains(searchEmail.ToLower())).FirstOrDefaultAsync();
                        //OrderHeaderList = await _db.OrderHeader.Include(x => x.ApplicationUser)
                        //    .Where(x => x.UserId == user.Id)
                        //    .OrderByDescending(x => x.OrderDate).ToListAsync();
                        OrderHeaderList = await _db.OrderHeader.Include(x => x.ApplicationUser)
                            .Where(x => x.ApplicationUser.Email.ToLower().Contains(searchEmail.ToLower()))
                            .OrderByDescending(x => x.OrderDate).ToListAsync();
                    }
                    else
                    {
                        if (searchPhone != null)
                        {
                            OrderHeaderList = await _db.OrderHeader.Include(x => x.ApplicationUser)
                                .Where(x => x.PhoneNumber.Contains(searchPhone))
                                .OrderByDescending(x => x.OrderDate).ToListAsync();
                        }
                    }
                }
            }
            else
            {
                OrderHeaderList = await _db.OrderHeader.Include(x => x.ApplicationUser).Where(x => x.Status == SD.StatusReady).ToListAsync();
            }

            foreach (var item in OrderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(x => x.OrderId == item.Id).ToListAsync()
                };
                orderListVM.Orders.Add(individual); //digunakan untuk paging
            }

            //digunakan untuk paging
            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(x => x.OrderHeader.Id)
                                .Skip((productPage - 1) * PageSize)
                                .Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo()
            {
                CurrentPage = productPage,
                ItemPerPage = PageSize,
                TotalItem = count,
                urlParam = param.ToString()
            };

            return View(orderListVM); //digunakan untuk paging
        }

        [Authorize(Roles = SD.FrontDeskUser + "," + SD.ManagerUser)]
        [HttpPost]
        [ActionName("OrderPickup")]
        public async Task<IActionResult> OrderPickupPost(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusCompleted;
            await _db.SaveChangesAsync();

            //send email menggunakan send grid -> karena belum mendaptkan key send grid maka tidak bisa mengirimkan email
            //await _emailSender.SendEmailAsync(_db.Users.Where(x => x.Id == orderHeader.UserId).FirstOrDefault().Email, "SPice - Order Completed " + orderHeader.Id.ToString(), "Order Has been completed successfully");

            return RedirectToAction("OrderPickup", "Order");
        }
    }
}
