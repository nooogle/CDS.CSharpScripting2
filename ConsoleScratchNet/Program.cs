class XXX
{
    /// <summary>
    /// Adds two integers
    /// </summary>
    /// <param name="a">The very first number</param>
    /// <param name="b">2nd number</param>
    /// <returns>Another number!</returns>
    public static int Add(int a, int b)
    {
        return a + b;
    }
}

class Program
{
    static void Main()
    {
        int result = XXX.Add(1, 2);
        System.Console.WriteLine(result);
    }
}