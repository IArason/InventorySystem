using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(Canvas))]
public class InventoryUI : MonoBehaviour {

    #region Exposed Variables

    [SerializeField, Tooltip("Prefab of the inventory node")]
    private GameObject gridButtonPrefab;
    [SerializeField, Tooltip("The nodes will be centered on this transform.")]
    private RectTransform inventoryNodeAnchor;
    [SerializeField, Tooltip("Margin between nodes")]
    private float nodeSpacing = 3f;

    #endregion

    #region Private Variables

    // The inventory UI nodes
    private RectTransform[,] inventoryGrid;

    // Holds the sprites of the stored objects.
    private Dictionary<Storeable, GameObject> spriteObjects = new Dictionary<Storeable, GameObject>();
    // Item currently "held" by the cursor
    private Storeable cursorItem;
    private RectTransform cursorSprite;
    // Is the held item rotated?
    private bool cursorRotated = false;

    // Intervals between nodes.
    private float xInterval;
    private float yInterval;

    private Inventory inventory;
    private Vector2 mousePos;
    private Canvas canvas;

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the UI.
    /// </summary>
    public void InitializeUI (InventoryNode[,] nodes, Inventory inventory)
    {
        this.inventory = inventory;

        canvas = GetComponent<Canvas>();

        // Transform InventoryNode[,] into RectTransform[,] and populate with UI elements.
        inventoryGrid = new RectTransform[nodes.GetLength(0), nodes.GetLength(1)];

        for (int i = 0; i < nodes.GetLength(0); i++)
        {
            for (int j = 0; j < nodes.GetLength(1); j++)
            {
                // Ignore empty nodes
                if (nodes[i, j] == null) continue;

                var node = Instantiate(gridButtonPrefab);
                node.transform.SetParent(transform, false);

                var rt = node.GetComponent<RectTransform>();

                xInterval = rt.sizeDelta.x + nodeSpacing;
                yInterval = rt.sizeDelta.y + nodeSpacing;

                // Spawn in a grid starting from top left
                rt.anchoredPosition = 
                    new Vector2(
                        inventoryNodeAnchor.anchoredPosition.x - nodes.GetLength(0) / 2f * xInterval + xInterval * (i + 0.5f),
                        inventoryNodeAnchor.anchoredPosition.y + nodes.GetLength(1) / 2f * yInterval - yInterval * (j + 0.5f)
                    );

                rt.name = "Node (" + i + ", " + j +")";

                inventoryGrid[i, j] = rt;

                // Initialize the UI node.
                node.GetComponent<InventoryUINode>().Initialize(this, i, j);
            }
        }
    }

