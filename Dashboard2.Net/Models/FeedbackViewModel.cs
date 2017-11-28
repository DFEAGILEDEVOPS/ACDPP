using System;
using System.ComponentModel.DataAnnotations;

namespace Dashboard.Net.Models
{
    public class FeedbackViewModel
    {
        public string Date { get; set; } = DateTime.Now.ToShortDateString();

        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [StringLength(2000, ErrorMessage = "Please enter up to 2000 characters")]
        [Display(Name = "Feedback details")]
        public string Details { get; set; }
        public string SourceUrl { get; set; }
    }
}