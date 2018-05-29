using System;
using ColossalFramework;
using ColossalFramework.Math;
using TreeUnlimiter.RedirectionFramework.Attributes;
using UnityEngine;

namespace TreeUnlimiter.Detours
{
    [TargetType(typeof(DisasterHelpers))]
    public class LimitDisasterHelpers
    {
        [RedirectMethod]
        private static void DestroyTrees(int seed, InstanceManager.Group group, Vector3 position, float totalRadius, float removeRadius, float destructionRadiusMin, float destructionRadiusMax, float burnRadiusMin, float burnRadiusMax)
        {
            int num = Mathf.Max((int)((position.x - totalRadius) / 32f + 270f), 0);
            int num2 = Mathf.Max((int)((position.z - totalRadius) / 32f + 270f), 0);
            int num3 = Mathf.Min((int)((position.x + totalRadius) / 32f + 270f), 539);
            int num4 = Mathf.Min((int)((position.z + totalRadius) / 32f + 270f), 539);
            Array32<global::TreeInstance> trees = Singleton<TreeManager>.instance.m_trees;
            uint[] treeGrid = Singleton<TreeManager>.instance.m_treeGrid;
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    uint num5 = treeGrid[i * 540 + j];
                    int num6 = 0;
                    while (num5 != 0u)
                    {
                        uint nextGridTree = trees.m_buffer[(int)((UIntPtr)num5)].m_nextGridTree;
                        global::TreeInstance.Flags flags = (global::TreeInstance.Flags)trees.m_buffer[(int)((UIntPtr)num5)].m_flags;
                        if ((flags & (global::TreeInstance.Flags.Created | global::TreeInstance.Flags.Deleted)) == global::TreeInstance.Flags.Created)
                        {
                            Vector3 position2 = trees.m_buffer[(int)((UIntPtr)num5)].Position;
                            float num7 = VectorUtils.LengthXZ(position2 - position);
                            if (num7 < totalRadius)
                            {
                                Randomizer randomizer = new Randomizer(num5 | (uint)((uint)seed << 16));
                                float num8 = (burnRadiusMax - num7) / Mathf.Max(1f, burnRadiusMax - burnRadiusMin);
                                bool flag = num7 < removeRadius;
                                bool flag2 = (float)randomizer.Int32(1000u) < num8 * 1000f;
                                if (flag)
                                {
                                    Singleton<TreeManager>.instance.ReleaseTree(num5);
                                }
                                else if (flag2 && (flags & global::TreeInstance.Flags.FireDamage) == global::TreeInstance.Flags.None)
                                {
                                    Singleton<TreeManager>.instance.BurnTree(num5, group, 128);
                                }
                            }
                        }
                        num5 = nextGridTree;
                        if (++num6 >= LimitTreeManager.Helper.TreeLimit)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
        }    
    }

}
