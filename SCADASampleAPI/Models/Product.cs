using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCADASampleAPI.Models;

/// <summary>Managed product/fluid catalog entry. Uses string Code as PK (e.g. "DIESEL").</summary>
[Table("Products")]
public class Product
{
    [Key]
    [MaxLength(32)]
    public string Code { get; set; } = "";

    [MaxLength(128)]
    public string Name { get; set; } = "";

    /// <summary>Hex color for UI rendering (e.g. "#F5A623").</summary>
    [MaxLength(16)]
    public string HexColor { get; set; } = "#64748b";

    /// <summary>Broad classification: Petroleum, Gas, Water, Blend.</summary>
    [MaxLength(32)]
    public string ProductType { get; set; } = "Blend";

    /// <summary>Density in kg/m³. Gas products use kg/m³ at standard conditions.</summary>
    public double Density { get; set; }

    public bool IsActive { get; set; } = true;
}
