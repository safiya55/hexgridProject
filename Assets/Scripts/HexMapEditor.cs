using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{

    enum OptionalToggle {
		Ignore, Yes, No
	}
	
	OptionalToggle riverMode, roadMode;

    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;

    int activeElevation;

	//keep track of the active water level, and whether it should be applied to cells.
	int activeWaterLevel;

	int activeUrbanLevel;

    bool applyElevation = true;
	bool applyWaterLevel = true;
	bool applyUrbanLevel;

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
			//for water level
			if (applyWaterLevel) {
				cell.WaterLevel = activeWaterLevel;
			}

			if (applyUrbanLevel) {
				cell.UrbanLevel = activeUrbanLevel;
			}

            //code to remove rivers
            if (riverMode == OptionalToggle.No) {
				cell.RemoveRiver();
			}
            if (roadMode == OptionalToggle.No) {
				cell.RemoveRoads();
			}
			if (isDrag) {
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell) {
					if (riverMode == OptionalToggle.Yes) {
						otherCell.SetOutgoingRiver(dragDirection);
					}
					if (roadMode == OptionalToggle.Yes) {
						otherCell.AddRoad(dragDirection);
					}
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

    public void SetRoadMode (int mode) {
		roadMode = (OptionalToggle)mode;
	}

	// methods to connect these settings with the UI.
	public void SetApplyWaterLevel (bool toggle) {
		applyWaterLevel = toggle;
	}

	public void SetWaterLevel (float level) {
		activeWaterLevel = (int)level;
	}

	public void SetApplyUrbanLevel (bool toggle) {
		applyUrbanLevel = toggle;
	}
	
	public void SetUrbanLevel (float level) {
		activeUrbanLevel = (int)level;
	}
}