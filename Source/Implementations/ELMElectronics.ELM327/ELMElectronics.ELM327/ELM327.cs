using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ELMElectronics
{
    public class ELM327
    {
        public ELM327(string portName)
        {
            this.serialPort = new SerialPort(portName, SERIAL_PORT_ALLOWED_BAUD_RATES.First(), SERIAL_PORT_PARITY, SERIAL_PORT_DATA_BITS, SERIAL_PORT_STOP_BITS) { NewLine = SERIAL_PORT_NEW_LINE.ToString() };
        }

        private readonly SerialPort serialPort;
        public int SerialPortBaudRate
        {
            get => this.serialPort.BaudRate;
            set { if (checkBaudRate(value)) this.serialPort.BaudRate = value; }
        }
        public static int[] SERIAL_PORT_ALLOWED_BAUD_RATES = new int[] { 9600, 38400 };
        public const string SERIAL_PORT_FORBIDDEN_BAUD_RATE_ARGUMENT_EXCEPTION_MESSAGE = "Forbidden baud rate";
        private static bool checkBaudRate(int baudRate)
        {
            if (SERIAL_PORT_ALLOWED_BAUD_RATES.Contains(baudRate))
                return true;
            else throw new ArgumentException(SERIAL_PORT_FORBIDDEN_BAUD_RATE_ARGUMENT_EXCEPTION_MESSAGE);
        }
        public const Parity SERIAL_PORT_PARITY = Parity.None;
        public const byte SERIAL_PORT_DATA_BITS = 8;
        public const StopBits SERIAL_PORT_STOP_BITS = StopBits.One;
        public const char SERIAL_PORT_NEW_LINE = '\r';

        public static TimeSpan TIMEOUT_INFINITE = System.Threading.Timeout.InfiniteTimeSpan;

        public const ushort DEFAULT_SERIAL_PORT_WATCHDOG_TIME_MS = 500;
        public static TimeSpan SERIAL_PORT_WATCHDOG_TIME = TimeSpan.FromMilliseconds(DEFAULT_SERIAL_PORT_WATCHDOG_TIME_MS);

        public const string TIMEOUT_EXPIRED_EXCEPTION_MESSAGE = "Timeout expired";
        public const string NOT_ELM327_IO_EXCEPTION_MESSAGE = "Serial port is not ELM327";
        public const string DEFAULT_HANDSHAKE_PATTERN = @"^ELM327 v(\d.\d)";
        public const RegexOptions DEFAULT_REGEX_OPTIONS = RegexOptions.IgnoreCase;
        public static Regex DEFAULT_HANDSHAKE_REGEX = new Regex(DEFAULT_HANDSHAKE_PATTERN, DEFAULT_REGEX_OPTIONS);
        public const string WRONG_EXECUTED_COMMAND_ACCESS_VIOLATION_EXCEPTION_MESSAGE = "Wrong executed command";
        public string SendCommand(string command)
        {
            this.serialPort.ReadExisting(); //Empty the serial port buffer

            string response = "";
            this.serialPort.WriteLine(command);
            if (!Watchdog.Execute(() => response = this.serialPort.ReadExisting(), SERIAL_PORT_WATCHDOG_TIME))
                throw new TimeoutException(TIMEOUT_EXPIRED_EXCEPTION_MESSAGE);

            string[] responseParts = response.Split(SERIAL_PORT_NEW_LINE);
            if (responseParts[0] == command)
            {
                //TODO: wait for idle state if not present
                return responseParts[1];
            }
            else throw new AccessViolationException(WRONG_EXECUTED_COMMAND_ACCESS_VIOLATION_EXCEPTION_MESSAGE);
        }
        public string SendATCommand(string atCommnad)
        {
            return this.SendCommand(AT_COMMAND_PREFIX + atCommnad);
        }

        //TODO: Check idle state before sending a command
        public const char IDLE_STATE_CHARACTER = '>';
        public const char SETTING_COMMAND_FAIL_RESPONSE = '?';
        public const string SETTING_COMMAND_SUCCESS_RESPONSE = "OK";
        public const string DATA_MISFORMED_RESPONSE_FOOTER = "<DATA ERROR";
        public bool SendSettingCommand(string settingCommand)
        {
            return this.SendCommand(settingCommand) == SETTING_COMMAND_SUCCESS_RESPONSE;
        }
        public bool SendSettingATCommand(string settingATCommand)
        {
            return this.SendSettingCommand(AT_COMMAND_PREFIX + settingATCommand);
        }
        public const char TRUE_BOOLEAN_SETTING_CHARACTER = '1';
        public const char FALSE_BOOLEAN_SETTING_CHARACTER = '0';
        public const string MISFORMED_BOOLEAN_SETTING_ARGUMENT_EXCEPTION = "Boolean setting misformed";
        private static bool parseBooleanSetting(char booleanSettingCharacter)
        {
            switch (booleanSettingCharacter)
            {
                case TRUE_BOOLEAN_SETTING_CHARACTER:
                    return true;
                case FALSE_BOOLEAN_SETTING_CHARACTER:
                    return false;
                default:
                    throw new ArgumentException(MISFORMED_BOOLEAN_SETTING_ARGUMENT_EXCEPTION);
            }
        }
        private static char synthesizeBooleanSetting(bool booleanSetting)
        {
            return booleanSetting ? TRUE_BOOLEAN_SETTING_CHARACTER : FALSE_BOOLEAN_SETTING_CHARACTER;
        }


        public const string AT_COMMAND_PREFIX = "AT ";

        private float icVersion;
        public float ICVersion { get => this.icVersion; }

        public void Connect()
        {
            this.serialPort.Open();
            this.identify();
            this.setDeviceBehaviour();
        }
        private void checkHandshake()
        {
            string possibleHandshake = "";

            if (!Watchdog.Execute(() => possibleHandshake = this.serialPort.ReadLine(), SERIAL_PORT_WATCHDOG_TIME))
                throw new TimeoutException(TIMEOUT_EXPIRED_EXCEPTION_MESSAGE);

            Match handshakeCheck = DEFAULT_HANDSHAKE_REGEX.Match(possibleHandshake);
            if (!handshakeCheck.Success) throw new IOException(NOT_ELM327_IO_EXCEPTION_MESSAGE);
            else this.icVersion = float.Parse(handshakeCheck.Groups[0].Captures[0].Value);
        }

        #region Device behaviour
        private void setDeviceBehaviour()
        {
            this.SpaceInHexMessage = false;
        }

        #region Device identifier
        public const string IDENTIFY_AT_COMMAND = "I";
        public void identify()
        {
            string identification = this.SendATCommand(IDENTIFY_AT_COMMAND);
            Match handshakeCheck = DEFAULT_HANDSHAKE_REGEX.Match(identification);
            if (!handshakeCheck.Success) throw new IOException(NOT_ELM327_IO_EXCEPTION_MESSAGE);
            else this.icVersion = float.Parse(handshakeCheck.Groups[0].Captures[0].Value);
        }

        public const char ACCESS_IDENTIFIER_AT_COMMAND_PREFIX = '@';
        public const char DISPLAY_DEVICE_DESCRIPTION_AT_COMMAND_SUFFIX = '1';
        public static string DISPLAY_DEVICE_DESCRIPTION_AT_COMMAND = new string(new char[] { ACCESS_IDENTIFIER_AT_COMMAND_PREFIX, DISPLAY_DEVICE_DESCRIPTION_AT_COMMAND_SUFFIX });
        public string Description { get => this.SendATCommand(DISPLAY_DEVICE_DESCRIPTION_AT_COMMAND); }
        public const char DISPLAY_DEVICE_IDENTIFIER_AT_COMMAND_SUFFIX = '2';
        public static string DISPLAY_DEVICE_IDENTIFIER_AT_COMMAND = new string(new char[] { ACCESS_IDENTIFIER_AT_COMMAND_PREFIX, DISPLAY_DEVICE_IDENTIFIER_AT_COMMAND_SUFFIX });
        public const string DEVICE_IDENTIFIER_NOT_SET_RESPONSE = "?";
        public const char STORE_DEVICE_IDENTIFIER_AT_COMMAND_SUFFIX = '3';
        public static string STORE_DEVICE_IDENTIFIER_AT_COMMAND = new string(new char[] { ACCESS_IDENTIFIER_AT_COMMAND_PREFIX, STORE_DEVICE_IDENTIFIER_AT_COMMAND_SUFFIX });
        public const byte DEVICE_IDENTIFIER_MAX_LENGTH = 12;
        public const string DEVICE_IDENTIFIER_TOO_LONG_ARGUMENT_EXCEPTION_MESSAGE_PATTERN = "Device identifier too long. Maximum length is {0} characters.";
        public static string DEVICE_IDENTIFIER_TOO_LONG_ARGUMENT_EXCEPTION_MESSAGE = String.Format(DEVICE_IDENTIFIER_TOO_LONG_ARGUMENT_EXCEPTION_MESSAGE_PATTERN, DEVICE_IDENTIFIER_MAX_LENGTH);
        public const string DEVICE_IDENTIFIER_NOT_SET_IO_EXCEPTION_MESSAGE = "Device identifier not set";
        public const string READONLY_DEVICE_IDENTIFIER_FIELD_ACCESS_EXCEPTION_MESSAGE = "Device identifier already set, it is now read-only";
        public string Identifier
        {
            get
            {
                string value = this.SendATCommand(DISPLAY_DEVICE_IDENTIFIER_AT_COMMAND);
                if (value == DEVICE_IDENTIFIER_NOT_SET_RESPONSE)
                    return string.Empty;
                else
                    return value;
            }
            set
            {
                if (value.Length > DEVICE_IDENTIFIER_MAX_LENGTH)
                    throw new ArgumentException(DEVICE_IDENTIFIER_TOO_LONG_ARGUMENT_EXCEPTION_MESSAGE);
                if (this.Identifier == string.Empty)
                {
                    if (!this.SendSettingATCommand(STORE_DEVICE_IDENTIFIER_AT_COMMAND + " " + value.PadRight(DEVICE_IDENTIFIER_MAX_LENGTH, ' ')))
                        throw new IOException(DEVICE_IDENTIFIER_NOT_SET_IO_EXCEPTION_MESSAGE);
                }
                else
                    throw new FieldAccessException(READONLY_DEVICE_IDENTIFIER_FIELD_ACCESS_EXCEPTION_MESSAGE);
            }
        }
        #endregion

        #region Reset
        public const string SET_DEFAULT_AT_COMMAND = "D";
        public void SetValueToDefault()
        {
            this.SendATCommand(SET_DEFAULT_AT_COMMAND);
            //TODO: Reset the default values
        }
    
        public const string COLD_RESET_COMMMAND_SUFFIX = "Z";
        public static string COLD_RESET_COMMAND = AT_COMMAND_PREFIX + COLD_RESET_COMMMAND_SUFFIX;
        public void ColdReset()
        {
            this.serialPort.WriteLine(COLD_RESET_COMMAND);
            this.serialPort.Close();
            this.serialPort.Open();
            checkHandshake();
        }

        public const string WARN_RESET_COMMAND_SUFFIX = "WS";
        public static string WARN_RESET_COMMAND = AT_COMMAND_PREFIX + WARN_RESET_COMMAND_SUFFIX;
        public void WarnReset()
        {
            this.serialPort.WriteLine(WARN_RESET_COMMAND);
            checkHandshake();
        }
        #endregion

        #region Message handling behavior
        public const string TOGGLE_SPACES_IN_HEX_MESSAGE_AT_COMMAND = "S";
        public const bool TOGGLE_SPACES_IN_HEX_MESSAGE_DEFAULT_VALUE = true;
        private bool spaceInHexMessage = TOGGLE_SPACES_IN_HEX_MESSAGE_DEFAULT_VALUE;
        public bool SpaceInHexMessage
        {
            get => this.spaceInHexMessage;
            set
            {
                if (this.SendSettingATCommand(TOGGLE_SPACES_IN_HEX_MESSAGE_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.spaceInHexMessage = value;
            }
        }

        public const string TOOGLE_LINEFEED_AT_COMMAND = "L";
        private bool linefeed;
        public bool Linefeed
        {
            get => this.linefeed; //TODO: Check linefeed at first connection
            set
            {
                if (this.SendSettingATCommand(TOOGLE_LINEFEED_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.linefeed = value;
            }
        }
        public const string ECHO_MESSAGE_AT_COMMAND = "E";
        public const bool ECHO_MESSAGE_DEFAULT_VALUE = true;
        private bool echoMessage = ECHO_MESSAGE_DEFAULT_VALUE;
        public bool EchoMessage
        {
            get => this.echoMessage;
            set
            {
                if (this.SendSettingATCommand(ECHO_MESSAGE_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.echoMessage = value;
            }
        }

        public const string TOGGLE_VEHICLE_RESPONSE_AT_COMMAND = "R";
        public const bool TOGGLE_VEHICLE_RESPONSE_DEFAULT_VALUE = true;
        private bool showVehicleResponse = TOGGLE_VEHICLE_RESPONSE_DEFAULT_VALUE;
        public bool ShowVehicleResponse
        {
            get => this.showVehicleResponse;
            set
            {
                if (this.SendSettingATCommand(TOGGLE_VEHICLE_RESPONSE_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.showVehicleResponse = value;
            }
        }
        #endregion

        #region Parameter setting
        public const string SET_PARAMETER_AT_COMMAND = "PP";
        public const string PARAMETER_SET_ACTIVATE_ARGUMENT = "ON";
        public const string PARAMETER_SET_DESACTIVATE_ARGUMENT = "OFF";
        private static string synthesizeParameterToggle(bool value)
        {
            return value ? PARAMETER_SET_ACTIVATE_ARGUMENT : PARAMETER_SET_DESACTIVATE_ARGUMENT;
        }
        public const string PARAMETER_SET_TOGGLE_NOT_SET_IO_EXCEPTION_MESSAGE = "Parameter set toggle not set";
        public void ToggleParameterSetValue(byte parameterAddress, bool value)
        {
            if (!this.SendSettingATCommand(SET_PARAMETER_AT_COMMAND + ' ' + BitConverter.ToString(new byte[] { parameterAddress }) + ' ' + synthesizeParameterToggle(value)))
                throw new IOException(PARAMETER_SET_TOGGLE_NOT_SET_IO_EXCEPTION_MESSAGE);
        }
        public const string SET_PARAMETER_VALUE_AT_COMMAND_SUFFIX = "SV";
        public const string PARAMETER_VALUE_NOT_SET_IO_EXCEPTION_MESSAGE = "Parameter value not set";
        public void SetParameter(byte address, byte value)
        {
            if (!this.SendSettingATCommand(SET_PARAMETER_AT_COMMAND + " " + BitConverter.ToString(new byte[] { address }) + " " + SET_PARAMETER_VALUE_AT_COMMAND_SUFFIX + " " + BitConverter.ToString(new byte[] { value })))
                throw new IOException(PARAMETER_VALUE_NOT_SET_IO_EXCEPTION_MESSAGE);
        }
        public const string SUM_PARAMETER_AT_COMMAND = "PPS";
        public const char PARAMETER_ENABLE_INDICATOR = 'N';
        public const char PARAMETER_DISABLE_INDICATOR = 'F';
        public static string SUM_PARAMETERS_PATTERN = @"([A-F\d]{2}):([A-F\d]{2}) ([" + PARAMETER_ENABLE_INDICATOR + PARAMETER_DISABLE_INDICATOR + "])";
        public static Regex SUM_PARAMETERS_REGEX = new Regex(SUM_PARAMETERS_PATTERN);
        public IEnumerable<Tuple<byte, byte, bool>> SumParameter()
        {
            MatchCollection parametersMatches = SUM_PARAMETERS_REGEX.Matches(this.SendATCommand(SUM_PARAMETER_AT_COMMAND));
            List<Tuple<byte, byte, bool>> parameters = new List<Tuple<byte, byte, bool>>();
            foreach (Match parameterMatch in parametersMatches)
            {
                parameters.Add(new Tuple<byte, byte, bool>
                (
                    byte.Parse(parameterMatch.Groups[0].Value, System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(parameterMatch.Groups[1].Value, System.Globalization.NumberStyles.HexNumber),
                    parameterMatch.Groups[1].Value[0] == PARAMETER_ENABLE_INDICATOR
                ));
            }
            return parameters.ToArray();
        }
        #endregion

        #region Serial communication
        public const string SET_BAUD_RATE_DIVISOR_COMMAND_SUFFIX = "BRD";
        public static string SET_BAUD_RATE_DIVISOR_COMMAND = AT_COMMAND_PREFIX + SET_BAUD_RATE_DIVISOR_COMMAND_SUFFIX;
        //TODO: Implement baud change on the ELM327 with the command BRD
        public const string SET_BAUD_RATE_TIMEOUT_COMMAND_SUFFIX = "BRT";
        public static string SET_BAUD_RATE_TIMEOUT_COMMAND = AT_COMMAND_PREFIX + SET_BAUD_RATE_TIMEOUT_COMMAND_SUFFIX;
        #endregion

        #region Non-volatile data storage
        public const string GET_NONVOLATILE_DATA_AT_COMMAND = "RD";
        public const string SET_NONVOLATILE_DATA_AT_COMMAND = "SD";
        public const string NONVOLATILE_DATA_NOT_SET_IO_EXCEPTION_MESSAGE = "Non-volatile data not set";
        public byte NonVolatileData
        {
            get => byte.Parse(this.SendATCommand(GET_NONVOLATILE_DATA_AT_COMMAND), System.Globalization.NumberStyles.HexNumber);
            set
            {
                if (!this.SendSettingATCommand(SET_NONVOLATILE_DATA_AT_COMMAND + " " + BitConverter.ToString(new byte[] { value })))
                    throw new IOException(NONVOLATILE_DATA_NOT_SET_IO_EXCEPTION_MESSAGE);
            }
        }
        #endregion

        public const string FORGET_EVENT_AT_COMMAND = "FE";
        public void ForgetEvent() { this.SendATCommand(FORGET_EVENT_AT_COMMAND); }

        public const string TOGGLE_PROTOCOL_CHOICE_MEMORY_AT_COMMAND = "M";
        private bool protocolChoiceMemory;
        public bool ProtocolChoiceMemory
        {
            get => this.protocolChoiceMemory; //TODO: Check memory usage at first connection
            set
            {
                if (this.SendSettingATCommand(TOGGLE_PROTOCOL_CHOICE_MEMORY_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.protocolChoiceMemory = value;
            }
        }

        #region Power
        public const string PUT_IN_LOW_POWER_MODE_AT_COMMAND = "LP";
        public const byte SLEEP_MODE_ACTIVATION_TIME_S = 1;
        public static TimeSpan SLEEP_MODE_ACTIVATION_TIME = TimeSpan.FromSeconds(SLEEP_MODE_ACTIVATION_TIME_S);
        public const string SLEEP_MODE_ACTIVATION_ERROR_IO_EXCEPTION = "Impossible to turn on sleep mode: ";
        public void PutInSleepMode()
        {
            this.serialPort.WriteLine(AT_COMMAND_PREFIX + " " + PUT_IN_LOW_POWER_MODE_AT_COMMAND);
            string response = null;
            if (!Watchdog.Execute(() => response = this.serialPort.ReadExisting(), SLEEP_MODE_ACTIVATION_TIME))
                throw new TimeoutException(TIMEOUT_EXPIRED_EXCEPTION_MESSAGE);
            else if (response != SETTING_COMMAND_SUCCESS_RESPONSE)
                throw new IOException(SLEEP_MODE_ACTIVATION_ERROR_IO_EXCEPTION + response);
        }

        public const string MONITOR_IGNITION_AT_COMMAND = "IGN";
        public const string IGNITION_ON_RESPONSE = "ON";
        public const string IGNITION_OFF_RESPONSE = "OFF";
        public const string CANT_PARSE_IGNITION_MONITORING_IOEXCEPTION_MESSAGE = "Can't parse ignition monitoring : ";
        public bool IsIgnite()
        {
            string ignitionStatus = this.SendATCommand(MONITOR_IGNITION_AT_COMMAND);
            if (ignitionStatus.StartsWith(IGNITION_ON_RESPONSE))
                return true;
            else if (ignitionStatus.StartsWith(IGNITION_OFF_RESPONSE))
                return false;
            else throw new IOException(CANT_PARSE_IGNITION_MONITORING_IOEXCEPTION_MESSAGE + ignitionStatus);
        }

        #region Voltage reading
        public const string CALIBRATE_VOLTAGE_AT_COMMAND = "CV";
        public const float CALIBRATE_VOLTAGE_DEFAULT_VALUE = 0; //in volts
        public const float CALIBRATE_VOLTAGE_MIN_VALUE_INCLUSIVE = 0;
        public const float CALIBRATE_VOLTAGE_MAX_VALUE_EXCLUSIVE = 100;
        public const string CALIBRATE_VOLTAGE_FORMAT = "00.00";
        public static string SynthesizeCalibrateVoltageArgument(float calibrateVoltage)
        {
            if ((calibrateVoltage < CALIBRATE_VOLTAGE_MIN_VALUE_INCLUSIVE) && (calibrateVoltage >= CALIBRATE_VOLTAGE_MAX_VALUE_EXCLUSIVE))
                throw new ArgumentException();
            else
                return calibrateVoltage.ToString(CALIBRATE_VOLTAGE_FORMAT).Replace(".", string.Empty);
        }
        private float calibrateVoltage = CALIBRATE_VOLTAGE_DEFAULT_VALUE;
        public float CalibrateVoltage
        {
            get => this.calibrateVoltage;
            set
            {
                if (this.SendSettingATCommand(CALIBRATE_VOLTAGE_AT_COMMAND + ' ' + SynthesizeCalibrateVoltageArgument(value)))
                    this.calibrateVoltage = value;
            }
        }

        public const string READ_INPUT_VOLTAGE_AT_COMMAND = "RV";
        public float InputVoltage
        {
            get => float.Parse(this.SendATCommand(READ_INPUT_VOLTAGE_AT_COMMAND));
        }
        #endregion
        #endregion
        #endregion

        #region Protocol handling
        public const char TOGGLE_LONG_LENGTH_COMMAND_COMMAND_PREFIX = 'A';
        public const char TOGGLE_NORMAL_LENGTH_COMMAND_COMMAND_PREFIX = 'N';
        public const char TOGGLE_LENGTH_COMMAND_COMMAND_SUFFIX = 'L';
        public const bool TOGGLE_LENGTH_COMMAND_DEFAULT_VALUE = true;
        private bool isLongCommand = TOGGLE_LENGTH_COMMAND_DEFAULT_VALUE;
        public bool IsLongCommand
        {
            get => this.isLongCommand;
            set
            {
                string atCommand = new string((value ? TOGGLE_LONG_LENGTH_COMMAND_COMMAND_PREFIX : TOGGLE_NORMAL_LENGTH_COMMAND_COMMAND_PREFIX), TOGGLE_LENGTH_COMMAND_COMMAND_SUFFIX);
                if (this.SendSettingATCommand(atCommand))
                    this.isLongCommand = value;
            }
        }

        public const string DISPLAY_ACTIVITY_MONITOR_COUNT_COMMAND_SUFFIX = "AMC";
        public static string DISPLAY_ACTIVITY_MONITOR_COUNT_COMMAND = AT_COMMAND_PREFIX + DISPLAY_ACTIVITY_MONITOR_COUNT_COMMAND_SUFFIX;
        public const float ACTIVITY_MONITOR_COUNT_PERIOD = 0.665f; //in seconds
        public byte ActivityMonitorCount => Convert.FromBase64String(this.SendCommand(DISPLAY_ACTIVITY_MONITOR_COUNT_COMMAND))[0];
        public float DelaySinceLastCommandExecuted => this.ActivityMonitorCount * ACTIVITY_MONITOR_COUNT_PERIOD;

        public const string SET_ACTIVITY_MONITOR_TIMEOUT_COMMAND_SUFFIX = "AMT";
        public static string SET_ACTIVITY_MONITOR_TIMEOUT_COMMAND = AT_COMMAND_PREFIX + SET_ACTIVITY_MONITOR_TIMEOUT_COMMAND_SUFFIX;
        public const byte ACTIVITY_MONITOR_TIMEOUT_INFINITE_VALUE = 0;
        private byte activityMonitorTimeoutValue = ACTIVITY_MONITOR_TIMEOUT_INFINITE_VALUE;
        public byte ActivityMonitorTimeoutValue
        {
            get => this.activityMonitorTimeoutValue;
            set
            {
                if (this.SendSettingCommand(SET_ACTIVITY_MONITOR_TIMEOUT_COMMAND + ' ' + Convert.ToBase64String(new byte[] { value })))
                    this.activityMonitorTimeoutValue = value;
            }
        }
        public TimeSpan ActivityMonitorTimeout
        {
            get
            {
                if (this.activityMonitorTimeoutValue == ACTIVITY_MONITOR_TIMEOUT_INFINITE_VALUE)
                    return TIMEOUT_INFINITE;
                else
                    return new TimeSpan(0, 0, 0, 0, (int)((this.activityMonitorTimeoutValue + 1) * ACTIVITY_MONITOR_COUNT_PERIOD * 1000));
            }
            set
            {
                if (value == TIMEOUT_INFINITE)
                    this.ActivityMonitorTimeoutValue = 0;
                else
                    this.ActivityMonitorTimeoutValue = (byte)(value.TotalSeconds / ACTIVITY_MONITOR_COUNT_PERIOD);
            }
        }

        public const string AUTO_SET_RECEIVED_ADDRESS_COMMAND_SUFFIX = "AR";
        public static string AUTO_SET_RECEIVED_ADDRESS_COMMAND = AT_COMMAND_PREFIX + AUTO_SET_RECEIVED_ADDRESS_COMMAND_SUFFIX;
        public const bool AUTO_SET_RECEIVED_ADDRESS_DEFAULT_VALUE = true;
        private bool autoReceivedAddress = AUTO_SET_RECEIVED_ADDRESS_DEFAULT_VALUE;
        public bool AutoSetReceivedAddress
        {
            get => this.autoReceivedAddress;
            set
            {
                if (this.SendSettingCommand(AUTO_SET_RECEIVED_ADDRESS_COMMAND + ' ' + synthesizeBooleanSetting(value)))
                    this.autoReceivedAddress = value;
            }
        }

        public const string SET_ADAPTIVE_TIMING_COMMAND_SUFFIX = "AT";
        public static string SET_ADAPTIVE_TIMING_COMMAND = AT_COMMAND_PREFIX + SET_ADAPTIVE_TIMING_COMMAND_SUFFIX;
        public enum AdaptiveTimeout
        {
            None = 0,
            Recommended = 1,
            Aggressive = 2
        }
        public const AdaptiveTimeout ADAPTIVE_TIMING_DEFAULT_VALUE = AdaptiveTimeout.Recommended;
        private AdaptiveTimeout adaptiveTiming = ADAPTIVE_TIMING_DEFAULT_VALUE;
        public AdaptiveTimeout AdaptiveTiming
        {
            get => this.adaptiveTiming;
            set
            {
                if (this.SendSettingCommand(SET_ADAPTIVE_TIMING_COMMAND + (int)value))
                    this.adaptiveTiming = value;
            }
        }

        public const string DUMP_ODB_BUFFER_COMMAND_SUFFIX = "BD";
        public static string DUMP_ODB_BUFFER_COMMAND = AT_COMMAND_PREFIX + DUMP_ODB_BUFFER_COMMAND_SUFFIX;
        //TODO: Implement it using Automotive.CANBus command

        public const string BYPASS_INIT_SEQUENCE_COMMAND_SUFFIX = "BI";
        public static string BYPASS_INIT_SEQUENCE_COMMAND = AT_COMMAND_PREFIX + BYPASS_INIT_SEQUENCE_COMMAND_SUFFIX;
        //TODO: Runs it only when the connection is reset at the serial point

        public enum OBDProtocol
        {
            Automatic = 0x0,
            SAE_J1850_PWM = 0x1,
            SAE_J1850_VPW = 0x2,
            ISO_9141_2 = 0x3,
            ISO_14230_4_KWP_5BaudInit = 0x4,
            ISO_14230_4_KWP_FastInit = 0x5,
            ISO_15765_4_CAN_11BitID_500kbaud = 0x6,
            ISO_15765_4_CAN_29BitID_500kbaud = 0x7,
            ISO_15765_4_CAN_11BitID_250kbaud = 0x8,
            ISO_15765_4_CAN_29BitID_250kbaud = 0x9,
            SAE_J1939_CAN = 0xA,
            User1_CAN = 0xB,
            User2_CAN = 0xC
        }
        public const string GET_PROTOCOL_NAME_AT_COMMAND = "DP";
        public const string GET_PROTOCOL_AT_COMMAND = "DPN";
        public const string GET_PROTOCOL_RESPONSE_PATTERN = @"^(A?)([A-F\d]{1})";
        public static Regex GET_PROTOCOL_RESPONSE_REGEX = new Regex(GET_PROTOCOL_RESPONSE_PATTERN, DEFAULT_REGEX_OPTIONS);
        public const string SET_PROTOCOL_AT_COMMAND = "SP";
        public const string PROTOCOL_NOT_SET_IO_EXCEPTION_MESSAGE = "Protocol not set";
        public OBDProtocol Protocol
        {
            get
            {
                string response = this.SendATCommand(GET_PROTOCOL_AT_COMMAND);
                string protocolString = GET_PROTOCOL_RESPONSE_REGEX.Match(response).Groups[1].Captures[0].Value;
                if (protocolString.Length == 1)
                    protocolString.Insert(0, "0");
                return (OBDProtocol)byte.Parse(protocolString, System.Globalization.NumberStyles.HexNumber);
            }
            set => this.setProtocolWithFallbackAutomaticSearch(value, this.FallbackAutomaticProtocolSearch);
        }
        public bool FallbackAutomaticProtocolSearch
        {
            get
            {
                string response = this.SendATCommand(GET_PROTOCOL_AT_COMMAND);
                return GET_PROTOCOL_RESPONSE_REGEX.Match(response).Groups[0].Success;
            }
            set => this.setProtocolWithFallbackAutomaticSearch(this.Protocol, value);
        }
        private void setProtocolWithFallbackAutomaticSearch(OBDProtocol protocol, bool fallbackAutomaticSearch, string setOrTryCommand = SET_PROTOCOL_AT_COMMAND)
        {
            string command = setOrTryCommand + " ";
            if (fallbackAutomaticSearch)
                command += "A";
            command += BitConverter.ToString(new byte[] { (byte)protocol })[1];
            if (!this.SendSettingATCommand(command))
                throw new IOException(PROTOCOL_NOT_SET_IO_EXCEPTION_MESSAGE);
        }
        public const string PROTOCOL_CLOSE_AT_COMMAND = "PC";
        public void ProtocolClose()
        {
            this.SendSettingATCommand(PROTOCOL_CLOSE_AT_COMMAND);
        }
        public const string FORCE_STANDARD_SEQUENCE_PROTOCOL_SEARCH_AT_COMMAND = "SS";
        public const string FORCE_STANDARD_SEQUENCE_PROTOCOL_SEARCH_IMPOSSIBLE_IO_EXCEPTION_MESSAGE = "Force standard sequesce protocol search impossible";
        public void ForceStandardSequenceProtocolSearch()
        {
            if (!this.SendSettingATCommand(FORCE_STANDARD_SEQUENCE_PROTOCOL_SEARCH_AT_COMMAND))
                throw new IOException(FORCE_STANDARD_SEQUENCE_PROTOCOL_SEARCH_IMPOSSIBLE_IO_EXCEPTION_MESSAGE);
        }
        public const string TRY_PROTOCOL_AT_COMMAND = "TP";
        public void TryProtocolWithFallbackAutomaticSearch(OBDProtocol protocol, bool fallbackAutomaticSearch)
        {
            this.setProtocolWithFallbackAutomaticSearch(protocol, fallbackAutomaticSearch, TRY_PROTOCOL_AT_COMMAND);
        }

        public const string TOGGLE_HEADER_AT_COMMAND = "H";
        public const bool TOGGLE_HEADER_DEFAULT_VALUE = false;
        private bool headerToggled = TOGGLE_HEADER_DEFAULT_VALUE;
        public bool HeaderToggled
        {
            get => this.headerToggled;
            set
            {
                if (this.SendSettingATCommand(TOGGLE_HEADER_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.headerToggled = value;
            }
        }

        public const string SEARCHING_FOR_PROTOCOL_MESSAGE = "SEARCHING";
        public const string PROTOCOL_INIT_MESSAGE = "BUS INIT";

        public const string MONITOR_ALL_MESSAGE_AT_COMMAND = "MA";
        public const string CHANGE_IN_MONITORING_MESSAGE = "SEARCHING...";
        public const string STOP_MONITORING_MESSAGE = "STOPPED";
        private Stack<string> monitorMessage(string monitorTargetMessageATCommand, CancellationToken cancellationToken, byte? filter = null)
        {
            string command = monitorTargetMessageATCommand;
            if (filter != null)
                command += ' ' + BitConverter.ToString(new byte[] { (byte)filter });
            this.SendATCommand(command);

            Stack<string> messages = new Stack<string>();
            Task monitorTargetMessages = new Task(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                    messages.Push(this.serialPort.ReadLine());
            }, cancellationToken);
            return messages;
        }
        public Stack<string> MonitorMessages(CancellationToken cancellationToken)
        {
            return this.monitorMessage(MONITOR_ALL_MESSAGE_AT_COMMAND, cancellationToken);
        }
        public const string MONITOR_TARGET_RECEIVER_MESSAGE_AT_COMMAND = "MR";
        public Stack<string> MonitorTargetReceiverMessage(byte filter, CancellationToken cancellationToken)
        {
            return this.monitorMessage(MONITOR_TARGET_RECEIVER_MESSAGE_AT_COMMAND, cancellationToken, filter);
        }
        public const string MONITOR_TARGET_TRANSMITTER_MESSAGE_AT_COMMAND = "MT";
        public Stack<string> MonitorTargetTransmitterMessage(byte filter, CancellationToken cancellationToken)
        {
            return this.monitorMessage(MONITOR_TARGET_TRANSMITTER_MESSAGE_AT_COMMAND, cancellationToken, filter);
        }

        public const string SET_RECEIVE_ADDRESS_AT_COMMAND = "RA";
        public const string SET_RECEIVE_ADDRESS_FALLBACK_AT_COMMAND = "SR";
        public static byte? RECEIVE_ADDRESS_DEFAULT_VALUE = null;
        private byte? receiveAddress = RECEIVE_ADDRESS_DEFAULT_VALUE;
        public byte? ReceiveAddress
        {
            get => this.receiveAddress;
            set
            {
                string command = SET_RECEIVE_ADDRESS_AT_COMMAND;
                if (value != null)
                    command += " " + BitConverter.ToString(new byte[] { (byte)value });
                if (this.SendSettingATCommand(command))
                    this.receiveAddress = value;
            }
        }

        public const string SET_HEADER_AT_COMMAND = "SH";
        public static BitArray HEADER_DEFAULT_VALUE = null;
        private BitArray header = HEADER_DEFAULT_VALUE;
        public BitArray Header
        {
            get => this.header;
            set
            {
                //if (CAN_ID_LENGTHS.Contains(value.Length))
                //{
                //    if (this.SendSettingATCommand(SET_HEADER_AT_COMMAND + ' ' + value.))
                //        this.header = value;
                //}
                //else throw new ArgumentException(CAN_ID_WRONG_LENGTH_EXCEPTION_MESSAGE);
            }
        }

        public const string SET_TIMEOUT_AT_COMMAND = "ST";
        public const byte TIMEOUT_DEFAULT_VALUE = 32;
        private byte timeoutValue = TIMEOUT_DEFAULT_VALUE;
        public byte TimeoutValue
        {
            get => this.timeoutValue;
            set
            {
                if (this.SendSettingATCommand(SET_TIMEOUT_AT_COMMAND + " " + BitConverter.ToString(new byte[] { value })))
                    this.timeoutValue = value;
            }
        }
        public const byte TIMEOUT_INCREMENT_DEFAULT_VALUE = 4; //in ms
        public const ushort TIMEOUT_MAX_VALUE = byte.MaxValue * TIMEOUT_INCREMENT_DEFAULT_VALUE; //in ms
        public const string TIMEOUT_OVERFLOW_EXCEPTION_MESSAGE_PATTERN = "Timeout overflow. Maximum value is {0} ms.";
        public static string TIMEOUT_OVERFLOW_EXCEPTION_MESSAGE = String.Format(TIMEOUT_OVERFLOW_EXCEPTION_MESSAGE_PATTERN, TIMEOUT_MAX_VALUE);
        public TimeSpan Timeout
        {
            get => new TimeSpan(0, 0, 0, 0, this.TimeoutValue * TIMEOUT_INCREMENT_DEFAULT_VALUE);
            set
            {
                if (value.TotalMilliseconds < TIMEOUT_MAX_VALUE)
                    this.TimeoutValue = (byte)(value.TotalMilliseconds / TIMEOUT_INCREMENT_DEFAULT_VALUE);
                else
                    throw new OverflowException(TIMEOUT_OVERFLOW_EXCEPTION_MESSAGE);
            }
        }

        public const string SET_TESTER_ADDRESS_AT_COMMAND = "TA";
        public const byte TESTER_ADDRESS_DEFAULT_VALUE = 0xF9;
        private byte testerAddress = TESTER_ADDRESS_DEFAULT_VALUE;
        public byte TesterAddress
        {
            get => this.testerAddress;
            set
            {
                if (this.SendSettingATCommand(SET_TESTER_ADDRESS_AT_COMMAND + " " + BitConverter.ToString(new byte[] { value })))
                    this.testerAddress = value;
            }
        }

        #region J1850 protocol
        public const string SAE_J1850_IN_FRAME_RESPONSE_AT_COMMAND_PREFIX = "IFR";
        public enum SAEJ1850InFrameResponse
        {
            None = 0,
            HeaderFirstBytesKBitDetermined = 1,
            Always = 2
        }
        public const byte SAE_J1850_IN_FRAME_RESPONSE_MODE_WHILE_MONITORING_START_INDEX = 4;
        public const bool MONITOR_SAE_J1850_DEFAULT_VALUE = false;
        private bool monitorSAEJ1850 = MONITOR_SAE_J1850_DEFAULT_VALUE;
        public bool MonitorSAEJ1850
        {
            get => this.monitorSAEJ1850;
            set
            {
                if (this.setSAEJ1850InFrameResponseMode(value, this.saeJ1850InFrameResponseMode))
                    this.monitorSAEJ1850 = value;
            }
        }
        public const SAEJ1850InFrameResponse SAE_J1850_IN_FRAME_RESPONSE_MODE = SAEJ1850InFrameResponse.HeaderFirstBytesKBitDetermined;
        private SAEJ1850InFrameResponse saeJ1850InFrameResponseMode = SAE_J1850_IN_FRAME_RESPONSE_MODE;
        public SAEJ1850InFrameResponse SAEJ1850InFrameResponseMode
        {
            get => this.saeJ1850InFrameResponseMode;
            set
            {
                if (this.setSAEJ1850InFrameResponseMode(this.monitorSAEJ1850, value))
                    this.saeJ1850InFrameResponseMode = value;
            }
        }
        private bool setSAEJ1850InFrameResponseMode(bool monitor, SAEJ1850InFrameResponse inFrameResponseMode)
        {
            return this.SendSettingATCommand(SAE_J1850_IN_FRAME_RESPONSE_AT_COMMAND_PREFIX + (((int)inFrameResponseMode) + (monitor ? SAE_J1850_IN_FRAME_RESPONSE_MODE_WHILE_MONITORING_START_INDEX : 0)));
        }
        public const string AUTOMATE_SAE_J1850_IN_FRAME_RESPONSE_SUFFIX = "H";
        public const string NOT_AUTOMATE_SAE_J1850_IN_FRAME_RESPONSE_SUFFIX = "S";
        public const bool AUTOMATE_SAE_J1850_IN_FRAME_RESPONSE_DEFAULT_VALUE = true;
        private bool automateSAEJ1850InFrameResponse = AUTOMATE_SAE_J1850_IN_FRAME_RESPONSE_DEFAULT_VALUE;
        public bool AutomateSAEJ1850InFrameResponse
        {
            get => this.automateSAEJ1850InFrameResponse;
            set
            {
                if (this.SendSettingATCommand(SAE_J1850_IN_FRAME_RESPONSE_AT_COMMAND_PREFIX + " " + (value ? AUTOMATE_SAE_J1850_IN_FRAME_RESPONSE_SUFFIX : NOT_AUTOMATE_SAE_J1850_IN_FRAME_RESPONSE_SUFFIX)))
                    this.automateSAEJ1850InFrameResponse = value;
            }
        }
        #endregion

        #region ISO protocols
        public const string EXECUTE_FAST_INIT_AT_COMMAND = "FI";
        public void ExecuteFastInit() { this.SendATCommand(EXECUTE_FAST_INIT_AT_COMMAND); }

        public const string SET_ISO_9141AND14230_BAUD_RATE_AT_COMMAND = "IB";
        public static ushort[] ISO_9141AND14230_BAUD_RATE_ALLOWED_VALUES = new ushort[] { 4800, 9600, 10400, 12500, 15625 };
        public static ushort ISO_9141AND14230_BAUD_RATE_DEFAULT_VALUE = 10400;
        private ushort iso9141And14230BaudRate = ISO_9141AND14230_BAUD_RATE_DEFAULT_VALUE;
        public ushort ISO9141And14230BaudRate
        {
            get => this.iso9141And14230BaudRate;
            set
            {
                if (ISO_9141AND14230_BAUD_RATE_ALLOWED_VALUES.Contains(value))
                    if (this.SendSettingATCommand(SET_ISO_9141AND14230_BAUD_RATE_AT_COMMAND + value.ToString().Substring(0, 2)))
                        this.iso9141And14230BaudRate = value;
            }
        }

        public const string SET_ISO_9141AND14230_INIT_ADDRESS_AT_COMMAND = "IIA";
        public const byte ISO_9141AND14230_INIT_ADDRESS_DEFAULT_VALUE = 0x33;
        private byte iso9141And14230InitAddress = ISO_9141AND14230_INIT_ADDRESS_DEFAULT_VALUE;
        public byte Iso9141And14230InitAddress
        {
            get => this.iso9141And14230InitAddress;
            set
            {
                if (this.SendSettingATCommand(SET_ISO_9141AND14230_INIT_ADDRESS_AT_COMMAND + " " + Convert.ToBase64String(new byte[] { value })))
                    this.iso9141And14230BaudRate = value;
            }
        }

        public const string DISPLAY_ISO_9141AND14230_KEYWORD_AT_COMMAND = "KW";
        //public byte[] GetISO9141And14230Keyword()
        //{
        //    DISPLAY_ISO_9141AND14230_KEYWORD_AT_COMMAND);
        //}

        public const string TOOGLE_ISO_9141AND14230_KEYWORK_CHECK_AT_COMMAND = "KW";
        public const bool ISO_9141AND14230_KEYWORK_CHECK_DEFAULT_VALUE = true;
        private bool iso9141And14230KeywordCheck = ISO_9141AND14230_KEYWORK_CHECK_DEFAULT_VALUE;
        public bool ISO9141And14230KeywordCheck
        {
            get => this.iso9141And14230KeywordCheck;
            set
            {
                if (this.SendSettingATCommand(TOOGLE_ISO_9141AND14230_KEYWORK_CHECK_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.iso9141And14230KeywordCheck = value;
            }
        }

        public const string PERFORM_SLOW_INITIATION_AT_COMMAND = "SI";
        public void PerformSlowInitiation()
        {
            this.SendATCommand(PERFORM_SLOW_INITIATION_AT_COMMAND);
        }

        public const string SET_VEHICLE_WATCHDOG_AT_COMMAND = "SW";
        public const byte VEHICLE_WATCHDOG_DEFAULT_VALUE = 92;
        private byte vehicleWatchdogValue = VEHICLE_WATCHDOG_DEFAULT_VALUE;
        public byte VehicleWatchdogValue
        {
            get => this.vehicleWatchdogValue;
            set
            {
                if (this.SendSettingATCommand(SET_VEHICLE_WATCHDOG_AT_COMMAND + " " + BitConverter.ToString(new byte[] { value })))
                    this.vehicleWatchdogValue = value;
            }
        }
        public const byte VEHICLE_WATCHDOG_INCREMENT_DEFAULT_VALUE = 20; //in ms
        public const ushort VEHICLE_WATCHDOG_MAX_VALUE = byte.MaxValue * VEHICLE_WATCHDOG_INCREMENT_DEFAULT_VALUE; //in ms
        public const string VEHICLE_WATCHDOG_OVERFLOW_EXCEPTION_MESSAGE_PATTERN = "Vehicle watchdog overflow. Maximum value is {0} ms.";
        public static string VEHICLE_WATCHDOG_OVERFLOW_EXCEPTION_MESSAGE = String.Format(VEHICLE_WATCHDOG_OVERFLOW_EXCEPTION_MESSAGE_PATTERN, VEHICLE_WATCHDOG_MAX_VALUE);
        public TimeSpan VehicleWatchdog
        {
            get
            {
                byte value = this.VehicleWatchdogValue;
                if (value == 0)
                    return TIMEOUT_INFINITE;
                else
                    return new TimeSpan(0, 0, 0, 0, this.TimeoutValue * VEHICLE_WATCHDOG_INCREMENT_DEFAULT_VALUE);
            }
            set
            {
                if (value == TIMEOUT_INFINITE)
                    this.TimeoutValue = 0;
                else if (value.TotalMilliseconds < VEHICLE_WATCHDOG_MAX_VALUE)
                    this.TimeoutValue = (byte)(value.TotalMilliseconds / VEHICLE_WATCHDOG_INCREMENT_DEFAULT_VALUE);
                else
                    throw new OverflowException(VEHICLE_WATCHDOG_OVERFLOW_EXCEPTION_MESSAGE);
            }
        }

        public const string SET_WATCHDOG_MESSAGE_AT_COMMAND = "WM";
        private byte[] watchdogMessage = null;
        public byte[] WatchdogMessage
        {
            get => this.watchdogMessage;
            set
            {
                if (this.SendSettingATCommand(SET_WATCHDOG_MESSAGE_AT_COMMAND + " " + BitConverter.ToString(value).Replace('-', ' ')))
                    this.watchdogMessage = value;
            }
        }
        #endregion

        #region CAN protocol
        public const string SET_CAN_AUTO_FORMATTING_COMMAND_SUFFIX = "CAF";
        public static string SET_CAN_AUTO_FORMATTING_COMMAND = AT_COMMAND_PREFIX + SET_CAN_AUTO_FORMATTING_COMMAND_SUFFIX;
        public const bool CAN_AUTO_FORMATTING_DEFAULT_VALUE = true;
        private bool canAutoFormatting = CAN_AUTO_FORMATTING_DEFAULT_VALUE;
        public bool CANAutoFormatting
        {
            get => this.canAutoFormatting;
            set
            {
                if (this.SendSettingCommand(SET_CAN_AUTO_FORMATTING_COMMAND + synthesizeBooleanSetting(value)))
                    this.canAutoFormatting = value;
            }
        }

        public const string SET_CAN_EXTENDED_ADDRESS_COMMAND_SUFFIX = "CEA";
        public static string SET_CAN_EXTENDED_ADDRESS_COMMAND = AT_COMMAND_PREFIX + SET_CAN_EXTENDED_ADDRESS_COMMAND_SUFFIX;

        public const string SET_CAN_EXTENDED_RX_ADDRESS_COMMAND_SUFFIX = "CER";
        public static string SET_CAN_EXTENDED_RX_ADDRESS_COMMAND = AT_COMMAND_PREFIX + SET_CAN_EXTENDED_RX_ADDRESS_COMMAND_SUFFIX;

        public const string WRONG_BITS_LENGTH_EXCEPTION_MESSAGE = "Must be {0} bits long";
        public static int[] CAN_ID_LENGTHS = new int[] { 11, 29 };
        public static string CAN_ID_WRONG_LENGTH_EXCEPTION_MESSAGE_PIECE_FORMAT = "{0} or {1}";
        public static string CAN_ID_WRONG_LENGTH_EXCEPTION_MESSAGE_PIECE = String.Format(CAN_ID_WRONG_LENGTH_EXCEPTION_MESSAGE_PIECE_FORMAT, CAN_ID_LENGTHS[0], CAN_ID_LENGTHS[1]);
        public static string CAN_ID_WRONG_LENGTH_EXCEPTION_MESSAGE = String.Format(WRONG_BITS_LENGTH_EXCEPTION_MESSAGE, CAN_ID_WRONG_LENGTH_EXCEPTION_MESSAGE_PIECE);
        public static BitArray CAN_ID_DEFAULT_VALUE = new BitArray(0);

        public const string SET_CAN_ID_FILTER_COMMAND_SUFFIX = "CF";
        public static string SET_CAN_ID_FILTER_COMMAND = AT_COMMAND_PREFIX + SET_CAN_ID_FILTER_COMMAND_SUFFIX;
        private BitArray canIDFilter = CAN_ID_DEFAULT_VALUE;
        //public BitArray CANIDFilter
        //{
        //    get => this.canIDFilter;
        //    set
        //    {
        //        if (CAN_ID_LENGTHS.Contains(value.Length))
        //        {
        //            if (this.SendSettingCommand(SET_CAN_ID_FILTER_COMMAND_SUFFIX + ' ' + value.))
        //                this.canIDFilter = value;
        //        }
        //        else throw new ArgumentException(CAN_ID_WRONG_LENGTH_EXCEPTION_MESSAGE);
        //    }
        //}

        public const string SET_CAN_FLOW_CONTROL_COMMAND_SUFFIX = "CFC";
        public static string SET_CAN_FLOW_CONTROL_COMMAND = AT_COMMAND_PREFIX + SET_CAN_FLOW_CONTROL_COMMAND_SUFFIX;
        public const bool CAN_FLOW_CONTROL_DEFAULT_VALUE = true;
        private bool canFlowControl = CAN_FLOW_CONTROL_DEFAULT_VALUE;
        public bool CANFlowControl
        {
            get => this.canFlowControl;
            set
            {
                if (this.SendSettingCommand(SET_CAN_FLOW_CONTROL_COMMAND_SUFFIX + synthesizeBooleanSetting(value)))
                    this.canFlowControl = value;
            }
        }

        //public const string SET_CAN_ID_MASK_COMMAND_SUFFIX = "CM";
        //public static string SET_CAN_ID_MASK_COMMAND = AT_COMMAND_PREFIX + SET_CAN_ID_MASK_COMMAND_SUFFIX;
        //public static BitArray DEFAULT_CAN_ID_MASK = new BitArray(0);
        //private BitArray canIDMask = DEFAULT_CAN_ID_MASK;
        //public BitArray CANIDMask
        //{
        //    get => this.canIDMask;
        //    set
        //    {
        //        if (CAN_ID_LENGTHS.Contains(value.Length))
        //        {
        //            if (this.SendSettingCommand(SET_CAN_ID_MASK_COMMAND_SUFFIX + ' ' + value.))
        //                this.canIDMask = value;
        //        }
        //        else throw new ArgumentException(CAN_ID_WRONG_LENGTH_EXCEPTION_MESSAGE);
        //    }
        //}

        public const string SET_CAN_PRIORITY_BITS_COMMAND_SUFFIX = "CP";
        public static string SET_CAN_PRIORITY_BITS_COMMAND = AT_COMMAND_PREFIX + SET_CAN_PRIORITY_BITS_COMMAND_SUFFIX;
        public const byte CAN_PRIORITY_BITS_LENGTH = 5;
        public static string CAN_PRIORITY_BITS_LENGTH_EXCEPTION_MESSAGE = String.Format(WRONG_BITS_LENGTH_EXCEPTION_MESSAGE, CAN_PRIORITY_BITS_LENGTH);
        public const byte DEFAULT_CAN_PRIORITY_BITS = 0x18;
        public const byte CAN_PRIORITY_BITS_MAX_VALUE = 0b00011111;
        private byte canPriorityBits = DEFAULT_CAN_PRIORITY_BITS;
        public byte CANPriorityBits
        {
            get => this.canPriorityBits;
            set
            {
                if (value <= CAN_PRIORITY_BITS_MAX_VALUE)
                {
                    if (this.SendSettingCommand(SET_CAN_PRIORITY_BITS_COMMAND + ' ' + Convert.ToBase64String(new byte[] { value })))
                        this.canPriorityBits = value;
                }
                else throw new ArgumentException(CAN_PRIORITY_BITS_LENGTH_EXCEPTION_MESSAGE);
            }
        }

        public const string SET_CAN_RX_ADDRESS_COMMAND_SUFFIX = "CRA";
        public static string SET_CAN_RX_ADDRESS_COMMAND = AT_COMMAND_PREFIX + SET_CAN_RX_ADDRESS_COMMAND_SUFFIX;

        public const string GET_CAN_STATUS_COUNTS_COMMAND_SUFFIX = "CS";
        public static string GET_CAN_STATUS_COUNTS_COMMAND = AT_COMMAND_PREFIX + GET_CAN_STATUS_COUNTS_COMMAND_SUFFIX;
        public const string CAN_STATUS_COUNTS_PATTERN = @"^T:(\d+) R:(\d+) F:([<>]?\d+)";
        public static Regex CAN_STATUS_COUNTS_REGEX = new Regex(CAN_STATUS_COUNTS_PATTERN);
        private void GetCANStatusCounts()
        {
            Match canStatusCounts = CAN_STATUS_COUNTS_REGEX.Match(this.SendCommand(GET_CAN_STATUS_COUNTS_COMMAND));
            this.canTxError = Convert.FromBase64String(canStatusCounts.Groups[0].Captures[0].Value)[0];
            this.canRxError = Convert.FromBase64String(canStatusCounts.Groups[1].Captures[0].Value)[0];
            this.canFrequency = canStatusCounts.Groups[2].Captures[0].Value;
        }
        private byte canTxError = 0;
        public byte CANTxError
        {
            get
            {
                this.GetCANStatusCounts();
                return this.canTxError;
            }
        }
        private byte canRxError = 0;
        public byte CANRxError
        {
            get
            {
                this.GetCANStatusCounts();
                return this.canRxError;
            }
        }
        public const string CAN_FREQUENCY_NO_SIGNAL = "No signal";
        private string canFrequency = CAN_FREQUENCY_NO_SIGNAL;
        public string CANFrequency
        {
            get
            {
                this.GetCANStatusCounts();
                return this.canFrequency;
            }
        }

        public const string SET_CAN_SILENT_MONITORING_COMMAND_SUFFIX = "CSM";
        public static string SET_CAN_SILENT_MONITORING_COMMAND = AT_COMMAND_PREFIX + SET_CAN_SILENT_MONITORING_COMMAND_SUFFIX;
        public const bool CAN_SILENT_MONITORING_DEFAULT_VALUE = true;
        private bool canSilentMonitoring = CAN_SILENT_MONITORING_DEFAULT_VALUE;
        public bool CANSilentMonitoring
        {
            get => this.canSilentMonitoring;
            set
            {
                if (this.SendSettingCommand(SET_CAN_SILENT_MONITORING_COMMAND + synthesizeBooleanSetting(value)))
                    this.canSilentMonitoring = value;
            }
        }

        public const string MULTIPLY_CAN_TIMING_BY_5_AT_COMMAND_PREFIX = "CTM";
        public const char MULTIPLY_CAN_TIMING_BY_5_OFF_VALUE_SUFFIX = '1';
        public const char MULTIPLY_CAN_TIMING_BY_5_ON_VALUE_SUFFIX = '5';
        public const bool MULTIPLY_CAN_TIMING_BY_5_DEFAULT_VALUE = false;
        private bool multiplierCANTimingBy5 = MULTIPLY_CAN_TIMING_BY_5_DEFAULT_VALUE;
        public bool MultiplierCANTimingBy5
        {
            get => this.multiplierCANTimingBy5;
            set
            {
                if (this.SendSettingATCommand(MULTIPLY_CAN_TIMING_BY_5_AT_COMMAND_PREFIX + (value ? MULTIPLY_CAN_TIMING_BY_5_ON_VALUE_SUFFIX : MULTIPLY_CAN_TIMING_BY_5_OFF_VALUE_SUFFIX)))
                    this.multiplierCANTimingBy5 = value;
            }
        }

        public const string DISPLAY_LENGTH_COUNT_AT_COMMAND = "D";
        public const byte DISPLAY_LENGTH_COUNT_DEFAULT_VALUE_ADDRESS = 0x29;
        private bool displayLengthCount;
        public bool DisplayLegnthCount
        {
            get => this.displayLengthCount;
            set
            {
                if (this.SendSettingATCommand(DISPLAY_LENGTH_COUNT_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.displayLengthCount = value;
            }
        }

        public const string FLOW_CONTROL_AT_COMMAND_HEADER = "FC";
        public const string FLOW_CONTROL_DATA_AT_COMMAND_SUFFIX = "SD";
        public static string FLOW_CONTROL_DATA_AT_COMMAND = FLOW_CONTROL_AT_COMMAND_HEADER + " " + FLOW_CONTROL_DATA_AT_COMMAND_SUFFIX;
        public static byte[] FLOW_CONTROL_DATA_DEFAULT_VALUE = new byte[] { };
        private byte[] flowControlData = FLOW_CONTROL_DATA_DEFAULT_VALUE;
        public byte[] FlowControlData
        {
            get => this.flowControlData;
            set
            {
                if (this.SendSettingATCommand(FLOW_CONTROL_DATA_AT_COMMAND + " " + Convert.ToBase64String(value).Replace('-', ' ')))
                    this.flowControlData = value;
            }
        }
        public const string FLOW_CONTROL_HEADER_AT_COMMAND_SUFFIX = "SH";
        public static string FLOW_CONTROL_HEADER_AT_COMMAND = FLOW_CONTROL_AT_COMMAND_HEADER + " " + FLOW_CONTROL_HEADER_AT_COMMAND_SUFFIX;
        private BitArray controlFilterHeader = CAN_ID_DEFAULT_VALUE;
        //public BitArray ControlFilterHeader
        //{
        //    get => this.controlFilterHeader;
        //    set
        //    {
        //        if (CAN_ID_LENGTHS.Contains(value.Length))
        //        {
        //            if (this.SendSettingCommand(SET_CAN_ID_FILTER_COMMAND_SUFFIX + ' ' + value.))
        //                this.controlFilterHeader = value;
        //        }
        //        else throw new ArgumentException(CAN_ID_WRONG_LENGTH_EXCEPTION_MESSAGE);
        //    }
        //}
        public const string FLOW_CONTROL_MODE_AT_COMMAND_SUFFIX = "SM";
        public static string FLOW_CONTROL_MODE_AT_COMMAND = FLOW_CONTROL_AT_COMMAND_HEADER + " " + FLOW_CONTROL_MODE_AT_COMMAND_SUFFIX;
        public enum FlowControlMode
        {
            Automatic = 0,
            Manual = 1,
            Preset = 2
        }
        public const FlowControlMode FLOW_CONTROL_MODE_DEFAULT_VALUE = FlowControlMode.Automatic;
        private FlowControlMode setFlowControlMode = FLOW_CONTROL_MODE_DEFAULT_VALUE;
        public FlowControlMode SetFlowControlMode
        {
            get => this.setFlowControlMode;
            set
            {
                if (this.SendSettingATCommand(FLOW_CONTROL_MODE_AT_COMMAND + " " + (int)value))
                    this.setFlowControlMode = value;
            }
        }

        public const string SET_VOLATILE_PARAMETER_AT_COMMAND = "PB";
        public const string VOLATILE_PARAMETER_NOT_SET_IO_EXCEPTION_MESSAGE = "Volatile parameter not set";
        public void SetVolatileParameter(byte parameterAddress, byte parameterValue)
        {
            if (!this.SendSettingATCommand(SET_VOLATILE_PARAMETER_AT_COMMAND + ' ' + BitConverter.ToString(new byte[] { parameterAddress }) + ' ' + BitConverter.ToString(new byte[] { parameterValue })))
                throw new IOException(VOLATILE_PARAMETER_NOT_SET_IO_EXCEPTION_MESSAGE);
        }

        public const string SEND_REMOTE_TRANSMISSION_REQUEST_AT_COMMAND = "RTR";
        //TODO: Change string to correct type
        public string RequestRemoteFrame()
        {
            return this.SendATCommand(SEND_REMOTE_TRANSMISSION_REQUEST_AT_COMMAND);
        }

        public const string TOGGLE_CAN_MESSAGE_VARIABLE_LENGTH_AT_COMMAND = "V";
        public const bool CAN_MESSAGE_VARIABLE_LENGTH_DEFAULT_VALUE = false;
        private bool canMessageVariableLength = CAN_MESSAGE_VARIABLE_LENGTH_DEFAULT_VALUE;
        public bool CanMessageVariableLength
        {
            get => this.canMessageVariableLength;
            set
            {
                if (this.SendSettingATCommand(TOGGLE_CAN_MESSAGE_VARIABLE_LENGTH_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.canMessageVariableLength = value;
            }
        }
        #endregion

        #region J1939 protocol
        public const string ACTIVATE_SAE_J1939_DIAGNOSTIC_MODE_1_AT_COMMAND = "DM1";
        public void ActivateSAEJ1939DiagnosticMode1() { this.SendATCommand(ACTIVATE_SAE_J1939_DIAGNOSTIC_MODE_1_AT_COMMAND); }


        public const string REVERSE_J1939_QUERY_DATA_AT_COMMAND_PREFIX = "J";
        public const string ENABLE_REVERSING_J1939_QUERY_DATA_AT_COMMAND_SUFFIX = "E";
        public const string DISABLE_REVERSING_J1939_QUERY_AT_COMMAND_SUFFIX = "S";
        public const bool REVERSE_J1939_QUERY_DATA_DEFAULT_VALUE = true;
        private bool reverseJ1939QueryData = REVERSE_J1939_QUERY_DATA_DEFAULT_VALUE;
        public bool ReverseJ1939QueryData
        {
            get => this.reverseJ1939QueryData;
            set
            {
                if (this.SendSettingATCommand(REVERSE_J1939_QUERY_DATA_AT_COMMAND_PREFIX + (value ? ENABLE_REVERSING_J1939_QUERY_DATA_AT_COMMAND_SUFFIX : DISABLE_REVERSING_J1939_QUERY_AT_COMMAND_SUFFIX)))
                    this.reverseJ1939QueryData = value;
            }
        }

        public const string FORMAT_J1939_RESPONSE_HEADER_AT_COMMAND = "JHF";
        public const bool FORMAT_J1939_RESPONSE_HEADER_DEFAULT_VALUE = true;
        private bool formatJ1939ReponseHeader = REVERSE_J1939_QUERY_DATA_DEFAULT_VALUE;
        public bool FormatJ1939ReponseHeader
        {
            get => this.formatJ1939ReponseHeader;
            set
            {
                if (this.SendSettingATCommand(FORMAT_J1939_RESPONSE_HEADER_AT_COMMAND + synthesizeBooleanSetting(value)))
                    this.formatJ1939ReponseHeader = value;
            }
        }

        public const string MULTIPLY_SAE_J1939_TIMER_BY_5_AT_COMMAND_PREFIX = "JTM";
        public const string MULTIPLY_SAE_J1939_TIMER_BY_5_AT_COMMAND_OFF_SUFFIX = "1";
        public const string MULTIPLY_SAE_J1939_TIMER_BY_5_AT_COMMAND_ON_SUFFIX = "5";
        public const bool MULTIPLY_SAE_J1939_TIMER_BY_5_DEFAULT_VALUE = false;
        private bool multiplySAEJ1939TimerBy5 = MULTIPLY_SAE_J1939_TIMER_BY_5_DEFAULT_VALUE;
        [Obsolete("4.1")]
        public bool MultiplySAEJ1939TimerBy5
        {
            get => this.multiplySAEJ1939TimerBy5;
            set
            {
                if (this.SendSettingATCommand(MULTIPLY_SAE_J1939_TIMER_BY_5_AT_COMMAND_PREFIX + (value ? MULTIPLY_SAE_J1939_TIMER_BY_5_AT_COMMAND_ON_SUFFIX : MULTIPLY_SAE_J1939_TIMER_BY_5_AT_COMMAND_ON_SUFFIX)))
                    this.multiplySAEJ1939TimerBy5 = value;
            }
        }

        public const string APPLY_MONITORING_SAE_J1939_PARAMETER_GROUP_NUMBERS_FILTER_AT_COMMAND = "MP";
        public const uint SAE_J1939_PARAMETER_GROUP_NUMBERS_FILTER_MAX_VALUE = 0xFFFFFF;
        public const string SAE_J1939_PARAMETER_GROUP_NUMBERS_FILTER_OVERFLOW_ARGUMENT_EXCEPTION_MESSAGE_FORMAT = "Monitoring SAE J1939 Parameter Group Numbers filter should be lower than {0}";
        public static string SAE_J1939_PARAMETER_GROUP_NUMBERS_FILTER_OVERFLOW_ARGUMENT_EXCEPTION_MESSAGE = String.Format(SAE_J1939_PARAMETER_GROUP_NUMBERS_FILTER_OVERFLOW_ARGUMENT_EXCEPTION_MESSAGE_FORMAT, SAE_J1939_PARAMETER_GROUP_NUMBERS_FILTER_MAX_VALUE);
        public const byte MONITORING_SAE_J1939_EXPECTED_MESSAGE_NUMBER_MAX_VALUE = 0xF;
        public const string MONITORING_SAE_J1939_EXPECTED_MESSAGE_NUMBER_OVERFLOW_ARGUMENT_EXCEPTION_MESSAGE_FORMAT = "Expected message number should be lower than {0}";
        public static string MONITORING_SAE_J1939_EXPECTED_MESSAGE_NUMBER_OVERFLOW_ARGUMENT_EXCEPTION_MESSAGE = String.Format(MONITORING_SAE_J1939_EXPECTED_MESSAGE_NUMBER_OVERFLOW_ARGUMENT_EXCEPTION_MESSAGE_FORMAT, MONITORING_SAE_J1939_EXPECTED_MESSAGE_NUMBER_MAX_VALUE);
        private bool applyMonitoringSAEJ1939ParameterGroupNumbersFilter(uint filter, byte? expectedMessageNumber = null)
        {

            if (filter < SAE_J1939_PARAMETER_GROUP_NUMBERS_FILTER_MAX_VALUE)
            {
                string command = APPLY_MONITORING_SAE_J1939_PARAMETER_GROUP_NUMBERS_FILTER_AT_COMMAND
                           + " "
                           + BitConverter.ToString(BitConverter.GetBytes((uint)filter)).Replace("-", string.Empty);
                if (expectedMessageNumber != null)
                {
                    if (expectedMessageNumber < MONITORING_SAE_J1939_EXPECTED_MESSAGE_NUMBER_MAX_VALUE)
                        command += String.Format(" {0:x1}", expectedMessageNumber);
                    else throw new ArgumentException(MONITORING_SAE_J1939_EXPECTED_MESSAGE_NUMBER_OVERFLOW_ARGUMENT_EXCEPTION_MESSAGE);
                }
                return this.SendSettingATCommand(command);
            }
            else throw new ArgumentException(SAE_J1939_PARAMETER_GROUP_NUMBERS_FILTER_OVERFLOW_ARGUMENT_EXCEPTION_MESSAGE);
        }
        private uint? monitoringSAEJ1939ParameterGroupNumbersFilter = null;
        public uint? MonitoringParameterGroupNumbersFilterSAEJ1939
        {
            get => this.monitoringSAEJ1939ParameterGroupNumbersFilter;
            set
            {
                //if (value == null)
                //TODO: handle monitor filter reset
                if (this.applyMonitoringSAEJ1939ParameterGroupNumbersFilter((uint)value))
                    this.monitoringSAEJ1939ParameterGroupNumbersFilter = value;
            }
        }
        //TODO: replace string by parsed SAE J1939 message
        public Stack<string> MonitorSAEJ1939Message(uint parameterGroupNumbersFilter, byte expectedMessagesNumber, CancellationToken cancellationToken)
        {
            this.applyMonitoringSAEJ1939ParameterGroupNumbersFilter(parameterGroupNumbersFilter, expectedMessagesNumber);

            Stack<string> messages = new Stack<string>();
            Task monitorSAEJ1939Messages = new Task(() =>
            {
                for (byte remainingMessagesNumber = expectedMessagesNumber; remainingMessagesNumber > 0; remainingMessagesNumber++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    messages.Push(this.serialPort.ReadLine());
                }
            }, cancellationToken);
            return messages;
        }
        #endregion
        #endregion
    }
}