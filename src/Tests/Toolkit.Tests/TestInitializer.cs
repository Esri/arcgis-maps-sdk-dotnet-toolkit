using System;
using System.Collections.Generic;
using System.Text;

namespace Toolkit.Tests
{
    [TestClass]
    public class TestInitializer
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            if (context.Properties.ContainsKey("APIKey"))
                Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = context.Properties["APIKey"] as string ?? string.Empty;
        }

        internal static void AssertAPIKey()
        {
            if (string.IsNullOrEmpty(Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey))
                Assert.Inconclusive("API KEY is required for this test. See TestInitializer.cs");
        }
    }
}
