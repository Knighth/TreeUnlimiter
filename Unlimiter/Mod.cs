using CitiesSkylinesDetour;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TreeUnlimiter
{
	public class Mod : IUserMod
	{
		internal const int MOD_TREE_SCALE = 4;
        internal const long MOD_WORKSHOPID = 455403039;
		internal const int DEFAULT_TREE_COUNT = 262144;
        internal const string MOD_OFFICIAL_NAME = "Tree Unlimiter 1.1";
        public static readonly string MOD_CONFIGPATH = "TreeUnlimiterConfig.xml";
		public static bool IsEnabled = false;
        public static bool IsInited = false;
        public static bool IsSetupActive = false;
        public static bool DEBUG_LOG_ON = false;
        public static SimulationManager.UpdateMode LastMode = SimulationManager.UpdateMode.Undefined;
        private static Dictionary<MethodInfo, RedirectCallsState> redirectDic = new Dictionary<MethodInfo, RedirectCallsState>();
        public Configuration config;


		public string Description
		{
			get
			{
				return "Allows you to place way more trees!";
			}
		}

		public string Name
		{
			get
			{
                if (config != null)
                { DEBUG_LOG_ON = config.DebugLogging; }

                if (IsInited == false)
                {
                    init(); 
                }

                return MOD_OFFICIAL_NAME;
			}
		}

		public Mod()
		{
            config = Configuration.Deserialize(MOD_CONFIGPATH);
            if(config == null)
            {
                config = new Configuration();
                config.DebugLogging = false;
            }
		}
        private void LoggingChecked(bool en)
        {
            DEBUG_LOG_ON = en;
            config.DebugLogging = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }
        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("TreeUnlimiter Logging");
            group.AddCheckbox("Enable Verbose Logging", DEBUG_LOG_ON, LoggingChecked);
        }


		public static void PluginsChanged()
		{
			try
			{
    			PluginManager.PluginInfo pluginInfo = (
					from p in Singleton<PluginManager>.instance.GetPluginsInfo()
#if (DEBUG)  //used for local debug testing
                    where p.name.ToString() == MOD_OFFICIAL_NAME
#else   //used for steam distribution - public release.
                    where p.publishedFileID.AsUInt64 == (long)MOD_WORKSHOPID
#endif
                    select p).FirstOrDefault<PluginManager.PluginInfo>();
				if (pluginInfo == null)
				{
					Mod.IsEnabled = false;
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "[TreeUnlimiter::PluginsChanged()] Can't find self. No idea if this mod is enabled.");
                    Debug.Log("[TreeUnlimiter::PluginsChanged()] Can't find self. No idea if this mod is enabled");
				}
				else
				{
					Mod.IsEnabled = pluginInfo.isEnabled;
                    if (Mod.IsEnabled == true)
                    {
                        Debug.Log("[TreeUnlimiter] Mod is enabled, id:" + pluginInfo.publishedFileID.AsUInt64.ToString());
                        if (IsSetupActive == false)
                        {
                            //Setup();
                        }
                    }
                    else
                    {
                        Debug.Log("[TreeUnlimiter] Mod is disabled, id:" + pluginInfo.publishedFileID.AsUInt64.ToString());
                        if (IsSetupActive == true)
                        {
                            ReveseSetup();
                        }
                    }
                }
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				Debug.LogException(exception);
                object[] type = new object[] { "[TreeUnlimiter::PluginsChanged()] ", exception.GetType(), ": ", exception.Message };
				DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, string.Concat(type));
                Debug.Log("[TreeUnlimiter::PluginsChanged()] " + exception.Message.ToString());
            }
		}

		private static void RedirectCalls(Type type1, Type type2, string p)
		{
            var bindflags1 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var bindflags2 = BindingFlags.Static | BindingFlags.NonPublic;
            var theMethod =  type1.GetMethod(p, bindflags1);
            redirectDic.Add(theMethod, RedirectionHelper.RedirectCalls(theMethod, type2.GetMethod(p, bindflags2), false));
            //RedirectionHelper.RedirectCalls(type1.GetMethod(p, bindflags1), type2.GetMethod(p, bindflags2), false);
		}

        public static void init()
        {
            if (IsInited == false)
            {
                PluginsChanged();
                Singleton<PluginManager>.instance.eventPluginsChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                Singleton<PluginManager>.instance.eventPluginsStateChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                IsInited = true;
            }
        }

		public static void Setup()
		{
            if (IsSetupActive) { return; }

            try
			{
				RedirectCalls(typeof(BuildingDecoration), typeof(LimitBuildingDecoration), "ClearDecorations");
				RedirectCalls(typeof(BuildingDecoration), typeof(LimitBuildingDecoration), "SaveProps");
				RedirectCalls(typeof(NaturalResourceManager), typeof(LimitNaturalResourceManager), "TreesModified");
				RedirectCalls(typeof(TreeTool), typeof(LimitTreeTool), "ApplyBrush");
				RedirectCalls(typeof(TreeManager.Data), typeof(LimitTreeManager.Data), "Serialize");
				RedirectCalls(typeof(TreeManager.Data), typeof(LimitTreeManager.Data), "Deserialize");
				MethodInfo[] methods = typeof(LimitTreeManager).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
				for (int i = 0; i < (int)methods.Length; i++)
				{
					MethodInfo methodInfo = methods[i];
					RedirectCalls(typeof(TreeManager), typeof(LimitTreeManager), methodInfo.Name);
				}
                IsSetupActive = true;
                if (DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter::Mod.Setup()]: Redirected Calls"); }
            }
			catch (Exception exception1)
			{
				Exception exception = exception1;
				DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, exception.ToString());
                Debug.Log("[TreeUnlimiter::Mod.Setup()Exception]: " + exception.Message.ToString());
			}
		}


        public static void ReveseSetup()
        {
            if(IsSetupActive == false){ return;}
            if (redirectDic.Count == 0) 
            {
                if (DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter::ReverseSetup()] No state entries exists to Revert"); }
                return;
            }
            try
            {
                foreach( var keypair in redirectDic)
                {
                    RedirectionHelper.RevertRedirect(keypair.Key ,keypair.Value);
                }
                redirectDic.Clear();
                IsSetupActive = false;
                if (DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter::ReverseSetup()] Reverted redirected calls"); }
            }
            catch (Exception exception1)
            { Debug.Log("TreeUnlimiter::ReverseSetup()] " + exception1.Message.ToString());}
        }
        
    }
}