using System;
using System.Collections.Generic;
using System.Text;

namespace Automotive.CANBus
{
    public class ODB
    {
        public enum PowerSource
        {
            NotAvailable = 0,
            Gasoline = 1,
            Methanol = 2,
            Ethanol = 3,
            Diesel = 4,
            LPG = 5,
            CNG = 6,
            Propane = 7,
            Electric = 8,
            BifuelGasoline = 9,
            BifuelMethanol = 10,
            BifuelEthanol = 11,
            BifuelLPG = 12,
            BifuelCNG = 13,
            BifuelPropane = 14,
            BifuelElectric = 15,
            BifuelElectricCombustion = 16,
            HybridGasoline = 17,
            HybridEthanol = 18,
            HybridDiesel = 19,
            HybridElectric = 20,
            HybridElectricCombustion = 21,
            HybridRegenerative = 22,
            BifuelDiesel = 23
        }
        public enum Type
        {
            OBD2 = 1,
            OBD = 2,
            OBD_OBD2 = 3,
            OBD1 = 4,
            NotOBDCompliant = 5,
            EOBD = 6, //European ODB
            EOBD_OBD2 = 7,
            EOBD_OBD = 8,
            EOBD_OBD_OBD2 = 9,
            JOBD = 10, //Japan ODB
            JOBD_OBD2 = 11,
            JOBD_EOBD = 12,
            JOBD_EOBD_OBD2 = 13,
            EMD = 17, //Engine Manufacturer Diagnostics
            EMD_Plus = 18, //EMD Enhanced
            HD_OBD = 20, //Heavy Duty On-Board Diagnostics
            HD_ODB_C = 19, //HD_ODB Child
            WWH_OBD = 21, //World Wide Harmonized OBD
            HD_EOBD1 = 23, //Heavy Duty Euro OBD Stage I
            HD_EOBD1_N = 24, //HD_EOBD1 with NOx control
            HD_EOBD2 = 25, //HD_EOBD Stage II
            HD_EOBD2_N = 26, //HD_EOBD2 with NOx control
            HD_EOBD4 = 33, //Heavy Duty Euro OBD Stage VI
            OBDBr1 = 28, //Brazil OBD Phase 1
            OBDBr2 = 29, //OBDBr Phase 2
            KOBD = 30, //Korean OBD
            IOBD1 = 31, //India OBD I
            IOBD2 = 32, //IODB II
        }
    }
}
