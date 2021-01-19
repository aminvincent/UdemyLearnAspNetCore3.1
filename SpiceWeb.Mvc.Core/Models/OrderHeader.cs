using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceWeb.Mvc.Core.Models
{
    public class OrderHeader
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser ApplicationUser { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public double OrderTotalOriginal { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:C}")]
        [Display(Name = "Order Total")]
        public double OrderTotal { get; set; }

        [Required]
        [Display(Name = "Pickup Time")]
        public DateTime PickupTime { get; set; }

        [Required]
        [NotMapped] //berarti tidak masuk di dalam database (pengecualian)
        public DateTime PickupDate { get; set; }


        [Display(Name = "Coupon Code")]
        public string CouponCode { get; set; }
        public double CouponCodeDiscount { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string Comments { get; set; }

        [Display(Name = "Pickup Name")]
        public string PickupName { get; set; }


        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        public string TransactionId { get; set; }
    }
}
