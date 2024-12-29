using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{

    enum OptionalToggle {
		Ignore, Yes, No
	}
	
	OptionalToggle riverMode;

    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;

    int activeElevation;

    bool applyElevation = true;

    bool applyColor;

    int brushSize;

    //Elements to detect Drag to create river
    bool isDrag;
	HexDirection dragDirection;
	HexCell previousCell;

	public void SetBrushSize (float size) 
    {
		brushSize = (int)size;
	}


    void Awake()
    {
        SelectColor(0);
    }

    void Update()
    {
        if (Input.GetMouseButton(0) &&
            !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
        else {
			previousCell = null;
		}
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            // current cell is the one that we find based on the hit point.
            //After we're done editing cells this update, 
            //that cell becomes the previous cell for the next update.
            HexCell currentCell = hexGrid.GetCell(hit.point);
            if (previousCell && previousCell != currentCell) {
				ValidateDrag(currentCell);
			}
			else {
				isDrag = false;
			}
			EditCells(currentCell);
			previousCell = currentCell;

        }
        else 
        {
            //set to null when not dragging
			previousCell = null;
		}
    }

    void ValidateDrag (HexCell currentCell) {
        //validate a drag by verifying that the current cell 
        //is a neighbor of the previous cell. 
        //by looping through its neighbors
		for (
			dragDirection = HexDirection.NE;
			dragDirection <= HexDirection.NW;
			dragDirection++
		) {
            // If we find a match we know the drag direction
			if (previousCell.GetNeighbor(dragDirection) == currentCell) {
				isDrag = true;
				return;
			}
		}
		isDrag = false;
	}

    void EditCells (HexCell center) 
    {
        int centerX = center.coordinates.X;
		int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) 
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++) {
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
			for (int x = centerX - brushSize; x <= centerX + r; x++) {
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
	}

    void EditCell(HexCell cell)
    {
        if (cell) {
			if (applyColor) {
				cell.Color = activeColor;
			}
			if (applyElevation) {
				cell.Elevation = activeElevation;
			}

            //code to remove rivers
            if (riverMode == OptionalToggle.No) {
				cell.RemoveRiver();
			}
			else if (isDrag && riverMode == OptionalToggle.Yes) {
                //This will draw a river from the previous cell to the current cell. 
                //But it ignores the brush size.
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell) {
					otherCell.SetOutgoingRiver(dragDirection);
				}
			}
		}
    }

    public void SelectColor(int index)
    {
        applyColor = index >= 0;
		if (applyColor) {
			activeColor = colors[index];
		}
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SetApplyElevation (bool toggle) {
		applyElevation = toggle;
	}

    public void ShowUI (bool visible) {
		hexGrid.ShowUI(visible);
	}

    public void SetRiverMode (int mode) {
		riverMode = (OptionalToggle)mode;
	}
}