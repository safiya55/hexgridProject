using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;  // Add this for UI components
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class HexGrid : MonoBehaviour
{
    //public Color defaultColor = Color.white;
    //public Color touchedColor = Color.green;
    public int cellCountX = 20, cellCountZ = 15;
    public bool wrapping;
    int chunkCountX, chunkCountZ;
    public HexCell cellPrefab;       // HexCell prefab
    public Text cellLabelPrefab;     // Text prefab for cell labels

    public HexGridChunk chunkPrefab;

    HexGridChunk[] chunks;

    public Texture2D noiseSource;

    HexCell[] cells;

    //seed for features
    public int seed;

    int searchFrontierPhase;

    HexCellPriorityQueue searchFrontier;

    HexCell currentPathFrom, currentPathTo;

    bool currentPathExists;

    List<HexUnit> units = new List<HexUnit>();

    public HexUnit unitPrefab;

    HexCellShaderData cellShaderData;

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexUnit.unitPrefab = unitPrefab;

        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        // setup the grid reference after creating shader data
        cellShaderData.Grid = this;
        CreateMap(cellCountX, cellCountZ, wrapping);
    }

    public bool CreateMap(int x, int z, bool wrapping)
    {

        if (
            x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.chunkSizeZ != 0
        )
        {
            Debug.LogError("Unsupported map size.");
            return false;
        }

        //destroying all the current chunks at the start
        //remove old map
        ClearPath();
        ClearUnits();
        if (chunks != null)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                Destroy(chunks[i].gameObject);
            }
        }

        //support for any size map
        cellCountX = x;
        cellCountZ = z;
        this.wrapping = wrapping;
        HexMetrics.wrapSize = wrapping ? cellCountX : 0;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        cellShaderData.Initialize(cellCountX, cellCountZ);

        CreateChunks();
        CreateCells();

        return true;
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    void CreateCells()
    {

        cells = new HexCell[cellCountZ * cellCountX];
        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    void OnEnable()
    {
        //Make sure that we're not generating it more often than necessary.
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;

            HexMetrics.wrapSize = wrapping ? cellCountX : 0;

            //To ensure that the vision adjusts itself automatically
            ResetVisibility();
        }
    }


    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }


    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);



        // Instantiate the HexCell
        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;

        // Assign the HexCoordinates to the cell
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Index = i;

        //assign its shader data component to this property.
        cell.ShaderData = cellShaderData;

        //Cells that's aren't at the edge are explorable, while all others are inexplorable.
        cell.Explorable =
            x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;

        //connect cells from east to west direction
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            //connecting to SE tile
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);

                //connect to SW tile
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            //doing same logic for odd rows
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        // Instantiate and position the label
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition =
            new Vector2(position.x, position.z);

        cell.uiRect = label.rectTransform;
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;

        if (z < 0 || z >= cellCountZ)
        {
            return null;
        }
        int x = coordinates.X + z / 2;

        if (x < 0 || x >= cellCountX)
        {
            return null;
        }

        return cells[x + z * cellCountX];
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    //iterate through cells to save info
    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        writer.Write(wrapping);

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }

        //Save method will take care of writing the unit data. 
        // //First write how many units there are, then loop through the units.
        writer.Write(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
        }
    }

    ////iterate through cells to load info
    public void Load(BinaryReader reader, int header)
    {
        ClearPath();
        ClearUnits();
        int x = 20, z = 15;
        if (header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        bool wrapping = header >= 5 ? reader.ReadBoolean() : false;

        //Because loading overwrites all the data of the existing cells, 
        //we actually don't have to create a new map if we end up loading one 
        //with the same size. So it's possible to skip this step.
        if (x != cellCountX || z != cellCountZ || this.wrapping != wrapping)
        {
            //abort map loading, when the map creation failed.
            if (!CreateMap(x, z, wrapping))
            {
                return;
            }
        }

        //remember the original mode 
        bool originalImmediateMode = cellShaderData.ImmediateMode;

        //switch to immediate mode in HexGrid.Load before the cells and units are loaded.
        //prevent slow downs on larger maps
        cellShaderData.ImmediateMode = true;

        //HexGrid.Load has to pass the header data on to HexCell.Load.
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader, header);
        }
        //refresh all chunks
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }


        if (header >= 2) // only works for save files that 
        // are at least version 2, otherwise there are no units to load.
        {
            //read the unit count and use it to load all 
            // the stored units, passing itself as an additional argument.
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }

        //switch back to original mode after the work is done.
        cellShaderData.ImmediateMode = originalImmediateMode;
    }

    public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, unit);
        ShowPath(unit.Speed);
    }

    //uses priority queue
    bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        int speed = unit.Speed;
        searchFrontierPhase += 2;
        //use preiority queue
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        //update frequency of 60 iterations per second is 
        // slow enough that we can see what's happening
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        HexCoordinates fromCoordinates = fromCell.coordinates;
        // algorithm loops as long as there is something in the queue. 
        // //Each iteration, the front-most cell is taken out of the queue.
        while (searchFrontier.Count > 0)
        {
            //yield return delay;
            HexCell current = searchFrontier.Dequeue();

            //fixes the damned road bug
            if (current == null)
            {
                break;
            }

            current.SearchPhase += 1;


            if (current == toCell)
            {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);

                //skip if cell that dont exist and those we have already given distance to
                if (
                    neighbor == null ||
                    neighbor.SearchPhase > searchFrontierPhase
                )
                {
                    continue;
                }

                if (!unit.IsValidDestination(neighbor))
                {
                    continue;
                }
                int moveCost = unit.GetMoveCost(current, neighbor, d);
                if (moveCost < 0)
                {
                    continue;
                }

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;

                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic =
                        neighbor.coordinates.DistanceTo(toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }

                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return false;
    }

    List<HexCell> GetVisibleCells(HexCell fromCell, int range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

        searchFrontierPhase += 2;
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        //The unit's inherent range represents its own height, 
        // flight altitude, or scouting potential.
        range += fromCell.ViewElevation;
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        HexCoordinates fromCoordinates = fromCell.coordinates;

        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            visibleCells.Add(current);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (
                    neighbor == null ||
                    neighbor.SearchPhase > searchFrontierPhase ||
                    !neighbor.Explorable
                )
                {
                    continue;
                }

                int distance = current.Distance + 1;
                // to add the neighbor's view elevation to the covered distance 
                // when determining when we can see a cell.
                if (distance + neighbor.ViewElevation > range || //ensure that only the shortest paths are considered when determining a cell's visibility. This can be done by skipping paths that would become longer than that.
                    distance > fromCoordinates.DistanceTo(neighbor.coordinates)
                    )
                {
                    continue;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return visibleCells;
    }

    void ShowPath(int speed)
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
        }
        currentPathFrom.EnableHighlight(Color.blue);
        currentPathTo.EnableHighlight(Color.red);
    }

    public void ClearPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            current.DisableHighlight();
            currentPathExists = false;
        }
        else if (currentPathFrom)
        {
            currentPathFrom.DisableHighlight();
            currentPathTo.DisableHighlight();
        }
        currentPathFrom = currentPathTo = null;
    }

    //when new map is created or loaded
    void ClearUnits()
    {
        //get rid of all units currently on the map
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }

    // add new units to the list when we create them.
    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        //takes care of positioning the unit and settings its parent.
        units.Add(unit);
        unit.Grid = this;
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    //for removing a unit to HexGrid as well. 
    public void RemoveUnit(HexUnit unit)
    {
        //remove the unit from the list and tell it to die.
        units.Remove(unit);
        unit.Die();
    }

    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }
        return null;
    }

    //If we have a valid path, 
    // then it should be possible to move the unit to the destination. 
    public bool HasPath
    {
        get
        {
            return currentPathExists;
        }
    }

    //method to retrieve the current path in the form of a list of cells. 
    // It can grab one from the list pool and return it, if there actually is a path.
    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
        {
            return null;
        }
        List<HexCell> path = ListPool<HexCell>.Get();
        // list is filled by following the path reference from the destination back to the start,
        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }
        //want the entire path, which also includes the starting cell.
        path.Add(currentPathFrom);
        //reverse the order since initially order was in reverse
        path.Reverse();
        return path;
    }

    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].DecreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    //to reset all the cells
    public void ResetVisibility()
    {
        //loop through the cells
        for (int i = 0; i < cells.Length; i++)
        {
            //delegate the resetting to them.
            cells[i].ResetVisibility();
        }

        //has to apply the vision of all units again
        for (int i = 0; i < units.Count; i++)
        {
            //needs to know each unit's vision range.
            HexUnit unit = units[i];
            //available via a VisionRange property.
            IncreaseVisibility(unit.Location, unit.VisionRange);
        }
    }

    public HexCell GetCell(int xOffset, int zOffset)
    {
        return cells[xOffset + zOffset * cellCountX];
    }

    public HexCell GetCell(int cellIndex)
    {
        return cells[cellIndex];
    }
}
