using System;
using Xunit;

namespace ELMElectronics.Test
{
    public class BasicTests
    {
        public const string COM_PORT_FORMAT = "com{0}";
        public const ushort COM_PORT_NUMBER_DEFAULT_VALUE = 3;
        public static string COM_PORT = String.Format(COM_PORT_FORMAT, COM_PORT_NUMBER_DEFAULT_VALUE);

        [Fact]
        public void Connection()
        {
            ELM327 elm327 = new ELM327(COM_PORT);
            elm327.Connect();
            Assert.True(elm327.ICVersion != 0);
        }
    }
}
