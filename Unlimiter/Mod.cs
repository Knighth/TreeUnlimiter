using CitiesSkylinesDetour;
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
	public class Mod : IUserMod
	{
		internal const int MOD_TREE_SCALE = 4;

		internal const int DEFAULT_TREE_COUNT = 262144;

		public static bool IsEnabled;

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
				this.Setup();
				return "Tree Unlimiter 1.1";
			}
		}

		public Mod()
		{
		}

		private void PluginsChanged()
		{
			try
			{
    			PluginManager.PluginInfo pluginInfo = (
					from p in Singleton<PluginManager>.instance.GetPluginsInfo()
#if (DEBUG)  //used for local debug testing
                    where p.name.ToString() == "TreeUnlimiter 1.1"
#else   //used for steam distribution - public release.  
                    where p.publishedFileID.AsUInt64 == (long)455403039
#endif
                    select p).FirstOrDefault<PluginManager.PluginInfo>();
				if (pluginInfo == null)
				{
					Mod.IsEnabled = false;
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "[TreeUnlimiter] Can't find self. No idea if this mod is enabled.");
                    Debug.Log("[TreeUnlimiter] Can't find self. No idea if this mod is enabled");
				}
				else
				{
					Mod.IsEnabled = pluginInfo.isEnabled;
                    Debug.Log("[TreeUnlimiter] Mod is enabled, id:" + pluginInfo.publishedFileID.AsUInt64.ToString());
                }
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				Debug.LogException(exception);
                object[] type = new object[] { "[TreeUnlimiter] ", exception.GetType(), ": ", exception.Message };
				DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, string.Concat(type));
                Debug.Log("[TreeUnlimiter] " + exception.Message.ToString());
            }
		}

		private void RedirectCalls(Type type1, Type type2, string p)
		{
			RedirectionHelper.RedirectCalls(type1.GetMethod(p, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), type2.GetMethod(p, BindingFlags.Static | BindingFlags.NonPublic));
		}

		private void Setup()
		{
			try
			{
				this.RedirectCalls(typeof(BuildingDecoration), typeof(LimitBuildingDecoration), "ClearDecorations");
				this.RedirectCalls(typeof(BuildingDecoration), typeof(LimitBuildingDecoration), "SaveProps");
				this.RedirectCalls(typeof(NaturalResourceManager), typeof(LimitNaturalResourceManager), "TreesModified");
				this.RedirectCalls(typeof(TreeTool), typeof(LimitTreeTool), "ApplyBrush");
				this.RedirectCalls(typeof(TreeManager.Data), typeof(LimitTreeManager.Data), "Serialize");
				this.RedirectCalls(typeof(TreeManager.Data), typeof(LimitTreeManager.Data), "Deserialize");
				MethodInfo[] methods = typeof(LimitTreeManager).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
				for (int i = 0; i < (int)methods.Length; i++)
				{
					MethodInfo methodInfo = methods[i];
					this.RedirectCalls(typeof(TreeManager), typeof(LimitTreeManager), methodInfo.Name);
				}
				this.PluginsChanged();
				Singleton<PluginManager>.instance.eventPluginsChanged += new PluginManager.PluginsChangedHandler(this.PluginsChanged);
				Singleton<PluginManager>.instance.eventPluginsStateChanged += new PluginManager.PluginsChangedHandler(this.PluginsChanged);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, exception.ToString());
				Debug.LogException(exception);
			}
		}
	}
}