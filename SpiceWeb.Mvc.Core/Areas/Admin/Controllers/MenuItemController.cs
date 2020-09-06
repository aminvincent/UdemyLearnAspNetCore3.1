using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceWeb.Mvc.Core.Data;
using SpiceWeb.Mvc.Core.Models;

namespace SpiceWeb.Mvc.Core.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment; //ini digunakan untuk 

        //langung binding data digunakan untuk menampung menu item agar tidak perlu menambhakan parameter input disetiap Create/Edit/Delete POST
        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; } 

        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            MenuItemVM = new MenuItemViewModel()
            {
                Category = _db.Category,
                MenuItem = new Models.MenuItem() //untuk sub category tidak dtambahkan karene sub category akan menampilkan sesuai dengan category
            };
        }
        public async Task<IActionResult> Index()
        {
            var menuItems = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).ToListAsync();
            return View(menuItems);
        }

        //GET - CREATE
        public IActionResult Create()
        {
            return View(MenuItemVM); //langsung memanggil bind property yang sudah diberikan value pada constructor
        }
    }
}
