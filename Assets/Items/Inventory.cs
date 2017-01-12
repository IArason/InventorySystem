using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class Inventory : MonoBehaviour
{
    #region Exposed Variables

    [SerializeField, Tooltip("Reference to the UI counterpart script.")]
    private InventoryUI ui;
    [SerializeField, Tooltip("Instantiated items to be added at start.")]
    private List<Storeable> defaultItems = new List<Storeable>();
    [SerializeField]
    private bool openOnStart = false;
    [SerializeField, Tooltip("Dropped items will be placed on this transform.")]
    private Transform dropPosition;
    [SerializeField, Tooltip("Should rotating items be allowed?")]
    bool canRotateItems = true;

    #endregion

    #region Private Variabels

    // The nodes of the inventory grid
    private InventoryNode[,] inventory;
    // Item being held in the cursor
    private Storeable itemInCursor = null;
    // Contains items and their rects. Used for UI display.
    private Dictionary<Storeable, IntRect> storedItems = new Dictionary<Storeable, IntRect>();
    // Is inventory in use?
    private bool active = false;

    #endregion
    
    #region Editor Variables

    [SerializeField, HideInInspector]
    int inventorySizeX = 6;
    [SerializeField, HideInInspector]
    int inventorySizeY = 5;
    [SerializeField, HideInInspector]
    bool[] activeNodes;

    #endregion

    #region Callbacks

    void Awake()
    {
        Initialize();
        OpenInventory();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            if (active)
                CloseInventory();
            else
                OpenInventory();
        }
    }

    void LateUpdate()
    {
        if(active)
        {
            ui.UpdateUI(storedItems, itemInCursor);

            // On right click, rotate item 90 degrees.
            if (canRotateItems && 
                Input.GetMouseButtonDown(1) && 
                itemInCursor != null)
            {
                itemInCursor.Rotate();
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Enables the inventory canvas.
    /// </summary>
    public void OpenInventory()
    {
        active = true;
        ui.gameObject.SetActive(true);
    }

    /// <summary>
    /// Disables the inventory canvas.
    /// </summary>
    public void CloseInventory()
    {
        active = false;
        ui.gameObject.SetActive(false);
    }

    /// <summary>
    /// Checks if an item can be stored in the inventory.
    /// </summary>
    public bool CanStoreItem(Storeable item)
    {
        return GetEmptyRect(item) != null;
    }

    /// <summary>
    /// Checks if an item can be stored at the given coordinate using item's center.
    /// </summary>
    public bool CanStoreItemAtClicked(Storeable item, int x, int y)
    {
        return CanSwap(item, x, y);
    }

    /// <summary>
    /// Tries to find a fitting grid location to store the item.
    /// If none is found, returns false.
    /// </summary>
    public bool StoreItem(Storeable item)
    {
        IntRect storageRect = GetEmptyRect(item);
        if (storageRect == null)
        {
            Debug.Log("Can't put away item");
            return false;
        }

        // Used to store the child nodes -- -1 for the parent
        List<InventoryNode> children = new List<InventoryNode>();
        InventoryNode parent = null;
        
        for (int x = storageRect.xOrigin; x < storageRect.xOrigin + storageRect.xSize; x++)
        {
            for (int y = storageRect.yOrigin; y < storageRect.yOrigin + storageRect.ySize; y++)
            {
                // If the current node is the integer center of the grid, make it the parent
                // otherwise it's a child
                if (x == storageRect.xOrigin + (storageRect.xSize / 2) && y == storageRect.yOrigin + (storageRect.ySize / 2))
                {
                    parent = inventory[x, y];
                }
                else
                {
                    children.Add(inventory[x, y]);
                }
            }
        }

        // Add item to dictionary
        if(item != null)
            storedItems.Add(item, storageRect);

        // Parent takes care of activating the children.
        parent.StoreItem(item, children.ToArray());

        item.SetState(ItemState.Inventory);

        return true;
    }

    /// <summary>
    /// Attempts to store an item at the cursor's click location.
    /// If possible, replaces item with cursor item.
    /// Returns false if unsuccessful.
    /// </summary>
    public bool StoreCursorItemAt(int x, int y)
    {
        // If cursor is empty, just empty the node clicked.
        if (itemInCursor == null)
        {
            itemInCursor = RemoveItemAt(x, y);
            return true;
        }

        var rect = InverseGetRectCenter(itemInCursor, x, y);
        if(CanSwap(itemInCursor, x, y))
        {
            List<InventoryNode> children = new List<InventoryNode>();
            // Grab all nodes in rect
            for (int i = rect.xOrigin; i < rect.xSize + rect.xOrigin; i++)
            {
                for (int j = rect.yOrigin; j < rect.ySize + rect.yOrigin; j++)
                {
                    children.Add(inventory[i, j]);
                }
            }

            InventoryNode parent = null;

            // Scan through and look for non-empty node to use as parent.
            for(int i = 0; i < children.Count; i++)
            {
                if(children[i] != null && !children[i].IsEmpty())
                {
                    parent = children[i];
                    children.RemoveAt(i);
                    break;
                }
            }

            // If no occupied nodes were found, swap it for this.
            if(parent == null)
            {
                parent = children[0];
                children.RemoveAt(0);
            }


            // Store old item in cursor, now placed in the grid.
            if(itemInCursor != null)
                storedItems.Add(itemInCursor, rect);
            itemInCursor = parent.SwapItem(itemInCursor, children.ToArray());

            // Remove the current one, now removed from the grid.
            if(itemInCursor != null)
                storedItems.Remove(itemInCursor);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the area which should be highlighted on hover at x, y.
    /// </summary>
    public IntRect HighlightOnHover(int x, int y)
    {
        // If empty, just highlight one square.
        if (itemInCursor == null) return new IntRect(x, y, 1, 1);

        return InverseGetRectCenter(itemInCursor, x, y);
    }

    /// <summary>
    /// Called by UI when inventory node is clicked.
    /// </summary>
    public void ClickedNode(int x, int y)
    {
        StoreCursorItemAt(x, y);
    }

    /// <summary>
    /// Drops the current cursor item.
    /// </summary>
    public void DropCursorItem()
    {
        if (itemInCursor != null)
        {
            itemInCursor.Drop();

            itemInCursor.transform.position = dropPosition.position;
            itemInCursor.transform.rotation = dropPosition.rotation;

            itemInCursor = null;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the inventory.
    /// </summary>
    private void Initialize()
    {
        GenerateInventory(inventorySizeX, inventorySizeY, activeNodes);

        ui.InitializeUI(inventory, this);

        if (!openOnStart) CloseInventory();

        AddDefaultItems();
    }

    /// <summary>
    /// Generates the inventory datastructures using data set through the inspector.
    /// </summary>
    private void GenerateInventory(int xSize, int ySize, bool[] nodes)
    {
        inventory = new InventoryNode[inventorySizeX, inventorySizeY];
        for (int i = 0; i < inventorySizeX; i++)
        {
            for (int j = 0; j < inventorySizeY; j++)
            {
                if (activeNodes[i + j * inventorySizeX])
                {
                    inventory[i, j] = new InventoryNode();
                }
            }
        }
    }

    /// <summary>
    /// Gets a rect surrounding the item's center node.
    /// </summary>
    private IntRect GetRectCenter(Storeable item, int x, int y)
    {
        IntRect r = new IntRect(x, y, item.x, item.y);

        r.xOrigin += (item.x - 1) / 2;
        r.yOrigin += (item.y - 1) / 2;

        return r;
    }

    /// <summary>
    /// Gets origin using item's center
    /// </summary>
    private IntRect InverseGetRectCenter(Storeable item, int x, int y)
    {
        IntRect r = new IntRect(x, y, item.x, item.y);

        r.xOrigin -= (item.x - 1) / 2;
        r.yOrigin -= (item.y - 1) / 2;

        return r;
    }

    /// <summary>
    /// Checks if an item can be stored with center x, y
    /// </summary>
    private bool CanSwap(Storeable item, int x, int y)
    {
        // If you have nothing taking up space, you can simply swap item with null.
        if (item == null) return true;

        var itemRect = InverseGetRectCenter(item, x, y);

        if (RectOutOfRange(itemRect))
        {
            return false;
        }

        // If it's empty or only one item is in the area, we can swap.
        return NumberOfItemsIn(itemRect) < 2;
    }

    /// <summary>
    /// Gets an empty rect an item can fit in.
    /// </summary>
    private IntRect GetEmptyRect(Storeable item)
    {
        // Start at upper left
        for (int x = 0; x < inventory.GetLength(0) - item.x + 1; x++)
        {
            for (int y = 0; y < inventory.GetLength(1) - item.y + 1; y++)
            {
                if (CheckRectEmpty(x, y, item.x, item.y))
                {
                    return new IntRect(x, y, item.x, item.y);
                }
            }
        }

        // Try again after rotating item.
        item.Rotate();
        for (int x = 0; x < inventory.GetLength(0) - item.x + 1; x++)
        {
            for (int y = 0; y < inventory.GetLength(1) - item.y + 1; y++)
            {
                if (CheckRectEmpty(x, y, item.x, item.y))
                {
                    return new IntRect(x, y, item.x, item.y);
                }
            }
        }

        // Rotate back if failed.
        item.Rotate();
        return null;
    }

    /// <summary>
    /// Checks the number of unique items present in a rect.
    /// </summary>
    private int NumberOfItemsIn(IntRect rect)
    {
        List<Storeable> items = new List<Storeable>();
        for (int i = rect.xOrigin; i < rect.xSize + rect.xOrigin; i++)
        {
            for (int j = rect.yOrigin; j < rect.ySize + rect.yOrigin; j++)
            {
                if(inventory[i, j] != null && inventory[i, j].PeekItem() != null)
                {
                    if (!items.Contains(inventory[i, j].PeekItem()))
                    {
                        items.Add(inventory[i, j].PeekItem());
                    }
                }
            }
        }
        return items.Count;
    }

    /// <summary>
    /// Checks if a rect is out of bounds or covers any inactive nodes.
    /// </summary>
    private bool RectOutOfRange(IntRect rect)
    {
        if(rect.xOrigin < 0 ||
            rect.yOrigin < 0 ||
            rect.xSize + rect.xOrigin - 1 >= inventory.GetLength(0) ||
            rect.ySize + rect.yOrigin - 1 >= inventory.GetLength(1))
        {
            return true;
        }

        // Checks for null nodes in rect.
        for (int i = rect.xOrigin; i < rect.xSize + rect.xOrigin; i++)
        {
            for (int j = rect.yOrigin; j < rect.ySize + rect.yOrigin; j++)
            {
                if (inventory[i, j] == null)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if a rectangle is valid for item storage.
    /// </summary>
    private bool CheckRectEmpty(IntRect rect)
    {
        return (CheckRectEmpty(rect.xOrigin, rect.yOrigin, rect.xSize, rect.ySize));
    }

    private bool CheckRectEmpty(int xOrigin, int yOrigin, int xSize, int ySize)
    {
        for (int i = xOrigin; i < xSize + xOrigin; i++)
        {
            for (int j = yOrigin; j < ySize + yOrigin; j++)
            {
                if (inventory[i, j] == null || !inventory[i, j].IsEmpty())
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Removes the item occupying a given node from the inventory and returns it.
    /// </summary>
    private Storeable RemoveItemAt(int x, int y)
    {
        var tmp = inventory[x, y].RemoveItem();
        if(tmp != null)
            storedItems.Remove(tmp);
        return tmp;
    }

    /// <summary>
    /// Adds the items in the "defaultItems" list
    /// </summary>
    private void AddDefaultItems()
    {
        for (int i = 0; i < defaultItems.Count; i++)
        {
            StoreItem(defaultItems[i]);
        }
    }

    #endregion

    /// <summary>
    /// UnityEngine.Rect but with integers.
    /// Used for representing object dimensions and location in the inventory grid.
    /// </summary>
    public class IntRect
    {
        public int xOrigin;
        public int yOrigin;
        public int xSize;
        public int ySize;

        public IntRect() { }

        public IntRect(int xOrigin, int yOrigin, int xSize, int ySize)
        {
            this.xOrigin = xOrigin;
            this.yOrigin = yOrigin;
            this.xSize = xSize;
            this.ySize = ySize;
        }

        public override string ToString()
        {
            return "Origin: " + xOrigin + ", " + yOrigin + " | Size: (" + xSize + ", " + ySize + ")";
        }
    }
}