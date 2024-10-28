using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleScratchFramework
{
    public class ScriptSample
    {
        public string Code { get; set; }
        public int Position { get; set; }
    }


    public static class ScriptSamples
    {
        private static ScriptSample s1 = CreateS1();
        private static ScriptSample s2 = CreateS2();
        private static ScriptSample s3 = CreateS3();
        private static ScriptSample s4 = CreateS4();


        public static ScriptSample S1 => s1;
        public static ScriptSample S2 => s2;
        public static ScriptSample S3 => s3;
        public static ScriptSample S4 => s4;


        private static ScriptSample CreateS1()
        {
            var scriptSample = new ScriptSample();

            scriptSample.Code = @"
        class XXX
        {
            /// <summary>
            /// Adds two integers
            /// </summary>
            /// <param name=""a"">First int</param>
            /// <param name=""b"">Second int</param>
            /// <returns>Sum of two integers</returns>
            public static int Add(int a, int b)
            {
                return a + b;
            }

            /// <summary>
            /// Adds two floats
            /// </summary>
            /// <param name=""a"">First float</param>
            /// <param name=""bb"">2nd float</param>
            /// <returns>Sum of two floats</returns>
            public static float Add(float a, float bb)
            {
                return a + bb;
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
";

            scriptSample.Position = scriptSample.Code.IndexOf(".Add") + 5;

            return scriptSample;
        }


        private static ScriptSample CreateS2()
        {
            var scriptSample = new ScriptSample();
            scriptSample.Code = "Console.WriteLine(";
            scriptSample.Position = s2.Code.Length;
            return scriptSample;
        }


        private static ScriptSample CreateS3()
        {
            var scriptSample = new ScriptSample();

            scriptSample.Code = @"
    /// <summary>
    /// Calculates a term of the Fibonacci sequence
    /// </summary>
    /// <param name=""n"">Term</param>
    /// <returns>Sequence value</returns>
    int Fibonacci(int n) => 0;
    Fib";

            scriptSample.Position = scriptSample.Code.Length;
        }



        private static ScriptSample CreateS4()
        {
            var scriptSample = new ScriptSample();

            scriptSample.Code = @"
int xxx = 10;
xxx = xxx * yyy;
";

            scriptSample.Position = scriptSample.Code.Length;

            return scriptSample;
        }
    }
}


