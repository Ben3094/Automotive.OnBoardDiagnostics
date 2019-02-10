using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.HashFunction.CRC;

namespace Automotive.CANBus
{
    public abstract class ISO11898Command
    {
        public ISO11898Command(bool[] arbitration, bool[] command, byte[] data)
        {
            if (arbitration.Length != ARBITRATION_MAX_LENGTH)
                throw new ArgumentException(BAD_LENGTH_ARBITRATION_ARGUMENT_EXCEPTION_MESSAGE);
            if (command.Length != COMMAND_MAX_LENGTH)
                throw new ArgumentException(BAD_LENGTH_COMMAND_ARGUMENT_EXCEPTION_MESSAGE);
            if (data.Length != DATA_MAX_LENGTH)
                throw new ArgumentException(BAD_LENGTH_DATA_ARGUMENT_EXCEPTION_MESSAGE);

            this.Arbitration = arbitration;
            this.Command = command;
            this.Data = data;

            this.CompleteCommandField = new bool[2 + COMMAND_MAX_LENGTH];
            this.CompleteCommandField[0] = IDENTIFIER_EXTENSION_BIT;
            this.CompleteCommandField[1] = RESERVED_BIT;
            this.Command.CopyTo(this.CompleteCommandField, 2);
        }
        public const bool START_OF_FRAME = false;

        /// <remarks>in bits</remarks>
        public abstract byte ARBITRATION_MAX_LENGTH { get; }
        public readonly bool[] Arbitration;

        public abstract bool IDENTIFIER_EXTENSION_BIT { get; }
        public const bool RESERVED_BIT = false;
        /// <remarks>in bits</remarks>
        public const byte COMMAND_MAX_LENGTH = 4;
        public readonly bool[] Command;
        public readonly bool[] CompleteCommandField;

        /// <remarks>in bits</remarks>
        public const byte DATA_MAX_LENGTH = 8;
        public readonly byte[] Data;

        public static ICRCConfig DEFAULT_CRC5_CONFIG = CRCConfig.CRC15;
        public const bool CRC_DELIMITER = true;
        //public ushort ComputeCRC()
        //{
        //    List<bool> temp = new List<bool>() { START_OF_FRAME };

        //    temp.AddRange(this.Arbitration);
            
        //    temp.AddRange(this.CompleteCommandField);
            
        //    bool[] compiledData = new bool[8 * this.Data.Length]);
        //    new BitArray(this.Data).CopyTo(compiledData, 0);

        //    return CRCFactory.Instance.Create(DEFAULT_CRC5_CONFIG).ComputeHash(.Hash;
        //}

        public const bool ACKNOWLEDGE_SLOT_BIT = false;
        public const bool ACKNOWLEGDE_DELIMITER = true;

        public static bool[] END_OF_FRAME = new bool[] { true, true, true, true, true, true, true };

        public const string BAD_LENGTH_BASE_ARGUMENT_EXCEPTION_MESSAGE = "{0} field not suitable length";
        public static string BAD_LENGTH_ARBITRATION_ARGUMENT_EXCEPTION_MESSAGE = String.Format(BAD_LENGTH_BASE_ARGUMENT_EXCEPTION_MESSAGE, nameof(Arbitration));
        public static string BAD_LENGTH_COMMAND_ARGUMENT_EXCEPTION_MESSAGE = String.Format(BAD_LENGTH_BASE_ARGUMENT_EXCEPTION_MESSAGE, nameof(Command));
        public static string BAD_LENGTH_DATA_ARGUMENT_EXCEPTION_MESSAGE = String.Format(BAD_LENGTH_BASE_ARGUMENT_EXCEPTION_MESSAGE, nameof(Data));
    }

    public class ISO11898ACommand : ISO11898Command
    {
        public ISO11898ACommand(bool[] arbitration, bool[] command, byte[] data) : base(arbitration, command, data)
        {
        }

        public override byte ARBITRATION_MAX_LENGTH => (byte)11;

        public override bool IDENTIFIER_EXTENSION_BIT => false;
    }

    public class ISO11898BCommand : ISO11898Command
    {
        public ISO11898BCommand(bool[] arbitration, bool[] command, byte[] data) : base(arbitration, command, data)
        {
        }

        public override byte ARBITRATION_MAX_LENGTH => (byte)26;

        public override bool IDENTIFIER_EXTENSION_BIT => true;
    }
}
