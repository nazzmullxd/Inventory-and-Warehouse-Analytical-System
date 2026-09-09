using System.Reflection;

namespace Iwas.Business.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class FactAttribute : Attribute;

public static class Assert
{
    public static void True(bool value) { if (!value) throw new Exception("Expected true."); }
    public static void False(bool value) { if (value) throw new Exception("Expected false."); }
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected}; actual {actual}.");
    }
    public static void Equal(double expected, double actual, int precision)
    {
        if (Math.Round(expected, precision) != Math.Round(actual, precision))
            throw new Exception($"Expected {expected}; actual {actual}.");
    }
    public static void InRange(decimal actual, decimal low, decimal high)
    {
        if (actual < low || actual > high) throw new Exception($"Expected {actual} in [{low}, {high}].");
    }
    public static T Single<T>(IEnumerable<T> values)
    {
        var array = values.ToArray();
        if (array.Length != 1) throw new Exception($"Expected one item; actual {array.Length}.");
        return array[0];
    }
    public static void Collection<T>(IEnumerable<T> values, params Action<T>[] inspectors)
    {
        var array = values.ToArray();
        Equal(inspectors.Length, array.Length);
        for (var i = 0; i < array.Length; i++) inspectors[i](array[i]);
    }
}

public static class Program
{
    public static int Main()
    {
        var instance = new BusinessAcceptanceTests();
        var tests = typeof(BusinessAcceptanceTests).GetMethods()
            .Where(x => x.GetCustomAttribute<FactAttribute>() is not null).OrderBy(x => x.Name).ToArray();
        var failed = 0;
        foreach (var test in tests)
        {
            try { test.Invoke(instance, null); Console.WriteLine($"PASS {test.Name}"); }
            catch (TargetInvocationException error) { failed++; Console.WriteLine($"FAIL {test.Name}: {error.InnerException?.Message}"); }
        }
        Console.WriteLine($"{tests.Length - failed}/{tests.Length} business acceptance tests passed.");
        return failed == 0 ? 0 : 1;
    }
}
