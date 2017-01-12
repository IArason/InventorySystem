using UnityEngine;
using System.Collections;

public class InventoryUINode : MonoBehaviour {

    private InventoryUI owner;
    private int x;
    private int y;

    bool hover = false;

    public void Initialize(InventoryUI owner, int x, int y)
    {
        this.owner = owner;
        this.x = x;
        this.y = y;
    }

    void Update()
    {
        if(hover)
            owner.Hover(x, y);
    }

    /// <summary>
    /// Called on mouse entry.
    /// </summary>
	public void OnMouseEnter()
    {
        hover = true;
    }

    /// <summary>
    /// Called on mouse exit.
    /// </summary>
    public void OnMouseExit()
    {
        hover = false;
    }

    /// <summary>
    /// Called when mouse is released within the same UI element.
    /// </summary>
    public void OnMouseClick()
    {
        owner.Click(x, y);
    }
}
