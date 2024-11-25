using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyMSTest;

namespace DotNet6UnitTests
{
    /// <summary>
    /// A demonstration of the Verify API using customised verification filenames
    /// to facilitate multiple verifications per test method.
    /// </summary>
    [TestClass]
    public partial class UT_VerifyScratchpad
    {
        /// <summary>
        /// A simple test that verifies a single addition operation. Using a custom
        /// filename isn't required here. However, this customised filename includes
        /// the namespace and provides consistency with the other tests in this class.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task NoDataRows_SingleVerification()
        {
            int additionResult = 100 + 200;

            await
                Verifier
                .Verify(additionResult, VerifySupport.Settings)
                .UseFileName(VerifySupport.SimpleFileName());
        }


        /// <summary>
        /// A simple test that verifies multiple operations using a 
        /// compound result record.
        /// </summary>
        [TestMethod]
        public async Task NoDataRows_CompoundVerification()
        {
            int additionResult = 100 + 200;
            int multiplicationResult = 100 * 200;

            var actuals = new
            {
                Addition = additionResult,
                Multiplication = multiplicationResult
            };

            await
                Verifier
                .Verify(actuals, VerifySupport.Settings)
                .UseFileName(VerifySupport.SimpleFileName());
        }


        /// <summary>
        /// A simple test that verifies multiple operations, where each
        /// verification has a customised filename.
        /// *** IMO compound verification is better ***
        /// </summary>
        [TestMethod]
        public async Task NoDataRows_MultipleVerification()
        {
            int additionResult = 100 + 200;
            int multiplicationResult = 100 * 200;

            await
                Verifier
                .Verify(additionResult, VerifySupport.Settings)
                .UseFileName(VerifySupport.ExtendedFileName(testName: "Addition"));

            await
                Verifier
                .Verify(multiplicationResult, VerifySupport.Settings)
                .UseFileName(VerifySupport.ExtendedFileName(testName: "Multiplication"));
        }


        /// <summary>
        /// A test that verifies a single addition operation, using a DataRow
        /// to control the test data. Each data row provides a test name which
        /// is incorporated into the customised filename.
        /// </summary>
        [TestMethod]
        [DataRow(1, 2, "Test 1")]
        [DataRow(10, 20, "Test 2")]
        public async Task DataRows_SingleVerification(int a, int b, string testName)
        {
            int additionResult = a + b;

            await
                Verifier
                .Verify(additionResult, VerifySupport.Settings)
                .UseFileName(VerifySupport.ExtendedFileName(testName: $"{testName}"));
        }


        /// <summary>
        /// A more complex test that verifies multiple operations for each 
        /// data row using a single, compound result record. 
        /// Each datarow test gets a customised filename that includes
        /// the test name.
        /// </summary>
        [TestMethod]
        [DataRow(1, 2, "Test 1")]
        [DataRow(10, 20, "Test 2")]
        public async Task DataRows_CompoundVerification(int a, int b, string testName)
        {
            int additionResult = a + b;
            int multiplicationResult = a * b;

            var actuals = new
            {
                Addition = additionResult,
                Multiplication = multiplicationResult
            };

            await
                Verifier
                .Verify(actuals, VerifySupport.Settings)
                .UseFileName(VerifySupport.ExtendedFileName(testName: testName));
        }


        /// <summary>
        /// A more complex test that verifies multiple operations for each 
        /// data row. Each verification has a customised filename that includes
        /// both the test name and the operation being verified.
        /// *** IMO compound verification is better that multiple individual verifications ***
        /// </summary>
        [TestMethod]
        [DataRow(1, 2, "Test 1")]
        [DataRow(10, 20, "Test 2")]
        public async Task DataRows_MultipleVerification(int a, int b, string testName)
        {
            int additionResult = a + b;
            int multiplicationResult = a * b;

            await
                Verifier
                .Verify(additionResult, VerifySupport.Settings)
                .UseFileName(VerifySupport.ExtendedFileName(testName: $"Add_{testName}"));

            await
                Verifier
                .Verify(multiplicationResult, VerifySupport.Settings)
                .UseFileName(VerifySupport.ExtendedFileName(testName: $"Mult_{testName}"));
        }
    }
}