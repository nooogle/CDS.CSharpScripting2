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
        private static ScriptSample s1;
        private static ScriptSample s2;
        private static ScriptSample s3;


        public static ScriptSample S1 => s1;
        public static ScriptSample S2 => s2;
        public static ScriptSample S3 => s3;


        static ScriptSamples()
        {
            CreateS1();
            CreateS2();
            CreateS3();
        }   


        private static void CreateS1()
        {
            s1 = new ScriptSample();

            s1.Code = @"
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

            s1.Position = s1.Code.IndexOf(".Add") + 5;
        }


        private static void CreateS2()
        {
            s2 = new ScriptSample();
            s2.Code = "Console.WriteLine(";
            s2.Position = s2.Code.Length;
        }


        private static void CreateS3()
        {
            s3 = new ScriptSample();

            s3.Code = @"
    /// <summary>
    /// Calculates a term of the Fibonacci sequence
    /// </summary>
    /// <param name=""n"">Term</param>
    /// <returns>Sequence value</returns>
    int Fibonacci(int n) => 0;
    Fib";

            s3.Position = s3.Code.Length;
        }
    }
}
