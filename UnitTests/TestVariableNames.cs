using System;
using System.Collections.Generic;
using System.Reflection;

using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    public class TestVariableNames
    {

        [Test]
        public void ListVariableNames()
        {
            var variableNameSymbols = GetVariableNamesWithSymbols();

            foreach (var variable in variableNameSymbols)
            {
                Console.WriteLine($"Variable: {variable.Key}, Symbol: {variable.Value}");
            }

        }
        
        private static Dictionary<string, string> GetVariableNamesWithSymbols()
        {
            var result = new Dictionary<string, string>();

            // Get all fields of the VariableNames enum
            var enumValues = typeof(VariableNames).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var fieldInfo in enumValues)
            {
                // Get the SymbolAttribute if it exists
                var symbolAttribute = fieldInfo.GetCustomAttribute<SymbolAttribute>();

                if (symbolAttribute != null)
                {
                    result.Add(fieldInfo.Name, symbolAttribute.Symbol);
                }
                else
                {
                    result.Add(fieldInfo.Name, "No Symbol Defined");
                }
            }

            return result;
        }

        
    }
}