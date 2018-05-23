using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using TreeUnlimiter.OptionsFramework;
using UnityEngine;


namespace TreeUnlimiter
{
	public class UTSettingsUI
	{
	    public static UILabel MaxTreeLabel { get; set; }

        public static void UpdateMaxTreesLabel(float val)
        {
            if (MaxTreeLabel != null)
            {
                MaxTreeLabel.text = $"Maximum trees: {OptionsWrapper<Configuration>.Options.GetScaledTreeCount()}";
            }
        }

        public static void OnDumpAllBurningTrees()
        {
            try
            {
                UTDebugUtils.DumpBurningTrees();
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }
        }


        public static void ClearAllBurningDamaged()
        {
            try
            {
                UTDebugUtils.ResetAllBurningTrees(true);
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }
        }

        public static void ClearAllSaveDataFromFile()
        {
            try
            {
                SaveDataUtils.EraseBytesFromNamedKey(Mod.MOD_OrgDataKEYNAME); // "mabako/unlimiter" 
                SaveDataUtils.EraseBytesFromNamedKey(UTSaveDataContainer.DefaultContainername); //"KH_UnlimitedTrees_v1_0"
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }
        }

        public static void OnListAllCustomDataInFile()
        {
            try
            {
                SaveDataUtils.ListDataKeysToLog();
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }

        }
	}
}
