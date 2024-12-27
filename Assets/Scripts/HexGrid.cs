using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;  // Add this for UI components

public class HexGrid : MonoBehaviour
{
    public int chunkCountX = 4, chunkCountZ = 3;
    public Color defaultColor = Color.white;
	public Color touchedColor = Color.green;
    int cellCountX, cellCountZ;
    public HexCell cellPrefab;       // HexCell prefab
    public Text cellLabelPrefab;     // Text prefab for cell labels

    public HexGridChunk chunkPrefab;

    public Texture2D noiseSource;

    HexCell[] cells;

    HexGridChunk[] chunks;

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
		cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

		CreateChunks();
        CreateCells();
    }

    void CreateChunks () {
		chunks = new HexGridChunk[chunkCountX * chunkCountZ];

		for (int z = 0, i = 0; z < chunkCountZ; z++) {
			for (int x = 0; x < chunkCountX; x++) {
				HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
				chunk.transform.SetParent(transform);
			}
		}
	}

	void CreateCells () {
		cells = new HexCell[cellCountZ * cellCountX];

		for (int z = 0, i = 0; z < cellCountZ; z++) {
			for (int x = 0; x < cellCountX; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

    public HexCell GetCell (HexCoordinates coordinates) {
		int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ) {
			return null;
		}
		int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX) {
			return null;
		}
		return cells[x + z * cellCountX];
	}


    void OnEnable () {
		HexMetrics.noiseSource = noiseSource;
	}


    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX  + coordinates.Z / 2;
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
        cell.color = defaultColor;

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
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX ]);

                //connect to SW tile
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX  - 1]);
                }
            }
            //doing same logic for odd rows
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX ]);
                if (x < cellCountX  - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX  + 1]);
                }
            }
        }

        // Instantiate and position the label
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);

        // Set the label text to show the coordinates of the cell
        label.text = cell.coordinates.ToStringOnSeparateLines();  // Displaying coordinates using HexCoordinates

        cell.uiRect = label.rectTransform;

        //make sure that the perturbation is applied immediately
        //Otherwise the grid would start out flat
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    void AddCellToChunk (int x, int z, HexCell cell) 
    {
        // find the correct chunk via integer divisions of x and z by the chunk sizes.
        int chunkX = x / HexMetrics.chunkSizeX;
		int chunkZ = z / HexMetrics.chunkSizeZ;
		HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        //determine the cell's index local to its chunk
        int localX = x - chunkX * HexMetrics.chunkSizeX;
		int localZ = z - chunkZ * HexMetrics.chunkSizeZ;

        //add the cell to the chunk
		chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
	}

    public void ShowUI (bool visible) {
		for (int i = 0; i < chunks.Length; i++) {
			chunks[i].ShowUI(visible);
		}
	}
}
