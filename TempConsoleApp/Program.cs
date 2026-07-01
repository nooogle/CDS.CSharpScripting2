Console.WriteLine(Math.Pow(3, 4));

Console.WriteLine(Calc.Add(2, 3));

Math.Asinh(1);
Math.Clamp(1, 2, 3);



/// <summary>My calculator!</summary>
[Obsolete]
static class Calc
{
    /// <summary>
    /// Adds two integers. See also <see cref="Math.Abs(int)"/>.
    /// </summary>
    /// <param name="a">The first value</param>
    /// <param name="b">The second value</param>
    /// <returns>The sum of the two values</returns>
    public static int Add(int a, int b) => a + b;
};

