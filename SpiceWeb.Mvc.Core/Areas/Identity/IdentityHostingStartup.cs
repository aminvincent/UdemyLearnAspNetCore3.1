using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpiceWeb.Mvc.Core.Data;

[assembly: HostingStartup(typeof(SpiceWeb.Mvc.Core.Areas.Identity.IdentityHostingStartup))]
namespace SpiceWeb.Mvc.Core.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            //ini di remove karena config sesuai dengan startup.cs sebelumnya, class ini adalah hasil dari generate scaffold identify Account User.
            /*builder.ConfigureServices((context, services) => {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("ApplicationDbContextConnection")));*/

            //services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
            //    .AddEntityFrameworkStores<ApplicationDbContext>();
        //});
        }
}
}