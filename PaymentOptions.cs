using System.ComponentModel.DataAnnotations;

public class PaymentOptions
{
    [Required]
    public string GatewayUrl { get; set; } = string.Empty;

    [Range(100, 100000)]
    public decimal MaxDepositBirr { get; set; }
}