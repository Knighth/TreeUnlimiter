using ICities;
using System;

namespace TreeUnlimiter
{
    public class DataExtension : SerializableDataExtensionBase
    {
        public static ISerializableData _serializableData;
        public static UTSaveDataContainer m_UTSaveDataContainer;


        //This get fired once per app 'Session'
        //It does not fire once per each map load unless you exit to mainmenu.
        public override void OnCreated(ISerializableData serializedData) 
        {
            if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("Oncreated() fired  " + DateTime.Now.ToString(Mod.DTMilli)); }
            try
            {
                DataExtension._serializableData = serializedData;
            }
            catch (Exception ex)
            { Logger.dbgLog(ex.ToString()); }
        }

        //This runs way late in the process Post Data.Deseralize() and Post Data.AfterDeserialize()
        //It basically doesn't get called till SimManager.LateUpdate()  gets called which is after(I think) LateUpdate()
        //has been called on all the managers.Effectively it's onLevelLoaded for DataExtentions Class
        public override void OnLoadData()
        {
            if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("OnLoadData() fired"); }
            try
            {
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                {
                    SaveDataUtils.ListDataKeysToLog();
                }
            }
            catch (Exception ex)
            { Logger.dbgLog(ex.ToString()); }

        }

        //This actually fires just prior to Data.Serialize calls and prior to SerializedStorageDictionary being saved.
        public override void OnSaveData()
        {
            try
            {
                if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("\r\n  OnSaveData() fired  " + DateTime.Now.ToString(Mod.DTMilli)); }
                LimitTreeManager.CustomSerializer.Serialize();
            }
            catch (Exception ex1)
            {
                Logger.dbgLog("custom tree data exception bubbled up: ", ex1, true);
            }
            try
            {
                LimitTreeManager.CustomSerializer.SerializeBurningTreeWrapper();
            }
            catch (Exception ex)
            {
                Logger.dbgLog("custom burning tree data exceptions bubbled up: ", ex, true);
            }

            if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("OnSaveData() Completely finished saving custom data." + DateTime.Now.ToString(Mod.DTMilli)); }
        }

        public override void OnReleased()
        {
            _serializableData = null;
            m_UTSaveDataContainer = null;
        }

    }
}