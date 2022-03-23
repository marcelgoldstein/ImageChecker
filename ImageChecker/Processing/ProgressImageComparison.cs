
namespace ImageChecker.Processing;

public class ProgressImageComparison
{
    public double Minimum { get; set; }
    public double Maximum { get; set; }
    public double Value { get; set; }
    public string Operation { get; set; }

    public long? EstimatedRemainingSeconds { get; set; }
    public long? TotalRunningSeconds { get; set; }

    public ProgressImageComparison(double minimum, double maximum, double value, string operation, long? estimatedRemainingSeconds, long? totalRunningSeconds)
    {
        Minimum = minimum;
        Maximum = maximum;
        Value = value;
        Operation = operation;
        EstimatedRemainingSeconds = estimatedRemainingSeconds;
        TotalRunningSeconds = totalRunningSeconds;
    }
}
