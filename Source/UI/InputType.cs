using System;

namespace EnhancedDistrictServices
{
    [Flags]
    public enum InputType
    {
        NONE = 0,
        INCOMING = 1,
        OUTGOING = 2,
        SUPPLY_CHAIN = 4
    }
}
