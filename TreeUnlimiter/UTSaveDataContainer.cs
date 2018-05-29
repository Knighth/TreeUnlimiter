using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace TreeUnlimiter
{
    [Serializable]
	public class UTSaveDataContainer
	{
        public const string DefaultContainername = "KH_UnlimitedTrees_v1_0";
        public const int CurrentSaveContainerFormatVersion = 1;
        public int SaveFormatVersion = CurrentSaveContainerFormatVersion;
        public bool m_HasBurningTreeData;
        public bool m_HasExtraTreeData;
        public BurningTreeData m_BurningTreeData;
        public ExtraTreeData m_ExtraTreeData;
        public string ContainerName = DefaultContainername;
        public DateTime CreatedDate = DateTime.UtcNow;
        public string GameVersion = BuildConfig.applicationVersionFull.ToString();
        public string GameSeralizerVersion = BuildConfig.DATA_FORMAT_VERSION.ToString();
        public string ModVersionString = Mod.VERSION_BUILD_NUMBER;

        public int ReservedSaveFlags1 = 0;
        public int ReservedSaveFlags2 = 0;
        public string ReservedString1 = "";
        public string ReservedString2 = "";
        public object m_ReservedObject1;
        public int SaveFlags = 0; //0=nothing 1=PackingUsed;
        public byte SaveType = 1;  //1=EmptyFlag 2=normal 4=reserved 8=reserved ...

        public UTSaveDataContainer()
        { }

        public bool VerifyContainer(byte bCase)
        {
            try 
            {
                switch(bCase)
                {
                    case 0: //top level basic verification.
                        if ((this.SaveFormatVersion <= CurrentSaveContainerFormatVersion) && (String.IsNullOrEmpty(this.ContainerName) == true))
                        {
                            return true;
                        }
                        break;
                    case 1:
                        if (hasExtraTreeData)
                        {
                            return true;
                        }
                        break;
                    case 2:
                        if (hasBurningTreesData)
                        { 
                            return true; 
                        }
                        break;
                    default:
                        break;
                }
            }
            catch(Exception ex)
            { Logger.dbgLog("", ex); }
            return false;

        }


        public bool hasBurningTreesData
        {
            get
            {
                if (m_BurningTreeData != null && m_BurningTreeData.BurningCount > 0)
                {
                    m_HasBurningTreeData = true;
                    return true;
                }
                return false;
            }
            set { m_HasBurningTreeData = hasBurningTreesData; }
        }


        public bool hasExtraTreeData
        {
            get
            {
                if (m_ExtraTreeData != null && (m_ExtraTreeData.ExtraTreeCount > 0 | m_ExtraTreeData.ExtraTreeList != null))
                {
                    return true;
                }
                return false;
            }
            set { m_HasBurningTreeData = hasBurningTreesData; }
        }



        //Holds our BurningTreeData container
        [Serializable]
        public class BurningTreeData
        {
            public const int CurrentSaveBurningFormatVersion = 1;
            public int SaveFormatVersion = CurrentSaveBurningFormatVersion;
            public int SaveFlags =0;
            public int BurningCount =0;
            public bool isPacked;
            public int ReservedInt1= 0;
            public object ReservedObject1;
            public List<UTBurningTreeInstance> BurningTreeList;

        }

        //Holds our ExtraTreeData container
        [Serializable]
        public class ExtraTreeData
        {
            public const int CurrentSaveTreeFormatVersion = 1;
            public int SaveFormatVersion = CurrentSaveTreeFormatVersion;
            public int SaveFlags = 0;
            public int OriginalSaveLimitSetting =0;
            public int ExtraTreeCount = 0;
            public bool isPacked = false;
            public int ReservedInt1;
            public object ReservedObject1;
            public List<UTTreeInstance> ExtraTreeList;
        }

        [Serializable]
        public struct UTTreeInstance
        {
            public byte version;
            public ushort pos_x;
            public ushort pos_z ;
            public ushort m_infoIndex;
            public ushort m_flags;
            public uint idxWhenSaved;
        }

        [Serializable]
        public struct UTBurningTreeInstance
        {
            public byte version;
            public uint m_treeIndex;
            public byte m_fireIntensity;
            public byte m_fireDamage;
            public uint idxWhenSaved;
        }
        

	}
}
