using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ICities;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TreeUnlimiter
{
	public class Loader : LoadingExtensionBase
	{
        public Loader() { }

        public override void OnLevelUnloading()
        {
            if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelUnloading]"); }
//            ResetTreeMananger(262144, 16384); //mod vals
            ResetTreeMananger(262144, 4096);  //org seems to solve problem of mapeditor retaining prior map trees.
            base.OnLevelUnloading();
        }

        public void ResetTreeMananger(uint tsize,uint updatesize) 
        {
            uint num;
            if ((int)Singleton<TreeManager>.instance.m_trees.m_buffer.Length != tsize)
            {
                Singleton<TreeManager>.instance.m_trees = new Array32<TreeInstance>((uint)tsize);
                Singleton<TreeManager>.instance.m_updatedTrees = new ulong[updatesize];
                Singleton<TreeManager>.instance.m_trees.ClearUnused();
                Singleton<TreeManager>.instance.m_trees.CreateItem(out num);
                if (TreeUnlimiter.Mod.DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter:OnLevelUnloading::ResetTreeManager]"); }
            }
        }
	}
}
