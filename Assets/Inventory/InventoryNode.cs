using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A node of the inventory grid.
/// If parent is not null, children and heldItem are null, and vice versa.
/// </summary>
public class InventoryNode
{
    // The node containing the item taking up this slot
    InventoryNode parent;

    // The other nodes being taken up by this item
    InventoryNode[] children;

    // The item stored in this slot
    Storeable item;
    

    /// <summary>
    /// Stores a Storeable item in this node, taking up the child nodes as well.
    /// </summary>
	public void StoreItem(Storeable item, InventoryNode[] children)
    {
        this.children = children;
        this.item = item;
        foreach(InventoryNode n in children)
        {
            n.SetParent(this);
        }
    }


    /// <summary>
    /// Removes the item from the node cluster and returns it,
    /// clearing all nodes in the process.
    /// </summary>
    /// <returns>The stored item, if any</returns>
    public Storeable RemoveItem()
    {
        if(IsEmpty())
        {
            return null;
        }

        // Is a child, so we have the parent remove the item and clear us.
        if (item == null)
        {
            return parent.RemoveItem();
        }


        // We are the parent, so we clear all children.
        foreach (InventoryNode n in children)
        {
            n.Clear();
        }

        var temp = item;

        // Clear self
        Clear();

        return temp;
    }


    /// <summary>
    /// Swaps the item with the given one and returns the old item, if any.
    /// </summary>
    public Storeable SwapItem(Storeable item, InventoryNode[] children)
    {
        Storeable oldItem = null;
        if (PeekItem() != null)
            oldItem = RemoveItem();

        StoreItem(item, children);

        return oldItem;
    }


    /// <summary>
    /// Returns the item without removing it.
    /// </summary>
    public Storeable PeekItem()
    {
        if (IsEmpty()) return null;

        if (item == null)
            return parent.PeekItem();

        return item;
    }


    /// <summary>
    /// Checks if a node is empty.
    /// </summary>
    public bool IsEmpty()
    {
        return (parent == null && item == null);
    }


    /// <summary>
    /// Sets the parent of this node.
    /// </summary>
    private void SetParent(InventoryNode parent)
    {
        this.parent = parent;
    }


    /// <summary>
    /// Clears a node and makes it ready for use
    /// </summary>
    private void Clear()
    {
        item = null;
        children = null;
        parent = null;
    }

    public override string ToString()
    {
        if (IsEmpty()) return "Empty";

        if(item == null)
        {
            return parent.ToString();
        }
        return item.name;
    }
}
