using System;
using System.Collections.Generic;
using System.Text;

namespace Automotive.CANBus
{
    public class EODB : ODB
    {
        public enum Family
        {
            Powertrain = 'P',
            Chassis = 'C',
            Body = 'B',
            UserNetwork = 'U'
        }

        public enum PowertrainType
        {
            Generic0 = 0,
            ManufacturerDefined0 = 1,
            Generic1 = 2
        }

        public enum ChassisType
        {
            Generic0 = 0,
            ManufacturerDefined0 = 1,
            ManufacturerDefined1 = 2,
            Generic1 = 3
        }

        public enum BodyType
        {
            Generic0 = 0,
            ManufacturerDefined0 = 1,
            ManufacturerDefined1 = 2,
            Generic1 = 3
        }

        public enum UserNetworkType
        {
            Generic0 = 0,
            ManufacturerDefined0 = 1,
            ManufacturerDefined1 = 2,
            Generic1 = 3
        }

        public enum PowertrainGeneric0Fault
        {
            Fuel_AirMetering_AuxEmissionControls = 0,
            Fuel_AirMetering = 1,
            InjectionCircuit = 2,
            IgnitionSystem_Misfire = 3,
            AuxEmissionsControls = 4,
            VehiculeSpeedControls_IdleControlSys = 5,
            ComputerOutpurCircuit = 6,
            Transmission0 = 7,
            Transmission1 = 8,
            Transmission2 = 9,
            HybridPropulsion0 = 0xA,
            HybridPropulsion1 = 0xB,
            HybridPropulsion2 = 0xC
        }
    }
}
