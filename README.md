# Unlimited Trees Mod fork

My changes are in Packer.cs.

I have identified the following issue:

Packer.Serialize() calls PrefabCollection<TreeInfo>.Serialize() for the first 262144 trees.
PrefabCollection<TreeInfo>.Serialize() is a simple method:

	public static void Serialize(uint index)
	{
		PrefabCollection<T>.m_encodedArray.Write((ushort)index);
		PrefabCollection<T>.PrefabData[] buf = PrefabCollection<T>.m_simulationPrefabs.m_buffer;
		buf[(int)index].m_refcount = buf[(int)index].m_refcount + 1;
	}

The first line is fine. It must be executed for the first 262144 trees only.

However, the latter part (refcounting) should cover all trees. Refcounting "locks" the infos that are
currently used so that when the save is loaded again, infoIndexes will point to the same infos, not
some arbitrary ones.

I have written a fix for this issue. In the fix, the above method is called for the rest of the
trees. I setup a dummy do-nothing m_encodedArray first in order not to affect serialization.
My solution looks a bit weird because there are private structs, fields and constructors blocking
access. At least it runs fast (no reflection in the loop).

The following test procedure verifies that this issue is real:

1. Disable almost all mods. I had just UT, Unlock All, 25 Spaces, and Extra Landscaping enabled.
2. Enable just a few custom trees (not all you have!)
3. Start a new game.
4. Fill the map with at least 262144 *standard* trees. I saved in between to pack them, to make
   sure the 262144 slots are used.
5. Place a few custom trees and memorize them.
6. Save and Exit.
7. In Content Manager, enable several more custom trees.
8. Load the savegame.
9. Notice that the custom trees have been replaced by other trees. I suppose null references
   are also possible.

Repeat with the suggested fix. The issue is gone.
