using System.ComponentModel.DataAnnotations;

public class TmsOptions
{
    [Required]
    public string GatewayUrl { get; set; } = string.Empty;
}