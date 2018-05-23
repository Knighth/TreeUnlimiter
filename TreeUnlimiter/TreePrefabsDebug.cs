using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.IO;
using UnityEngine;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TreeUnlimiter.OptionsFramework;


namespace TreeUnlimiter
{
	public static class TreePrefabsDebug
	{
        private static Dictionary<int,int> _LoadedIndexes = new Dictionary<int,int>();

        [Flags]
        public enum NullTreeOptions
        {
            [Description("DoNothing (Default)")]
            DoNothing = 0,
            [Description("Replace")]
            ReplaceTree = 1,
            [Description("Remove")]
            RemoveTree = 2,
        }


        /// <summary>
        /// A method to remove *ALL* tress from a map.
        /// This is provided only as an emergency option for people with really broken maps as a last resort.
        /// </summary>
        /// <param name="confirmed">If not set to true function does nothing</param>
        internal static void RemoveAllTrees(bool confirmed)
        {
            int c = 0;
            try 
            {
                if (confirmed == true & OptionsWrapper<Configuration>.Options.EmergencyOnly_RemoveAllTrees == true & OptionsWrapper<Configuration>.Options.IsLoggingEnabled() == true & OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1 & OptionsWrapper<Configuration>.Options.NullTreeOptionsIndex == (int)NullTreeOptions.RemoveTree)
                {
                    TreeManager treeManager = Singleton<TreeManager>.instance;
                    TreeInstance[] tBuffer = treeManager.m_trees.m_buffer;
                    Logger.dbgLog("Now deleating all active trees as requested!");
                    for (uint i = 1; i < tBuffer.Length; i++)
                    {
                        if (tBuffer[i].m_flags != 0)
                        { 
                            treeManager.ReleaseTree(i);
                            c++;
                        }
                    }
                    Logger.dbgLog("Removed all " + c.ToString() + " active trees as instructed!"); 
                }
                else 
                {
                    Logger.dbgLog("*** You tried a danagerous EmergencyOnly_RemoveAllTrees option but did not enable debug level 2 and-or RemoveTree settings, for your safety we did not do anything.  ***"); 
                }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("Could not remove all trees as instructed due to errors " + c.ToString() + " were removed.", ex, true);
            }
        }


