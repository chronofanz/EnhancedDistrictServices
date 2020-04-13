using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnhancedDistrictServices.Serialization
{
    [Serializable]
    public class TransferHistoryv1
    {
        public List<TransferHistory.TransferEvent> TransferEvents = new List<TransferHistory.TransferEvent>();

        private static readonly string m_id = "EnhancedDistrictTransferHistory_v1";

        private class TransferHistoryv1Binder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return typeof(TransferHistoryv1);
            }
        }

        public string Id
        {
            get
            {
                return m_id;
            }
        }

        public static bool TryLoadData(EnhancedDistrictTransferHistorySerializableData loader, out TransferHistoryv1 data)
        {
            if (loader.TryLoadData(m_id, new TransferHistoryv1Binder(), out TransferHistoryv1 target))
            {
                if (target != null)
                {
                    data = target;
                    return true;
                }
                else
                {
                    data = null;
                    return false;
                }
            }
            else
            {
                data = null;
                return false;
            }
        }
    }
}