    /// <summary>
    /// Called every Update() from Inventory if inventory active.
    /// </summary>
    /// <param name="items">The items stored in the inventory and their rects.</param>
    public void UpdateUI(Dictionary<Storeable, Inventory.IntRect> items, Storeable cursorItem)
    {
        // Search for new entries
        foreach (KeyValuePair<Storeable, Inventory.IntRect> p in items)
        {
            // If key doesn't already exists
            if (!spriteObjects.ContainsKey(p.Key))
            {
                GenerateStoredSprites(p.Key, p.Value);
            }
        }

        // Clear old entires
        List<Storeable> toRemove = new List<Storeable>();
        foreach (KeyValuePair<Storeable, GameObject> p in spriteObjects)
        {
            // If key no longer exists in item list, remove it
            if (!items.ContainsKey(p.Key))
            {
                toRemove.Add(p.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            // Destory sprite
            Destroy(spriteObjects[toRemove[i]]);
            // Clear item from list
            spriteObjects.Remove(toRemove[i]);
        }

        if (this.cursorItem != cursorItem || (cursorItem != null && cursorItem.Rotated != cursorRotated))
        {
            UpdateCursorItem(cursorItem);
        }

        // Just in case, not concrete
        if (cursorSprite == null || cursorItem == null)
        {
            return;
        }

        var posOnCanvas = GetRelativeMousePosition();

        // Add 0.25 increment to move it to the object's integer center.
        if (cursorItem.x % 2 == 0) { posOnCanvas.x += xInterval * 0.5f; }
        if (cursorItem.y % 2 == 0) { posOnCanvas.y -= yInterval * 0.5f; }
        
        cursorSprite.anchoredPosition = posOnCanvas;
    }

    /// <summary>
    /// Button at x, y was clicked. Called by nodes.
    /// </summary>
    public void Click(int x, int y)
    {
        inventory.ClickedNode(x, y);
    }

    /// <summary>
    /// Called by UI buttons covering the drop area.
    /// </summary>
    public void DropCursorItem()
    {
        inventory.DropCursorItem();
    }

    /// <summary>
    /// Button at x, y is being hovered over. Called via nodes.
    /// </summary>
    public void Hover(int x, int y)
    {
        // Get the rect to highlight
        var rect = inventory.HighlightOnHover(x, y);
        
        for(int i = rect.xOrigin; i < rect.xOrigin + rect.xSize; i++)
        {
            for (int j = rect.yOrigin; j < rect.yOrigin + rect.ySize; j++)
            {
                EventSystem.current.SetSelectedGameObject(inventoryGrid[x, y].gameObject);
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns the cursor position on the canvas.
    /// </summary>
    private Vector2 GetRelativeMousePosition()
    {
        Vector2 hitLocation = new Vector2();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            var plane = new Plane(inventoryNodeAnchor.position, inventoryNodeAnchor.forward);
            float dist;
            if (plane.Raycast(ray, out dist))
            {
                hitLocation = ray.origin + ray.direction * dist;
                return transform.InverseTransformDirection(hitLocation);
            }
            return Vector3.zero;
        }
        else
        {
            return Input.mousePosition - new Vector3(Screen.width / 2, Screen.height / 2);
        }
    }

    /// <summary>
    /// Highlights the grid box at coordinates (x, y)
    /// </summary>
    private void Highlight(int x, int y)
    {
        if(x >= 0 && y >= 0 && x < inventoryGrid.GetLength(0) && x < inventoryGrid.GetLength(1))
        {
            if (inventoryGrid[x, y] != null)
                inventoryGrid[x, y].GetComponent<Button>().Select();
        }
    }

    /// <summary>
    /// Generates the sprite of an item and places it in the right area.
    /// </summary>
    private void GenerateStoredSprites(Storeable item, Inventory.IntRect rect)
    {
        var spriteGO = new GameObject();
        spriteGO.name = item.name;
        var sprite = spriteGO.AddComponent<Image>();
        sprite.sprite = item.Sprite;
        sprite.raycastTarget = false;
        

        var rt = spriteGO.GetComponent<RectTransform>();
        rt.SetParent(transform, false);

        if (item.Rotated)
            rt.Rotate(0, 0, 90);
        
        // Get upper left node x
        float xCenter = inventoryNodeAnchor.anchoredPosition.x - inventoryGrid.GetLength(0) / 2f * xInterval;
        // Count up to xOrigin + xSize/2 (center)
        xCenter += (rect.xOrigin + rect.xSize / 2f) * xInterval;

        // Get upper left node y
        float yCenter = inventoryNodeAnchor.anchoredPosition.y + inventoryGrid.GetLength(1) / 2f * yInterval;
        // Count down to yOrigin + ySize/2 (center)
        yCenter -= (rect.yOrigin + rect.ySize / 2f) * yInterval;
        
        rt.anchoredPosition = new Vector2(xCenter, yCenter);

        if(item.Rotated)
            rt.sizeDelta = new Vector2(rect.ySize * yInterval, rect.xSize * xInterval);
        else
            rt.sizeDelta = new Vector2(rect.xSize * xInterval, rect.ySize * yInterval);

        spriteObjects.Add(item, spriteGO);

    }

    /// <summary>
    /// Updates the visual representation of the object currently in cursor.
    /// </summary>
    private void UpdateCursorItem(Storeable item)
    {
        if (cursorSprite != null)
            Destroy(cursorSprite.gameObject);

        if (item == null)
        {
            cursorItem = null;
            return;
        }

        var go = new GameObject();
        var sprite = go.AddComponent<Image>();
        sprite.sprite = item.Sprite;
        sprite.raycastTarget = false;

        cursorItem = item;
        cursorSprite = go.GetComponent<RectTransform>();


        var rt = cursorSprite.GetComponent<RectTransform>();
        rt.SetParent(transform, false);

        if (item.Rotated)
        {
            rt.sizeDelta = new Vector2(item.y * yInterval, item.x * xInterval);
            rt.Rotate(0, 0, 90);
            cursorRotated = true;
        }
        else
        {
            rt.sizeDelta = new Vector2(item.x * xInterval, item.y * yInterval);
            cursorRotated = false;
        }
    }

    #endregion
}
