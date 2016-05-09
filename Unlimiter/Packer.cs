using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ColossalFramework;
using ColossalFramework.IO;
using UnityEngine;

namespace TreeUnlimiter
{
	static class Packer
	{
        public static List<int> GetPackedList()
        {
            List<int> treeidx;
            TreeManager TM = Singleton<TreeManager>.instance;
            try 
            {
                if (TM != null && TM.m_treeCount > 0)
                {
                    treeidx = new List<int>(TM.m_treeCount);
                    //Sadly we have to jackup entry [0] to account for buffer[0] and 1 based entries in serialize\deserialize  
                    treeidx.Add(0); //add dummy entry at list index[0]
                    for(int i = 0 ; i < TM.m_trees.m_size;i++)
                    {
                        if (TM.m_trees.m_buffer[i].m_flags != 0)
                        {
                            treeidx.Add(i);
                        }
                    }
                    return treeidx;
                }
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex, true); }
            return new List<int>();
        }


        internal static void Serialize(ref List<int> idxList, ref DataSerializer s)
        {
            
            //Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginSerialize(s, "TreeManager");


            if(Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
            {
                if (idxList == null)
                { Logger.dbgLog("idxlist is null. lastsaveusedpacking: " + Loader.LastSaveUsedPacking.ToString()); }
                else
                { Logger.dbgLog("idxlist count: " + idxList.Count + " lastsaveusedpacking: " + Loader.LastSaveUsedPacking.ToString()); }
            }

            TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
            int num = Mod.DEFAULT_TREE_COUNT; //262144

            //if (idxList != null && idxList.Count >= Mod.DEFAULT_TREE_COUNT) //custom one.
            if (idxList != null && idxList.Count > 1) //always use custom one unless problem.
            {
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("Start using packer seralizer for first 262144 realones."); }
                if (Mod.DEBUG_LOG_ON && idxList.Count < 262144) 
                { Logger.dbgLog(string.Format("tree seralizer will serialize first {0} trees then dummy up the rest.",idxList.Count)); }
                Loader.LastSaveUsedPacking = true;
                EncodedArray.UShort num1 = EncodedArray.UShort.BeginWrite(s);

                //remember idxlist index entry 0 is also zeroed'd to account for treebuffer starting at 1 not 0)
                for (int i = 1; i < num; i++)
                {
                    if (i < idxList.Count)
                    { num1.Write(mBuffer[idxList[i]].m_flags); }
                    else { num1.Write(0); }
                }
                num1.EndWrite();
                try
                {
                    PrefabCollection<TreeInfo>.BeginSerialize(s);
                    int limit = Math.Min(num, idxList.Count);
                    for (int j = 1; j < limit; j++)
                    {
                        if (mBuffer[idxList[j]].m_flags != 0)
                        {
                            PrefabCollection<TreeInfo>.Serialize(mBuffer[idxList[j]].m_infoIndex);
                        }
                    }

                    // The easiest (and only) way to create a dummy DataSerializer that writes to Stream.Null. This simply calls RefCounter.Serialize().
                    try
                    {
                        if (idxList.Count > limit)
                            DataSerializer.Serialize(System.IO.Stream.Null, DataSerializer.Mode.Memory, 0, new RefCounter(limit, idxList));
                    }
                    catch (Exception ex)
                    { Logger.dbgLog("RefCounter", ex, true); }
                }
                finally
                {
                    PrefabCollection<TreeInfo>.EndSerialize(s);
                }
                EncodedArray.Short num2 = EncodedArray.Short.BeginWrite(s);
                for (int k = 1; k < num; k++)
                {
                    if (k < idxList.Count)
                    {
                        if (mBuffer[idxList[k]].m_flags != 0)
                        {
                            num2.Write(mBuffer[idxList[k]].m_posX);
                        }
                    }
                }
                num2.EndWrite();
                EncodedArray.Short num3 = EncodedArray.Short.BeginWrite(s);
                for (int l = 1; l < num; l++)
                {
                    if (l < idxList.Count)
                    {
                        if (mBuffer[idxList[l]].m_flags != 0)
                        {
                            num3.Write(mBuffer[idxList[l]].m_posZ);
                        }
                    }
                }
                num3.EndWrite();
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("End using packer seralizer."); }

            }

            else //use original.
            {
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("Start using original seralizer."); }
                EncodedArray.UShort num1 = EncodedArray.UShort.BeginWrite(s);
                for (int i = 1; i < num; i++)
                {
                    num1.Write(mBuffer[i].m_flags);
                }
                num1.EndWrite();
                try
                {
                    PrefabCollection<TreeInfo>.BeginSerialize(s);
                    for (int j = 1; j < num; j++)
                    {
                        if (mBuffer[j].m_flags != 0)
                        {
                            PrefabCollection<TreeInfo>.Serialize(mBuffer[j].m_infoIndex);
                        }
                    }
                }
                finally
                {
                    PrefabCollection<TreeInfo>.EndSerialize(s);
                }
                EncodedArray.Short num2 = EncodedArray.Short.BeginWrite(s);
                for (int k = 1; k < num; k++)
                {
                    if (mBuffer[k].m_flags != 0)
                    {
                        num2.Write(mBuffer[k].m_posX);
                    }
                }
                num2.EndWrite();
                EncodedArray.Short num3 = EncodedArray.Short.BeginWrite(s);
                for (int l = 1; l < num; l++)
                {
                    if (mBuffer[l].m_flags != 0)
                    {
                        num3.Write(mBuffer[l].m_posZ);
                    }
                }
                num3.EndWrite();
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("End using original seralizer."); }
            }
           // Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndSerialize(s, "TreeManager");
        }

	}

    class RefCounter : IDataContainer
    {
        readonly List<int> idxList;
        readonly int startIndex;

        public RefCounter(int startIndex, List<int> idxList)
        {
            this.startIndex = startIndex;
            this.idxList = idxList;
        }

        // The given DataSerializer is the dummy one that writes to Stream.Null.
        public void Serialize(DataSerializer s)
        {
            Debug.Log("[UT] Serialize from " + startIndex.ToString() + " to " + idxList.Count.ToString());

            // Setup a dummy PrefabCollection<TreeInfo>.m_encodedArray so that we can continue refcounting.
            FieldInfo encoderField = typeof(PrefabCollection<TreeInfo>).GetField("m_encodedArray", BindingFlags.NonPublic | BindingFlags.Static);
            EncodedArray.UShort original = (EncodedArray.UShort) encoderField.GetValue(null);

            try
            {
                EncodedArray.UShort dummy = EncodedArray.UShort.BeginWrite(s);
                encoderField.SetValue(null, dummy);

                // Now that the do-nothing m_encodedArray has been set, we can safely continue refcounting.
                TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                int limit = idxList.Count;

                for (int j = startIndex; j < limit; j++)
                    if (mBuffer[idxList[j]].m_flags != 0)
                        PrefabCollection<TreeInfo>.Serialize(mBuffer[idxList[j]].m_infoIndex);
            }
            finally
            {
                encoderField.SetValue(null, original);
            }
        }

        public void AfterDeserialize(DataSerializer s) { }
        public void Deserialize(DataSerializer s) { }
    }
}
