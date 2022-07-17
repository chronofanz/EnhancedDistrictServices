using HarmonyLib;

namespace EnhancedDistrictServices
{
    [HarmonyPatch(typeof(TransferManager))]
    [HarmonyPatch("MatchOffers")]
    public class TransferManagerMatchOffersPatch
    {
        public static bool Prefix(TransferManager.TransferReason material)
        {
            if (TransferManagerMod.MatchOffers(material))
            {
                // If our mod attempted to match the offers, do not run stock code.
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
