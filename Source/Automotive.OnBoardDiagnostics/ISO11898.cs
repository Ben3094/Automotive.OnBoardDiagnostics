//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Numerics;
//using System.Data.HashFunction.CRC;

//namespace Automotive.CANBus
//{
//    public abstract class Bus
//    {
//        public const bool START_OF_FRAME = false;

//        public abstract byte ARBITRATION_FIELD_NUMBER { get; }

//        public const byte COMMAND_FIELD_NUMBER = 4;
//        public abstract bool IDENTIFIER_EXTENSION_BIT { get; }
//        public const bool RESERVED_BIT = false;

//        public const byte MAX_DATA_FIELD = 8;

//        public static ICRCConfig DEFAULT_CRC5_CONFIG = CRCConfig.CRC15;
//        public const ushort CRC_MULTIPLIER = (ushort)(2 ^ 14);
//        public static byte[] CRC_DIVIDER_POLYNOMIAL_POWERS = new byte[] { 15, 14, 10, 8, 7, 4, 3, 0 };
//        public const bool CRC_DELIMITER = true;
//        private const ushort CRC_DELIMITER_ = CRC_DELIMITER ? 1 : 0;
        
//        public const bool ACKNOWLEGDE_DELIMITER = true;

//        public static bool[] END_OF_FRAME = new bool[] { false, false, false, false, false, false, false };

//        public bool[] SendCommand(bool[] arbitration, bool[] command, byte[] data)
//        {
//            if (arbitration.Length != ARBITRATION_FIELD_NUMBER)
//                throw new ArgumentException("");
//            if (command.Length != COMMAND_FIELD_NUMBER)
//                throw new ArgumentException("");
//            if (data.Length != MAX_DATA_FIELD)
//                throw new ArgumentException("");

//            bool[] commandField = new bool[6];
//            commandField[0] = IDENTIFIER_EXTENSION_BIT;
//            commandField[1] = RESERVED_BIT;
//            Array.ConstrainedCopy(command, 0, commandField, 2, 4);

//            bool[] tempResult = new bool[1 + ARBITRATION_FIELD_NUMBER + commandField.Length + (command.Length * 8)];
//            tempResult[0] = START_OF_FRAME;
//            byte tempIndex = 1;
//            arbitration.CopyTo(tempResult, tempIndex);
//            tempIndex += ARBITRATION_FIELD_NUMBER;
//            commandField.CopyTo(tempResult, tempIndex);
//            tempIndex += 2 + COMMAND_FIELD_NUMBER;
//            (new BitArray(data)).CopyTo(tempResult, tempIndex);
//            BigInteger tempsResultBitInteger = 0;
//            (new BitArray(tempResult)).CopyTo(new BigInteger[] { tempsResultBitInteger }, 0);
//            byte[] crcPieces = CRCFactory.Instance.Create(DEFAULT_CRC5_CONFIG).ComputeHash(value).Hash;
//            BigInteger CRC = tempsResultBitInteger * CRC_MULTIPLIER;
//            BigInteger CRCDivider = 0;
//            foreach (byte CRCDividerPower in CRC_DIVIDER_POLYNOMIAL_POWERS)
//                CRCDivider += tempsResultBitInteger ^ CRCDividerPower;
//            CRC /= CRCDivider;
//            CRC = (CRC << 1) & CRC_DELIMITER_;

//            bool acknowlegde = true;
//        }
//    }

//    public class BusA : Bus
//    {
//        public override byte ARBITRATION_FIELD_NUMBER => (byte)11;

//        public override bool IDENTIFIER_EXTENSION_BIT => false;
//    }

//    public class BusB : Bus
//    {
//        public override byte ARBITRATION_FIELD_NUMBER => (byte)26;

//        public override bool IDENTIFIER_EXTENSION_BIT => true;
//    }

//    public class Device
//    {

//    }
//}
