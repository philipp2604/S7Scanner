namespace S7Scanner.Lib.Models;

/// <summary>
/// Represents detailed information gathered from a Siemens S7 PLC.
/// </summary>
public record PlcDetails
{
    /// <summary>
    /// Information on the PLC module.
    /// </summary>
    public string? Module { get; init; }

    /// <summary>
    /// Information on the PLC's basic hardware.
    /// </summary>
    public string? BasicHardware { get; init; }

    /// <summary>
    /// Information on the PLC's version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Information on the PLC's system name.
    /// </summary>
    public string? SystemName { get; init; }

    /// <summary>
    /// Information on the PLC's module type.
    /// </summary>
    public string? ModuleType { get; init; }

    /// <summary>
    /// Information on the PLC's serial number.
    /// </summary>
    public string? SerialNumber { get; init; }

    /// <summary>
    /// Information on the PLC's plant identifier.
    /// </summary>
    public string? PlantIdentification { get; init; }

    /// <summary>
    /// Information on the PLC copyright.
    /// </summary>
    public string? Copyright { get; init; }
}