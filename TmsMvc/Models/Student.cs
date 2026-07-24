using System.ComponentModel.DataAnnotations;

namespace TmsMvc.Models;

public class Student
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Registration Number is required")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Range(0.0, 4.0, ErrorMessage = "GPA must be between 0 and 4")]
    public decimal GPA { get; set; }

    public bool IsActive { get; set; }
}