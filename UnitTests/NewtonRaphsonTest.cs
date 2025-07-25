using System;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class NewtonRaphsonTest
    {
        [Test]
        public void TestNewtonRaphson()
        {
         
            var involuteValue = 0.034316; // Example involute value
            var initialGuess = 0.5; // Initial guess for alpha
            var tolerance = 1e-6; // Tolerance for convergence
            var maxIterations = 100; // Maximum number of iterations

            var alpha = CustomCalculations.PressureAngleByNewtonRaphson(involuteValue, initialGuess, tolerance, maxIterations);

            ConsoleIO.WriteLine($"Calculated angle (alpha) in radians: {alpha}");
            ConsoleIO.WriteLine($"Calculated angle (alpha) in degrees: {alpha * (180.0 / Math.PI)}");
            
            Assert.That(alpha, Is.EqualTo(0.455330).Within(0.000001));
            Assert.That(alpha * (180.0 / Math.PI), Is.EqualTo(26.0885324).Within(0.000001));
        }

        
       

        
    }
}