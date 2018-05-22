using System;
using ColossalFramework;
using ColossalFramework.Math;
using TreeUnlimiter.RedirectionFramework.Attributes;
using UnityEngine;

namespace TreeUnlimiter.Detours
{
    [TargetType(typeof(FireCopterAI))]
	public class LimitFireCopterAI
	{
        [RedirectMethod]
        private static uint FindBurningTree(int seed, Vector3 pos, float maxDistance, Vector3 priorityPos)
        {
            TreeManager instance = Singleton<TreeManager>.instance;
            int num = Mathf.Max((int)((pos.x - maxDistance) / 32f + 270f), 0);
            int num2 = Mathf.Max((int)((pos.z - maxDistance) / 32f + 270f), 0);
            int num3 = Mathf.Min((int)((pos.x + maxDistance) / 32f + 270f), 539);
            int num4 = Mathf.Min((int)((pos.z + maxDistance) / 32f + 270f), 539);
            float num5 = maxDistance * maxDistance;
            int num6 = 1000000000;
            uint result = 0u;
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    uint num7 = instance.m_treeGrid[i * 540 + j];
                    int num8 = 0;
                    while (num7 != 0u)
                    {
                        global::TreeInstance.Flags flags = (global::TreeInstance.Flags)instance.m_trees.m_buffer[(int)((UIntPtr)num7)].m_flags;
                        if ((flags & global::TreeInstance.Flags.Burning) != global::TreeInstance.Flags.None && instance.m_trees.m_buffer[(int)((UIntPtr)num7)].GrowState != 0)
                        {
                            Vector3 position = instance.m_trees.m_buffer[(int)((UIntPtr)num7)].Position;
                            float num9 = VectorUtils.LengthSqrXZ(position - pos);
                            if (num9 < num5)
                            {
                                Randomizer randomizer = new Randomizer((uint)(seed ^ (int)num7));
                                int num10 = Mathf.RoundToInt(VectorUtils.LengthXZ(position - priorityPos));
                                num10 = randomizer.Int32(num10 >> 1, num10);
                                if (num10 < num6)
                                {
                                    result = num7;
                                    num6 = num10;
                                }
                            }
                        }
                        num7 = instance.m_trees.m_buffer[(int)((UIntPtr)num7)].m_nextGridTree;
                        if (++num8 >= LimitTreeManager.Helper.TreeLimit)
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
