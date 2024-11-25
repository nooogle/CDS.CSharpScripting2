using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyMSTest;

[assembly: UsesVerify]


[TestClass]
public partial class VerifySupport
{
    [TestMethod]
    public async Task Run()
    {
        await VerifyChecks.Run();
    }
}
