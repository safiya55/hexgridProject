using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;

public class HexMapEditor : MonoBehaviour
{

	enum OptionalToggle
	{
		Ignore, Yes, No
	}

	OptionalToggle riverMode, roadMode, walledMode;

	//public Color[] colors;

	int activeTerrainTypeIndex;

	public HexGrid hexGrid;

	//private Color activeColor;

	int activeElevation;

	//keep track of the active water level, and whether it should be applied to cells.
	int activeWaterLevel;

	//keep track of the active urban cells with features, 
	//and whether it should be applied to cells.
	int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;

	bool applyElevation = true;
	bool applyWaterLevel = true;
	bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;

	// bool applyColor;

	int brushSize;

	//Elements to detect Drag to create river
	bool isDrag;
	HexDirection dragDirection;
	HexCell previousCell;

	public Material terrainMaterial;

	public void SetBrushSize(float size)
	{
		brushSize = (int)size;
	}


	void Awake()
	{
		terrainMaterial.DisableKeyword("GRID_ON");
		SetEditMode(false);
	}

	void Update()
	{
		//if the cursor is not on top of a GUI element.
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButton(0))
			{
				HandleInput();
				return;
			}
			//invokes CreateUnit when the U key is pressed.
			if (Input.GetKeyDown(KeyCode.U))
			{
				if (Input.GetKey(KeyCode.LeftShift))
				{
					DestroyUnit();
				}
				else
				{
					CreateUnit();
				}
				return;
			}
		}
		previousCell = null;
	}

	void HandleInput()
	{
		HexCell currentCell = GetCellUnderCursor();
		if (currentCell)
		{
			if (previousCell && previousCell != currentCell)
			{
				ValidateDrag(currentCell);
			}
			else
			{
				isDrag = false;
			}

				EditCells(currentCell);
			

			//old code for path finding
			// {
			// // to check whether the shift key is being held down.
			// else if (Input.GetKey(KeyCode.LeftShift) && searchToCell != currentCell)
			 	{
			// 		if (searchFromCell != currentCell)
			// 		{
			// 			if (searchFromCell)
			// 			{
			// 				searchFromCell.DisableHighlight();
			// 			}
			// 			searchFromCell = currentCell;
			// 			searchFromCell.EnableHighlight(Color.blue);
			// 			if (searchToCell)
			// 			{
			// 				hexGrid.FindPath(searchFromCell, searchToCell, 24);
			// 			}
			// 		}
			// 	}
			// 	////if not in edit mode find distance of cells
			// 	else if (searchFromCell && searchFromCell != currentCell)
			// 	{
			// 		if (searchToCell != currentCell)
			// 		{
			// 			searchToCell = currentCell;
			// 			hexGrid.FindPath(searchFromCell, searchToCell, 24);
			// 		}
			// 	}
			 }

			previousCell = currentCell;

		}
		else
		{
			//set to null when not dragging
			previousCell = null;
		}
	}


	HexCell GetCellUnderCursor()
	{
		return
			hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
	}

	void ValidateDrag(HexCell currentCell)
	{
		//validate a drag by verifying that the current cell 
		//is a neighbor of the previous cell. 
		//by looping through its neighbors
		for (
			dragDirection = HexDirection.NE;
			dragDirection <= HexDirection.NW;
			dragDirection++
		)
		{
			// If we find a match we know the drag direction
			if (previousCell.GetNeighbor(dragDirection) == currentCell)
			{
				isDrag = true;
				return;
			}
		}
		isDrag = false;
	}

	void EditCells(HexCell center)
	{
		int centerX = center.coordinates.X;
		int centerZ = center.coordinates.Z;

		for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
		{
			for (int x = centerX - r; x <= centerX + brushSize; x++)
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}

		for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
		{
			for (int x = centerX - brushSize; x <= centerX + r; x++)
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
	}

	void EditCell(HexCell cell)
	{
		if (cell)
		{
			if (activeTerrainTypeIndex >= 0)
			{
				Debug.Log("set cell value");
				cell.TerrainTypeIndex = activeTerrainTypeIndex;
			}
			if (applyElevation)
			{
				cell.Elevation = activeElevation;
			}
			//for water level
			if (applyWaterLevel)
			{
				cell.WaterLevel = activeWaterLevel;
			}

			if (applySpecialIndex)
			{
				cell.SpecialIndex = activeSpecialIndex;
			}

			if (applyUrbanLevel)
			{
				cell.UrbanLevel = activeUrbanLevel;
			}

			if (applyFarmLevel)
			{
				cell.FarmLevel = activeFarmLevel;
			}

			if (applyPlantLevel)
			{
				cell.PlantLevel = activePlantLevel;
			}

			//code to remove rivers
			if (riverMode == OptionalToggle.No)
			{
				cell.RemoveRiver();
			}

			if (roadMode == OptionalToggle.No)
			{
				cell.RemoveRoads();
			}
			//When the wall toggle is active, just set the the current cell's walled state based on the toggle.
			if (walledMode != OptionalToggle.Ignore)
			{
				cell.Walled = walledMode == OptionalToggle.Yes;
			}

			if (isDrag)
			{
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell)
				{
					if (riverMode == OptionalToggle.Yes)
					{
						otherCell.SetOutgoingRiver(dragDirection);
					}
					if (roadMode == OptionalToggle.Yes)
					{
						otherCell.AddRoad(dragDirection);
					}
				}
			}
		}
	}

	public void SetElevation(float elevation)
	{
		activeElevation = (int)elevation;
	}

	public void SetApplyElevation(bool toggle)
	{
		applyElevation = toggle;
	}

	public void SetRiverMode(int mode)
	{
		riverMode = (OptionalToggle)mode;
	}

	public void SetRoadMode(int mode)
	{
		roadMode = (OptionalToggle)mode;
	}

	// methods to connect these settings with the UI.
	public void SetApplyWaterLevel(bool toggle)
	{
		applyWaterLevel = toggle;
	}

	public void SetWaterLevel(float level)
	{
		activeWaterLevel = (int)level;
	}

	public void SetApplyUrbanLevel(bool toggle)
	{
		applyUrbanLevel = toggle;
	}

	public void SetUrbanLevel(float level)
	{
		activeUrbanLevel = (int)level;
	}

	public void SetApplyFarmLevel(bool toggle)
	{
		applyFarmLevel = toggle;
	}

	public void SetFarmLevel(float level)
	{
		activeFarmLevel = (int)level;
	}

	public void SetApplyPlantLevel(bool toggle)
	{
		applyPlantLevel = toggle;
	}

	public void SetPlantLevel(float level)
	{
		activePlantLevel = (int)level;
	}
	//support for a toggle to adjust walled state of cells
	public void SetWalledMode(int mode)
	{
		walledMode = (OptionalToggle)mode;
	}

	public void SetApplySpecialIndex(bool toggle)
	{
		applySpecialIndex = toggle;
	}

	public void SetSpecialIndex(float index)
	{
		activeSpecialIndex = (int)index;
	}

	public void SetTerrainTypeIndex(int index)
	{
		Debug.Log("here");
		activeTerrainTypeIndex = index;
	}

	public void ShowGrid(bool visible)
	{
		if (visible)
		{
			terrainMaterial.EnableKeyword("GRID_ON");
		}
		else
		{
			terrainMaterial.DisableKeyword("GRID_ON");
		}
	}

	public void SetEditMode(bool toggle)
	{
		enabled = toggle;
	}

	void CreateUnit()
	{
		HexCell cell = GetCellUnderCursor();
		//if there is a cell
		if (cell && !cell.Unit)
		{
			//invoking AddUnit with a newly 
			// //instantiated unit, its location, and a random orientation.
			hexGrid.AddUnit(
				Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f)
			);
		}
	}

	//make it able to destroy units
	void DestroyUnit()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell && cell.Unit)
		{
			//invoke remove unit from hexgrid n also indirectly tells it to die
			hexGrid.RemoveUnit(cell.Unit);
		}
	}
}