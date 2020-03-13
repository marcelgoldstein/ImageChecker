
namespace ImageChecker.Processing
{
    public class ProgressRenamingFiles
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Value { get; set; }
        public string Operation { get; set; }

        public ProgressRenamingFiles(double minimum, double maximum, double value, string operation)
        {
            Minimum = minimum;
            Maximum = maximum;
            Value = value;
            Operation = operation;
        }
    }
}
