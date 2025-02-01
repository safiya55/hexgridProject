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

	public void SetBrushSize(float size)
	{
		brushSize = (int)size;
	}


	void Awake()
	{
		SetTerrainTypeIndex(0);
	}

	void Update()
	{
		if (Input.GetMouseButton(0) &&
			!EventSystem.current.IsPointerOverGameObject())
		{
			HandleInput();
		}
		else
		{
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
			if (previousCell && previousCell != currentCell)
			{
				ValidateDrag(currentCell);
			}
			else
			{
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

	public void ShowUI(bool visible)
	{
		hexGrid.ShowUI(visible);
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

	public void Save()
	{
		//Debug.Log(Application.persistentDataPath);
		//create save file path
		string path = Path.Combine(Application.persistentDataPath, "test.map"); //this is supposed to be deleted but it doesn't work w/o it
		//write to file
		using (BinaryWriter saveWriter = new BinaryWriter(File.Open(path, FileMode.Create))
		)
		{
			saveWriter.Write(1);
			hexGrid.Save(saveWriter);
		}
	}

	public void Load()
	{
		string path = Path.Combine(Application.persistentDataPath, "test.map"); //this is supposed to be deleted but it doesn't work w/o it
		using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
		{
			int header = reader.ReadInt32();
			if (header <= 1) {
				hexGrid.Load(reader, header);
				HexMapCamera.ValidatePosition();
			}
			else {
				Debug.LogWarning("Unknown map format " + header);
			}
		}
	}
}