using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using ColossalFramework.Threading;
//using ColossalFramework.Steamworks;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace TreeUnlimiter
{
    public class Loader : LoadingExtensionBase
    {
        internal static bool LastSaveUsedPacking = false;
        internal static List<int> LastSaveList;
        public Loader() {}
        public override void OnCreated(ILoading loading)
        {
            
            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Logger.dbgLog("OnCreated fired."); }
            //It's useless to try and detect loadingmode here as the simmanager loading obj is empty on first start
            //and there after only contains the previous state at this stage, ie the prior map or assset or game.
            //it's fresh state gets updated sometime after OnCreated. Below both will fail with null obj.
            //SimulationManager.UpdateMode mUpdateMode = Singleton<SimulationManager>.instance.m_metaData.m_updateMode;
            //Debug.Log("[TreeUnlimiter:OnCreated] " + loading.currentMode.ToString());
            // Damn shame, as I have to crap in levelloaded() which is after deserialization.
            try
            {
                if (Mod.IsEnabled == true & Mod.IsSetupActive == false)
                {
                    if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Logger.dbgLog("Enabled and redirects not setup yet"); }
                    //if (mUpdateMode != SimulationManager.UpdateMode.LoadAsset || mUpdateMode != SimulationManager.UpdateMode.NewAsset)
                    //{
                    //    if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnCreated]  AssetModeNotDetcted"); }

                    //1.6.0 - This does not work anymore because onCreated() is firing
                    // way to late in the load process now, so while I'm keeping it here.
                    // it's really now as a back up.
                    Mod.Setup();  //by default we always run setup again.

                    
                    //}
                }
                //9-25-2015 - *no longer needed
                /*
                            if (Mod.IsEnabled == true & Mod.IsSetupActive == true && Mod.DEBUG_LOG_ON)
                            {
                                if (Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:Loader::OnCreated] enabled and redirect setup already"); }
                                //if (mUpdateMode == SimulationManager.UpdateMode.LoadAsset || mUpdateMode == SimulationManager.UpdateMode.NewAsset)
                                //{
                                //    if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnCreated]  AssetModeDetcted"); }
                                //    Mod.ReveseSetup();
                                //}

                            }
                */
            }
            catch (Exception ex)
            {
                Logger.dbgLog("Onlevelload Exception:", ex, true);
            }
            
            base.OnCreated(loading);

        }


        public override void OnLevelLoaded(LoadMode mode)
        {
            LastSaveUsedPacking = false;
            if (Mod.DEBUG_LOG_ON == true) { Logger.dbgLog("Map LoadMode:" + mode.ToString()); }
            try
            {
                //hide ability to change maxtrees.
                if (Mod.maxTreeSlider != null) { Mod.maxTreeSlider.Disable();}

                if (Mod.IsEnabled == true & Mod.IsSetupActive == false)
                {
                    //should rarely, if ever, reach here as should be taken care of in onCreated().
                    // if we ran tried to run setup here we could but our Array was not expanded
                    // during the load deserialize process that came before us. Hence an attempt to save will produce
                    // a problem during custom serialze save as it'll exception error cause the buffer wasn't expanded.
                    // Will maybe enhance custom_serialzer to check for bigger buffer first, 
                    // though let's avoid that problem entirely here.

                    if (mode != LoadMode.LoadAsset & mode != LoadMode.NewAsset)  //fire only on non Assett modes, we don't want it to get setup on assett mode anyway.
                    {
                        if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Logger.dbgLog(" AssetModeNotDetcted"); }
                        string strmsg = "[TreeUnlimiter:OnLevelLoaded]  *** Enabled but not setup yet, why did this happen??\n" +
                            "Did OnCreated() not fire?? did redirections exception error?\n If you see this please contact author or make sure other mods did not cause a critical errors prior to this one during the load process." +
                            "\n We are now going to disable this mod from running during this map load attempt.";
                        Logger.dbgLog(strmsg);
                        DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, strmsg);
                        //1.2.0f3_Build007 above noted Bug - Mod.Setup();  
                    }
                }

                if (Mod.IsEnabled == true & Mod.IsSetupActive == true)
                {
                    if (Mod.DEBUG_LOG_ON == true) { Logger.dbgLog("Enabled and setup already.(expected)"); }
                    
                    if (mode == LoadMode.LoadAsset || mode == LoadMode.NewAsset)
                    {
                        //if we are asseteditor then revert the redirects, and reset the treemanager data.
                        if (Mod.DEBUG_LOG_ON == true) { Logger.dbgLog("AssetModeDetcted, removing redirects and resetting treemanager"); }

                        //1.6.0 commented out these 2 lines
                        //ResetTreeMananger(Mod.DEFAULT_TREE_COUNT, Mod.DEFAULT_TREEUPDATE_COUNT, true);
                        //Mod.ReveseSetup();

                        if (mode == LoadMode.NewAsset & (Singleton<TreeManager>.instance.m_treeCount < 0))
                        {
                            if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("AssetModeDetcted, Treecount is less < then 0 !"); };
                            //{ Singleton<TreeManager>.instance.m_treeCount = 0; }
                        }
                    }

                    if (mode == LoadMode.NewMap || mode == LoadMode.LoadMap || mode == LoadMode.NewGame || mode== LoadMode.LoadMap)
                    {
                        //total hack to address wierd behavior of -1 m_treecount and 0 itemcount
                        // this hack attempts to jimmy things up the way things appear without the mod loaded in the map editor.
                        // somehow the defaulting of 1 'blank' item doesn't get set correctly when using redirected functions.
                        // really would still like to remove this hack and find actual cause.
                        //                    uint inum;
                        if (Singleton<TreeManager>.instance.m_trees.ItemCount() == 0 & Singleton<TreeManager>.instance.m_treeCount == -1)
                        {
                            if (Mod.DEBUG_LOG_ON == true) { Logger.dbgLog(" New or LoadMap Detected & itemcount==0 treecount == -1"); }
                            //removed for 1 vs 0 fix in Deserialize routine that was causing hack problem.
                            //                        if (Singleton<TreeManager>.instance.m_trees.CreateItem(out inum))
                            //                        {
                            //                            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  New or Loadmap Detected - Added padding, createditem# " + inum.ToString()); }
                            //                            Singleton<TreeManager>.instance.m_treeCount = (int)(Singleton<TreeManager>.instance.m_trees.ItemCount() - 1u);
                            //                            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  New or Loadmap Detected - treecount updated: " + Singleton<TreeManager>.instance.m_treeCount.ToString()); }
                            //                        }
                        }
                    }

                }

                if (Mod.DEBUG_LOG_ON == true)  //Debugging crap for the above stated hack.
                {
                    TreeManager TreeMgr = Singleton<TreeManager>.instance;
                    int mtreecountr = TreeMgr.m_treeCount;
                    uint mtreebuffsize = TreeMgr.m_trees.m_size;
                    int mtreebuffleg = TreeMgr.m_trees.m_buffer.Length;
                    uint mtreebuffcount = TreeMgr.m_trees.ItemCount();
                    int mupdtreenum = TreeMgr.m_updatedTrees.Length;
                    int mburntreenum = TreeMgr.m_burningTrees.m_size;
                    Logger.dbgLog("Debugging-TreeManager: treecount=" + mtreecountr.ToString() + " msize=" + mtreebuffsize.ToString() + " mbuffleg=" + mtreebuffleg.ToString() + " buffitemcount=" + mtreebuffcount.ToString() + " UpdatedTreesSize=" + mupdtreenum.ToString() + " burntrees=" + mburntreenum.ToString());
                    //Debug.Log("[TreeUnlimiter:OnLevelLoaded]  Done. ModStatus: " + Mod.IsEnabled.ToString() + "    RedirectStatus: " + Mod.IsSetupActive.ToString());
                    
                }
            }

            catch (Exception ex)
            {
                Logger.dbgLog("Onlevelload Exception:", ex, true);
            }
            base.OnLevelLoaded(mode);
        }


        public override void OnLevelUnloading()
        {
            try
            {
                if (Mod.IsEnabled == true | Mod.IsSetupActive == true)
                {
                    if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Logger.dbgLog("OnLevelUnloading()"); }
                    ResetTreeMananger(Mod.DEFAULT_TREE_COUNT, Mod.DEFAULT_TREEUPDATE_COUNT);  //rebuild to org values seems to solve problem of mapeditor retaining prior map trees.
                    if (LastSaveList != null)
                    { LastSaveList.Clear(); LastSaveList.Capacity = 1; LastSaveList = null; }
                }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("Error: ",ex,true);
            }
            base.OnLevelUnloading();
        }


        public override void OnReleased()
        {
            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Logger.dbgLog("OnReleased()"); }

            if (Mod.IsEnabled == true | Mod.IsSetupActive == true)
            {
                //1.6.0 -
                // we're going to temp. not do this.
                //Mod.ReveseSetup(); //attempt to revert redirects | has it's own try catch.
            }
            base.OnReleased();
        }


        //fuction to re-create the TreeManagers m_trees Array32 buffer entirely.
        //used to make sure it's clean and our objects don't carry over map 2 map.
        //also used in certain cases where we want to revert back to nomal treemanager sizes.
        public void ResetTreeMananger(uint tsize, uint updatesize, bool bforce = false)
        {
            uint num;
            object[] ostring = new object[]{ tsize.ToString(), updatesize.ToString(), bforce.ToString(), Singleton<TreeManager>.instance.m_trees.m_buffer.Length.ToString() };
            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true && TreeUnlimiter.Mod.DEBUG_LOG_LEVEL > 1)
            { Logger.dbgLog(string.Format("ResetTreeManager tszie= {0} updatesize= {1} bforce= {2} currentTMlen= {3}", ostring)); }

            if ((int)Singleton<TreeManager>.instance.m_trees.m_buffer.Length != tsize || bforce == true)
            {
                Singleton<TreeManager>.instance.m_trees = new Array32<TreeInstance>((uint)tsize);
                Singleton<TreeManager>.instance.m_updatedTrees = new ulong[updatesize];
                Singleton<TreeManager>.instance.m_trees.CreateItem(out num);
                if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Logger.dbgLog("ResetTreeManager completed; forced=" + bforce.ToString()); }
            }
        }
    }
}
