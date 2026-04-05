namespace SCADASampleAPI.Options;

public class SyntheticScadaOptions
{
    public const string SectionName = "SyntheticScada";

    public int UpdateIntervalSeconds { get; set; } = 2;

    /// <summary>Random walk step as a fraction of (MaxValue - MinValue).</summary>
    public double StepFraction { get; set; } = 0.03;
}
