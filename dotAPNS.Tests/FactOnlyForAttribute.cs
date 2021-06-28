using System;
using Xunit;

namespace dotAPNS.Tests
{
    public class FactOnlyForAttribute : FactAttribute
    {
        public FactOnlyForAttribute(PlatformID platform)
        {
            if (Environment.OSVersion.Platform != platform)
                Skip = "Test is skipped for current platform.";
        }
    }
}
