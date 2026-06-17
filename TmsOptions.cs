using System.ComponentModel.DataAnnotations;

public class TmsOptions
{
    // 'Required' የሚለው አትሪቢዩት፣ ይህ መረጃ በ json ውስጥ የግድ መኖር እንዳለበት ይቆጣጠራል
    [Required]
    public string GatewayUrl { get; set; } = string.Empty;
}