using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;  // Add this for UI components
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour
{
    //public Color defaultColor = Color.white;
    //public Color touchedColor = Color.green;
    public int cellCountX = 20, cellCountZ = 15;
    int chunkCountX, chunkCountZ;
    public HexCell cellPrefab;       // HexCell prefab
    public Text cellLabelPrefab;     // Text prefab for cell labels

    public HexGridChunk chunkPrefab;

    HexGridChunk[] chunks;

    public Texture2D noiseSource;

    HexCell[] cells;

    //seed for features
    public int seed;

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        CreateMap(cellCountX, cellCountZ);
    }

    public bool CreateMap(int x, int z)
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

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

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

        // Set the color to the default color
        //cell.color = defaultColor;

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

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }
    }

    ////iterate through cells to load info
    public void Load(BinaryReader reader, int header)
    {
        int x = 20, z = 15;
        if (header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        //Because loading overwrites all the data of the existing cells, 
        //we actually don't have to create a new map if we end up loading one 
        //with the same size. So it's possible to skip this step.
        if (x != cellCountX || z != cellCountZ)
        {
            //abort map loading, when the map creation failed.
            if (!CreateMap(x, z))
            {
                return;
            }
        }


        CreateMap(x, z);

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }
        //refresh all chunks
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }
    }

    public void FindPath(HexCell fromCell, HexCell toCell)
    {
        //ensure that only a single search is active at any time.
        StopAllCoroutines();
        //starting a new search
        StartCoroutine(Search(fromCell, toCell));
    }

    //uses priority queue
    IEnumerator Search(HexCell fromCell, HexCell toCell)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Distance = int.MaxValue;
            //rid of all previous highlights. 
            cells[i].DisableHighlight();
        }

        fromCell.EnableHighlight(Color.blue);
		toCell.EnableHighlight(Color.red);

        //update frequency of 60 iterations per second is 
        // slow enough that we can see what's happening
        WaitForSeconds delay = new WaitForSeconds(1 / 60f);
        List<HexCell> frontier = new List<HexCell>();
        fromCell.Distance = 0;
        frontier.Add(fromCell);

        // algorithm loops as long as there is something in the queue. 
        // //Each iteration, the front-most cell is taken out of the queue.
        while (frontier.Count > 0)
        {
            yield return delay;
            HexCell current = frontier[0];
            frontier.RemoveAt(0);
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);

                //skip if cell that dont exist and those we have already given distance to
                if (neighbor == null)
                {
                    continue;
                }

                //skip cells that are underwater
                if (neighbor.IsUnderwater)
                {
                    continue;
                }
                //cells skip cliffs
                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff)
                {
                    continue;
                }

                //make it easy and fast to travel by road
                //leave road at 1 ancrease cost of other edges to 10
                int distance = current.Distance;
                if (current.HasRoadThroughEdge(d))
                {
                    distance += 1;
                }
                else if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    distance += edgeType == HexEdgeType.Flat ? 5 : 10;

                    //cost for terrain features
                    distance += neighbor.UrbanLevel + neighbor.FarmLevel +
                        neighbor.PlantLevel;
                }

                if (neighbor.Distance == int.MaxValue)
                {
                    neighbor.Distance = distance;
                    frontier.Add(neighbor);
                }

                else if (distance < neighbor.Distance)
                {
                    neighbor.Distance = distance;
                }

                //sort the cells by their distance. To do so, we have to invoke the 
                // //list's sort method with a reference to a method that performs 
                // this comparison.
                frontier.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            }
        }
    }
}
