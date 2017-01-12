using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System;
using System.Linq;

/// <summary>
/// Base class for items. Should not be accessed by non-ItemComponent classes.
/// </summary>
public class Item : MonoBehaviour
{

}

// For where it will be placed
public enum ItemSlotSize
{
    None = 0, // Can't be carried
    Small = 1, // Pouch
    Medium = 2, // Belt - Always weapon
    Large = 3 // Back - Always weapon
}

public enum ItemState
{
    // Held in hand
    Held,
    // In a quick/weapon slot
    Slot,
    // Stored in inventory
    Inventory,
    // In world 
    World
}