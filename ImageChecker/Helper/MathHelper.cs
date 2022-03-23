namespace ImageChecker.Helper;

public static class MathHelper
{
    /// <summary>
    /// Calculates the GaussSum for 'n'.
    /// </summary>
    /// <param name="n"></param>
    /// <returns>GaussSum</returns>
    public static long SumToN(long n)
    {
        return n * (n + 1) / 2;
    }

    /// <summary>
    /// Calculates the GaussSum for 'n' till 'm'.
    /// </summary>
    /// <param name="n"></param>
    /// <param name="m"></param>
    /// <returns>GaussSum</returns>
    public static long PartialSumToNProgress(long n, long progress)
    {
        long sum = 0L;

        for (long i = 1; i <= progress; i++)
        {
            sum += n - i;
        }

        return sum;
    }
}
