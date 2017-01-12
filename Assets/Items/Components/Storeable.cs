using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class Storeable : MonoBehaviour {

    [Header("Inventory grid size")]
    public int x = 1;
    public int y = 1;

    [SerializeField]
    private Sprite sprite;
    public Sprite Sprite { get { return sprite; } }

    // If true, swap x and y values and rotate sprite 90 degrees.
    private bool rotated = false;
    public bool Rotated { get { return rotated; } }

    /// <summary>
    /// Rotates the item in inventory.
    /// </summary>
    public void Rotate()
    {
        int tmp = x;
        x = y;
        y = tmp;
        rotated = !rotated;
    }
    
    void OnStateChange(ItemState state)
    {
        if (state == ItemState.Inventory)
            gameObject.SetActive(false);
        else
            gameObject.SetActive(true);
    }
    
    public void SetState(ItemState state)
    {
        OnStateChange(state);
    }

    public void Drop()
    {
        Debug.Log("Dropped");
        OnStateChange(ItemState.World);
    }

}

public enum StoreType
{
    Generic = 3,
    SmallWeapon = 2,
    LargeWeapon = 1
}