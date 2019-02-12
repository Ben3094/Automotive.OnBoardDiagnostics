using System;
using Xunit;

namespace ELMElectronics.Test
{
    public class BasicTests
    {
        public const string COM_PORT_FORMAT = "com{0}";
        public const ushort COM_PORT_NUMBER_DEFAULT_VALUE = 3;
        public static string COM_PORT = String.Format(COM_PORT_FORMAT, COM_PORT_NUMBER_DEFAULT_VALUE);
        ELM327 elm327 = new ELM327(COM_PORT);

        [Fact]
        public void Connection()
        {
            elm327.Connect();
            Assert.True(elm327.ICVersion != 0);
        }

        [Fact]
        public void NonVolatileDataSaving()
        {
            byte[] randomBytes = new byte[1];
            new Random().NextBytes(randomBytes);
            elm327.NonVolatileData = randomBytes[0];

            elm327.WarnReset();
            elm327.Connect();

            Assert.True(elm327.NonVolatileData == randomBytes[0]);
        }

        #region Protocol handling
        [Fact]
        public void ProtocolSetting()
        {
            ELM327.OBDProtocol protocol = (ELM327.OBDProtocol)(new Random().Next() % 13);
            elm327.Protocol = protocol;
            bool fallback = (new Random().Next() & 0b1) == 1;
            elm327.FallbackAutomaticProtocolSearch = fallback;

            Assert.True((protocol == elm327.Protocol) && (fallback == elm327.FallbackAutomaticProtocolSearch));
        }
        #endregion
    }
}
