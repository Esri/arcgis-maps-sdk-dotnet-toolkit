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
            // Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = "";
        }
    }
}
