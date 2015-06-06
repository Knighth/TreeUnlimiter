using ColossalFramework;
using System;
using UnityEngine;

namespace TreeUnlimiter
{
	internal static class LimitNaturalResourceManager
	{
		private static void TreesModified(NaturalResourceManager nrm, Vector3 position)
		{
			unsafe
			{
				int num = Mathf.Clamp((int)((double)position.x / 33.75 + 256), 0, 511);
				int num1 = Mathf.Clamp((int)((double)position.z / 33.75 + 256), 0, 511);
				float single = (float)(((double)num - 256) * 33.75);
				float single1 = (float)(((double)num1 - 256) * 33.75);
				float single2 = (float)(((double)(num + 1) - 256) * 33.75);
				float single3 = (float)(((double)(num1 + 1) - 256) * 33.75);
				int num2 = Mathf.Max((int)((double)single / 32 + 270), 0);
				int num3 = Mathf.Max((int)((double)single1 / 32 + 270), 0);
				int num4 = Mathf.Min((int)((double)single2 / 32 + 270), 539);
				int num5 = Mathf.Min((int)((double)single3 / 32 + 270), 539);
				TreeManager treeManager = Singleton<TreeManager>.instance;
				int num6 = 0;
				int growState = 0;
				for (int i = num3; i <= num5; i++)
				{
					for (int j = num2; j <= num4; j++)
					{
						uint mTreeGrid = treeManager.m_treeGrid[i * 540 + j];
						int num7 = 0;
						while (mTreeGrid != 0)
						{
							if ((treeManager.m_trees.m_buffer[mTreeGrid].m_flags & 3) == 1)
							{
								Vector3 vector3 = treeManager.m_trees.m_buffer[mTreeGrid].Position;
								if ((double)vector3.x >= (double)single && (double)vector3.z >= (double)single1 && (double)vector3.x <= (double)single2 && (double)vector3.z <= (double)single3)
								{
									num6 = num6 + 15;
									growState = growState + treeManager.m_trees.m_buffer[mTreeGrid].GrowState;
								}
							}
							mTreeGrid = treeManager.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
							int num8 = num7 + 1;
							num7 = num8;
							if (num8 < LimitTreeManager.Helper.TreeLimit)
							{
								continue;
							}
							CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
							break;
						}
					}
				}
				byte num9 = (byte)Mathf.Min(num6 * 4, 255);
				byte num10 = (byte)Mathf.Min(growState * 4, 255);
				NaturalResourceManager.ResourceCell mNaturalResources = nrm.m_naturalResources[num1 * 512 + num];
				if (num9 == mNaturalResources.m_forest && num10 == mNaturalResources.m_tree)
				{
					return;
				}
				bool mForest = num9 != mNaturalResources.m_forest;
				mNaturalResources.m_forest = num9;
				mNaturalResources.m_tree = num10;
				nrm.m_naturalResources[num1 * 512 + num] = mNaturalResources;
				if (!mForest)
				{
					return;
				}
				nrm.AreaModified(num, num1, num, num1);
			}
		}
	}
}