        /// <summary>
        /// Deletes the tree specified by the given treebuffer index.
        /// </summary>
        /// <param name="idx"></param>
        internal static void RemoveSpecificTree(uint idx)
        {
            try
            {
                if (OptionsWrapper<Configuration>.Options.NullTreeOptionsIndex == (int)NullTreeOptions.RemoveTree)
                {
                    TreeManager treeManager = Singleton<TreeManager>.instance;
                    treeManager.ReleaseTree(idx);
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled())
                    { Logger.dbgLog("Removed specific tree at index " + idx.ToString() + " as instructed!"); }
                }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("Could not remove specific tree at index " + idx.ToString() + " due to error.", ex, true);
            }
        }

        
        /// <summary>
        /// Replaces a specific tree index's .Info obj and .m_infoIndex value with that of the default tree prefab
        /// at index 0 in the prefabcollection.
        /// </summary>
        /// <param name="idx">uint of the idx in the treeinstance array to replace</param>
        internal static void ReplaceSpecificTree(uint idx)
        {
            try
            {
                TreeInstance[] tbuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                TreeInfo tinew = PrefabCollection<TreeInfo>.GetLoaded(0);
                if (tinew != null)
                {
                    ushort old = tbuffer[idx].m_infoIndex;
                    tbuffer[idx].Info = tinew;
                    tbuffer[idx].m_infoIndex = (ushort)tinew.m_prefabDataIndex;
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled())
                    { Logger.dbgLog("Replaced specific tree " + idx.ToString() + " as instructed with infoindx " + old.ToString() + " to info(0) " + tinew.name.ToString() + " new m_infoidx " + tbuffer[idx].m_infoIndex.ToString()); }
                }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("Could not replace specific tree " + idx.ToString() ,ex,true);
            }

        }

        /// <summary>
        /// Populates a dictionary with the scene_prefabs that are loaded.
        /// key = thier dataindex value; value=index in loadedarray;
        /// </summary>
        internal static void validateTreeInfosLoaded()
        {
            try
            {
                _LoadedIndexes.Clear();
                int totalcount = PrefabCollection<TreeInfo>.LoadedCount();
                if (totalcount > 0)
                {
                    int x = 0;
                    for (uint i = 0; i < totalcount; i++)
                    {
                        TreeInfo ti = null;
                        try
                        {
                            ti = PrefabCollection<TreeInfo>.GetLoaded(i);
                        }
                        catch (Exception ex)
                        {
                            Logger.dbgLog("error getloaded() " + i.ToString(), ex);
                        }
                        if (ti != null & ti.m_prefabDataIndex != -1) //.m_prefabInitialized
                        { 
                            int q = -1;
                            if (_LoadedIndexes.TryGetValue(ti.m_prefabDataIndex, out q) == false)
                            { _LoadedIndexes.Add(ti.m_prefabDataIndex, (int)i); }
                            x++; 
                        }
                    }

                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1)
                    {
                        string tmpstr = "";
                        foreach (KeyValuePair<int, int> kvp in _LoadedIndexes)
                        {
                            tmpstr = string.Concat(tmpstr, kvp.Key.ToString() + ",");
                        }
                        Logger.dbgLog("validated loaded _loadedindexes = " + tmpstr);
                    }
                }
            }
            catch (Exception ex)
            { Logger.dbgLog(" Error validating scene_loaded TreeInfo's", ex); }

        }

        /// <summary>
        /// Debugging function to dump treeinfo prefab data.
        /// </summary>
        /// <param name="idbyte">Debug Marker id of where in the code it's executing</param>
        internal static void DumpLoadedPrefabInfos(byte idbyte = 0)
        {
            int RealCount = PrefabCollection<TreeInfo>.PrefabCount(); //real? ie simulation_prefab size
            int LoadedCount = PrefabCollection<TreeInfo>.LoadedCount(); //attempted? scene_prefab size
            string tmpstr = string.Concat("Dumping Tree prefab information (" + idbyte.ToString() + ") ", "RealCount: " + RealCount.ToString(), " LoadedCount: " + LoadedCount.ToString());
            Logger.dbgLog(tmpstr);
            if (RealCount > 0)
            {
                Logger.dbgLog("Simulation_prefabs:");
                try
                {
                    string ti_null = "nullobj";
                    string[] tmparray = new string[6] { ti_null, ti_null, ti_null, ti_null, ti_null, ti_null};
                    for (uint i = 0; i < RealCount; i++)
                    {
                        tmparray[0] = i.ToString();
                        tmparray[1] = PrefabCollection<TreeInfo>.PrefabName(i);
                        TreeInfo ti = PrefabCollection<TreeInfo>.GetPrefab(i);
                        tmparray[2] = ti_null.ToString();
                        tmparray[3] = ti_null.ToString();
                        tmparray[4] = ti_null.ToString();
                        tmparray[5] = ti_null.ToString();

                        if (ti != null)
                        {
                            tmparray[2] = ti.m_prefabDataIndex.ToString();
                            tmparray[3] = ti.m_prefabInitialized.ToString();
                            tmparray[4] = ti.name.ToString();
                            tmparray[5] = ti.m_dlcRequired.ToString();
                        }
                        
                        Logger.dbgLog(String.Format("CoreIdx:{0} PrefabName:{1} DataIdx:{2} Init:{3} gamename:{4} dlcneeded:{5} ",tmparray),null,false,true);
                        tmparray[0] = ti_null;
                        tmparray[1] = ti_null;
                        tmparray[2] = ti_null;
                        tmparray[3] = ti_null;
                        tmparray[4] = ti_null;
                        tmparray[5] = ti_null;
                    }

                }
                catch (Exception ex)
                { Logger.dbgLog("simulationprefabs ", ex); }
            }

            if (LoadedCount > 0)
            {
                Logger.dbgLog("Loaded_scene_prefabs:");
                try
                {
                    string ti_null2 = "nullobj";
                    string[] tmparray2 = new string[6] { ti_null2, ti_null2, ti_null2, ti_null2, ti_null2, ti_null2 };
                    for (uint i = 0; i < LoadedCount; i++)
                    {
                        tmparray2[0] = i.ToString();
                        tmparray2[1] = PrefabCollection<TreeInfo>.PrefabName(i);
                        TreeInfo ti = PrefabCollection<TreeInfo>.GetLoaded(i);
                        tmparray2[2] = ti_null2.ToString();
                        tmparray2[3] = ti_null2.ToString();
                        tmparray2[4] = ti_null2.ToString();
                        tmparray2[5] = ti_null2.ToString();

                        if (ti != null)
                        {
                            tmparray2[2] = ti.m_prefabDataIndex.ToString();
                            tmparray2[3] = ti.m_prefabInitialized.ToString();
                            tmparray2[4] = ti.name.ToString();
                            tmparray2[5] = ti.m_dlcRequired.ToString();
                        }

                        Logger.dbgLog(String.Format("CoreIdx:{0} PrefabName:{1} DataIdx:{2} Init:{3} gamename:{4} dlcneeded:{5}", tmparray2), null, false, true);
                        tmparray2[0] = ti_null2;
                        tmparray2[1] = ti_null2;
                        tmparray2[2] = ti_null2;
                        tmparray2[3] = ti_null2;
                        tmparray2[4] = ti_null2;
                        tmparray2[5] = ti_null2;
                    }

                }
                catch (Exception ex)
                { Logger.dbgLog("loaded_scene_prefabs", ex); }
            }
        }

        /// <summary>
        /// Wrapper to handles the remove or replace operation.
        /// </summary>
        /// <param name="idx"></param>
        internal static void RemoveOrReplace(uint idx)
        {
            if (OptionsWrapper<Configuration>.Options.NullTreeOptionsIndex == (int)NullTreeOptions.ReplaceTree)
            {
                ReplaceSpecificTree(idx); 
            }
            else if (OptionsWrapper<Configuration>.Options.NullTreeOptionsIndex == (int)NullTreeOptions.RemoveTree)
            { 
                RemoveSpecificTree(idx); 
            }

        }

        /// <summary>
        /// The original C/O code to loop though the tree array and update m_infoIndex
        /// on all treeinstances with non-null .Info's with updated m_prefabDataIndex values.
        /// </summary>
        public static void DoOriginal()
        {
            TreeManager instance = Singleton<TreeManager>.instance;
            TreeInstance[] buffer = instance.m_trees.m_buffer;
            int num = buffer.Length;
            for (int i = 1; i < num; i++)
            {
                if (buffer[i].m_flags != 0)
                {

                    TreeInfo info = buffer[i].Info;
                    if (info != null)
                    {
                        buffer[i].m_infoIndex = (ushort)info.m_prefabDataIndex;
                    }
                }
            }
        }

        /// <summary>
        /// Handles all the TreeInfo validation for all trees marked created.
        /// </summary>
        /// <returns>true if successful an no need to run original or false if error or it didn't do anything</returns>
        internal static bool ValidateAllTreeInfos()
        {
            bool errflag = false;
            try
            {
                if (OptionsWrapper<Configuration>.Options.EmergencyOnly_RemoveAllTrees == true)
                {
                    if (OptionsWrapper<Configuration>.Options.NullTreeOptionsIndex == (int)NullTreeOptions.RemoveTree)
                    {
                        RemoveAllTrees(true);
                        return false;
                    }
                }
                validateTreeInfosLoaded();
                List<int> TreeIndexes = Packer.GetPackedList();
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1)
                {
                    Logger.dbgLog("TreeIndexes count = " + TreeIndexes.Count.ToString());
                    Logger.dbgLog("total m_treeCount = " + Singleton<TreeManager>.instance.m_treeCount.ToString());
                }
                uint total = (uint)TreeIndexes.Count;

                if (total > 1) //has real trees 'tree[0] = dummy
                {
                    TreeInstance[] tbuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                    int nullcounter = 0;
                    int errcounter = 0;
                    TreeInfo ti = null;
                    for (int i = 1; i < total; i++) //ignore item 0 from packedlist.
                    {
                        if (tbuffer[(uint)TreeIndexes[i]].m_infoIndex >= 0)
                        {
                            ti = tbuffer[(uint)TreeIndexes[i]].Info;
                            if (ti == null || ti.m_prefabDataIndex < 0)
                            {
                                nullcounter++;
                                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled())
                                {
                                    Logger.dbgLog("tree idx " + TreeIndexes[i].ToString() + " .Info or Info.m_prefabDataIndex was -1");
                                }
                                RemoveOrReplace((uint)TreeIndexes[i]);
                            }
                            else
                            {
                                if (_LoadedIndexes.ContainsKey(ti.m_prefabDataIndex) == false)
                                {
                                    errcounter++;
                                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled())
                                    {
                                        Logger.dbgLog("tree idx " + TreeIndexes[i].ToString() + " contains an Info.m_prefabDataIndex of " + ti.m_prefabDataIndex.ToString() + " which was not in the validated list.");
                                    }
                                    RemoveOrReplace((uint)TreeIndexes[i]);
                                }

                                //clean update the infoindex like the original does for non-nulls.
                                tbuffer[(uint)TreeIndexes[i]].m_infoIndex = (ushort)tbuffer[(uint)TreeIndexes[i]].Info.m_prefabDataIndex;
                            }
                        }
                        else
                        {
                            nullcounter++;
                            //not sure.. set to 0?
                        }

                    }
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled())
                    {
                        Logger.dbgLog("validation report -  totalchecked:" + total.ToString() + "; errcounter:" + errcounter.ToString() + "; nullcounter:" + nullcounter.ToString());
                    }
                    errflag = true;
                }
                else
                {
                    Logger.dbgLog("No real trees to validate.");
                    errflag = false; //set to that original code upstream is attepted.
                }
            }
            catch (Exception ex)
            { 
                Logger.dbgLog("Major error: ", ex, true);
                errflag = false;  //set so that original code upstream is attempted.
            }
            return errflag;
        }
	}
}
