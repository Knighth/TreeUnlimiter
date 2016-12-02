using ICities;
using System;

namespace TreeUnlimiter
{
    public class DataExtension : SerializableDataExtensionBase
    {
        public override void OnLoadData()
        {
            //if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("OnLoadData() fired"); }
        }

        public override void OnSaveData()
        {
            LimitTreeManager.CustomSerializer.Serialize();
        }
    }
}