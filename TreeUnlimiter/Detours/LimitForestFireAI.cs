using System;
using ColossalFramework;
using TreeUnlimiter.RedirectionFramework.Attributes;
using UnityEngine;

namespace TreeUnlimiter.Detours
{
    [TargetType(typeof(ForestFireAI))]
	public class LimitForestFireAI
	{
        [RedirectMethod]
        private static uint FindClosestTree(Vector3 pos)
        {
            TreeManager instance = Singleton<TreeManager>.instance;
            int num = Mathf.Max((int)(pos.x / 32f + 270f), 0);
            int num2 = Mathf.Max((int)(pos.z / 32f + 270f), 0);
            int num3 = Mathf.Min((int)(pos.x / 32f + 270f), 539);
            int num4 = Mathf.Min((int)(pos.z / 32f + 270f), 539);
            int num5 = num + 1;
            int num6 = num2 + 1;
            int num7 = num3 - 1;
            int num8 = num4 - 1;
            uint num9 = 0u;
            float num10 = 1E+12f;
            float num11 = 0f;
            while (num != num5 || num2 != num6 || num3 != num7 || num4 != num8)
            {
                for (int i = num2; i <= num4; i++)
                {
                    for (int j = num; j <= num3; j++)
                    {
                        if (j >= num5 && i >= num6 && j <= num7 && i <= num8)
                        {
                            j = num7;
                        }
                        else
                        {
                            uint num12 = instance.m_treeGrid[i * 540 + j];
                            int num13 = 0;
                            while (num12 != 0u)
                            {
                                if ((instance.m_trees.m_buffer[(int)((UIntPtr)num12)].m_flags & 67) == 1 && instance.m_trees.m_buffer[(int)((UIntPtr)num12)].GrowState != 0)
                                {
                                    Vector3 position = instance.m_trees.m_buffer[(int)((UIntPtr)num12)].Position;
                                    float num14 = Vector3.SqrMagnitude(position - pos);
                                    if (num14 < num10)
                                    {
                                        num9 = num12;
                                        num10 = num14;
                                    }
                                }
                                num12 = instance.m_trees.m_buffer[(int)((UIntPtr)num12)].m_nextGridTree;
                                if (++num13 >= LimitTreeManager.Helper.TreeLimit)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                    break;
                                }
                            }
                        }
                    }
                }
                if (num9 != 0u && num10 <= num11 * num11)
                {
                    return num9;
                }
                num11 += 32f;
                num5 = num;
                num6 = num2;
                num7 = num3;
                num8 = num4;
                num = Mathf.Max(num - 1, 0);
                num2 = Mathf.Max(num2 - 1, 0);
                num3 = Mathf.Min(num3 + 1, 539);
                num4 = Mathf.Min(num4 + 1, 539);
            }
            return num9;
        }
	}
}
