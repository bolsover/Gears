using System;

namespace Bolsover.Gears.Calculator;

public static class CustomCalculations
{
    /// <summary>
    /// Uses the Newton-Raphson method to calculate the pressure angle represented by the given involute function k.
    /// </summary>
    /// <param id="k - the involute function"></param>
    /// <param id="initialGuess"></param>
    /// <param id="tolerance"></param>
    /// <param id="maxIterations"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static double PressureAngleByNewtonRaphson(double k, double initialGuess, double tolerance,
        int maxIterations)
    {
        var alpha = initialGuess;
        int iteration = 0;

        while (iteration < maxIterations)
        {
            var f = Math.Tan(alpha) - alpha - k;
            var fPrime = 1 / Math.Cos(alpha) / Math.Cos(alpha) - 1;
            var nextAlpha = alpha - f / fPrime;
            if (Math.Abs(nextAlpha - alpha) < tolerance)
            {
                return nextAlpha;
            }

            alpha = nextAlpha;
            iteration++;
        }

        return 0;
        // throw new Exception("Newton-Raphson method did not converge.");
    }
}