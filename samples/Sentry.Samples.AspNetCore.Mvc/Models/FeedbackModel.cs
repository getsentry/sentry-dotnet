using System.ComponentModel.DataAnnotations;

namespace Samples.AspNetCore.Mvc.Models;

public class FeedbackModel
{
    [Required]
    public string? Message { get; set; }

    [EmailAddress]
    public string? ContactEmail { get; set; }

    public string? Name { get; set; }

    public IFormFile? Screenshot { get; set; }
}
