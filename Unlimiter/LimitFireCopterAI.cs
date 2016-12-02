using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Reflection;
using UnityEngine;

namespace TreeUnlimiter
{
	internal static class LimitFireCopterAI
	{
        private static uint FindBurningTree(Vector3 pos, float maxDistance)
        {
            TreeManager instance = Singleton<TreeManager>.instance;
            int num = Mathf.Max((int)((pos.x - maxDistance) / 32f + 270f), 0);
            int num2 = Mathf.Max((int)((pos.z - maxDistance) / 32f + 270f), 0);
            int num3 = Mathf.Min((int)((pos.x + maxDistance) / 32f + 270f), 539);
            int num4 = Mathf.Min((int)((pos.z + maxDistance) / 32f + 270f), 539);
            float num5 = maxDistance * maxDistance;
            uint result = 0u;
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    uint num6 = instance.m_treeGrid[i * 540 + j];
                    int num7 = 0;
                    while (num6 != 0u)
                    {
                        global::TreeInstance.Flags flags = (global::TreeInstance.Flags)instance.m_trees.m_buffer[(int)((UIntPtr)num6)].m_flags;
                        if ((flags & global::TreeInstance.Flags.Burning) != global::TreeInstance.Flags.None && instance.m_trees.m_buffer[(int)((UIntPtr)num6)].GrowState != 0)
                        {
                            Vector3 position = instance.m_trees.m_buffer[(int)((UIntPtr)num6)].Position;
                            float num8 = VectorUtils.LengthSqrXZ(position - pos);
                            if (num8 < num5)
                            {
                                result = num6;
                                num5 = num8;
                            }
                        }
                        num6 = instance.m_trees.m_buffer[(int)((UIntPtr)num6)].m_nextGridTree;
                        if (++num7 >= LimitTreeManager.Helper.TreeLimit)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            return result;
        }
    }
}
