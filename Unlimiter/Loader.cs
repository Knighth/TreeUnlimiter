using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using ColossalFramework.Threading;
using ColossalFramework.Steamworks;
using ICities;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace TreeUnlimiter
{
	public class Loader : LoadingExtensionBase
	{
        
        public Loader() { }
        public override void OnCreated(ILoading loading)
        {
  
            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnCreated]"); }
            //It's useless to try and detect loadingmode here as the simmanager loading obj is empty on first start
            //and there after only contains the previous state at this stage, ie the prior map or assset or game.
            //it's fresh state gets updated sometime after OnCreated. Below both will fail with null obj.
            //SimulationManager.UpdateMode mUpdateMode = Singleton<SimulationManager>.instance.m_metaData.m_updateMode;
            //Debug.Log("[TreeUnlimiter:OnCreated] " + loading.currentMode.ToString());
            // Damn shame, as I have to crap in levelloaded() which is after deserialization.

            if (Mod.IsEnabled == true & Mod.IsSetupActive == false)
            {
                if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnCreated]  enabled and redirects not setup yet"); }
                //if (mUpdateMode != SimulationManager.UpdateMode.LoadAsset || mUpdateMode != SimulationManager.UpdateMode.NewAsset)
                //{
                //    if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnCreated]  AssetModeNotDetcted"); }
                    Mod.Setup();  //by default we always run setup.
                //}
            }
            
            if (Mod.IsEnabled == true & Mod.IsSetupActive == true)
            {
                if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnCreated] enabled and redirect setup already"); }
                //if (mUpdateMode == SimulationManager.UpdateMode.LoadAsset || mUpdateMode == SimulationManager.UpdateMode.NewAsset)
                //{
                //    if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnCreated]  AssetModeDetcted"); }
                //    Mod.ReveseSetup();
                //}
 
            }

            base.OnCreated(loading);

        }


        public override void OnLevelLoaded(LoadMode mode)
        {
            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  LoadMode:" + mode.ToString()); }
            if (Mod.IsEnabled == true & Mod.IsSetupActive == false)
            {
                //should rarely, if ever, reach here as should be taken care of in onCreated().
                //if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  enabled and not setup yet"); }
                if (mode != LoadMode.LoadAsset & mode != LoadMode.NewAsset)
                {
                    if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  AssetModeNotDetcted"); }
                    Mod.Setup();  //fire only on non Assett modes.
                }
            }
            
            if (Mod.IsEnabled == true & Mod.IsSetupActive == true)
            {
                if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded] enabled and setup already"); }
                if (mode == LoadMode.LoadAsset || mode == LoadMode.NewAsset)
                {
                    //if we are asseteditor then revert the redirects, and reset the treemanager data.
                    if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  AssetModeDetcted, removing redirects and resetting treemanager"); }
                    ResetTreeMananger(262144, 4096,true);
                    Mod.ReveseSetup();
                    if (mode == LoadMode.NewAsset & Singleton<TreeManager>.instance.m_treeCount < 0)
                    { Singleton<TreeManager>.instance.m_treeCount = 0; }
                }
                
                if (mode == LoadMode.NewMap || mode==LoadMode.LoadMap ) 
                {
                    //total hack to address wierd behavior of -1 m_treecount and 0 itemcount
                    // this hack attempts to jimmy things up the way things appear without the mod loaded in the map editor.
                    // somehow the defaulting of 1 'blank' item doesn't get set correctly when using redirected functions.
                    // really would still like to remove this hack and find actual cause.
                    uint inum;
                    if (Singleton<TreeManager>.instance.m_trees.ItemCount() == 0 & Singleton <TreeManager>.instance.m_treeCount == -1)
                    {
                        if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  New or LoadMap Detected - itemcount==0 treecount == -1"); }
                        if (Singleton<TreeManager>.instance.m_trees.CreateItem(out inum))
                        {
                            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  New or Loadmap Detected - Added padding, createditem# " + inum.ToString()); }
                            Singleton<TreeManager>.instance.m_treeCount = (int)(Singleton<TreeManager>.instance.m_trees.ItemCount() - 1u);
                            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelLoaded]  New or Loadmap Detected - treecount updated: " + Singleton<TreeManager>.instance.m_treeCount.ToString()); }
                        }
                    }
                }

            }

            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true)  //Debugging crap for the above stated hack.
            {
                TreeManager TreeMgr = Singleton<TreeManager>.instance;
                int mtreecountr = TreeMgr.m_treeCount;
                uint mtreebuffsize = TreeMgr.m_trees.m_size;
                int mtreebuffleg = TreeMgr.m_trees.m_buffer.Length;
                uint mtreebuffcount = TreeMgr.m_trees.ItemCount();
                int mupdtreenum = TreeMgr.m_updatedTrees.Length;
                Debug.Log("[TreeUnlimiter:OnLevelLoaded] Debugging-TreeManager: treecount=" + mtreecountr + " msize=" + mtreebuffsize + " mbuffleg=" + mtreebuffleg + " buffitemcount=" + mtreebuffcount + " UpdatedTreesSize=" + mupdtreenum); 
                //Debug.Log("[TreeUnlimiter:OnLevelLoaded]  Done. ModStatus: " + Mod.IsEnabled.ToString() + "    RedirectStatus: " + Mod.IsSetupActive.ToString());
            }
            base.OnLevelLoaded(mode);
        }


        public override void OnLevelUnloading()
        {
            if (Mod.IsEnabled == true | Mod.IsSetupActive == true)
            {
                if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelUnloading]"); }
                ResetTreeMananger(262144, 4096);  //rebuild to org values seems to solve problem of mapeditor retaining prior map trees.
            }
            base.OnLevelUnloading();
        }


        public override void OnReleased()
        {
            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnReleased]"); }
            if (Mod.IsEnabled == true | Mod.IsSetupActive == true)
            {
                Mod.ReveseSetup(); //attempt to revert redirects ca
            }
            base.OnReleased();
        }


        //fuction to re-create the TreeManagers m_trees Array32 buffer entirely.
        public void ResetTreeMananger(uint tsize,uint updatesize,bool bforce = false) 
        {
            uint num;
            if ((int)Singleton<TreeManager>.instance.m_trees.m_buffer.Length != tsize || bforce==true)
            {
                Singleton<TreeManager>.instance.m_trees = new Array32<TreeInstance>((uint)tsize);
                Singleton<TreeManager>.instance.m_updatedTrees = new ulong[updatesize];
//unneccessary?                Singleton<TreeManager>.instance.m_trees.ClearUnused();
                Singleton<TreeManager>.instance.m_trees.CreateItem(out num);
//tried as -1 fix - no dice      Singleton<TreeManager>.instance.m_treeCount = (int)(Singleton<TreeManager>.instance.m_trees.ItemCount() - 1u);

                if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:ResetTreeManager] completed; forced=" + bforce.ToString()); }
            }
        }
	}
}
