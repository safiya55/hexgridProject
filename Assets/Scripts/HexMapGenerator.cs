using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{

    public HexGrid grid;

    public bool useFixedSeed;

    int cellCount;

    HexCellPriorityQueue searchFrontier;

    int searchFrontierPhase;

    public int seed;

    [Range(0f, 0.5f)]
    public float jitterProbability = 0.25f;

    [Range(20, 200)]
    public int chunkSizeMin = 30;

    [Range(20, 200)]
    public int chunkSizeMax = 100;

    [Range(5, 95)]
    public int landPercentage = 40;

    [Range(1, 5)]
    public int waterLevel = 3;

    [Range(0f, 1f)]
    public float highRiseProbability = 0.25f;

    //probability land sinks under
    //should always make it less likely to sink than to raise. 
    [Range(0f, 0.4f)]
    public float sinkProbability = 0.2f;

    [Range(-4, 0)]
    public int elevationMinimum = -2;

    [Range(6, 10)]
    public int elevationMaximum = 8;

    [Range(0, 10)]
    public int mapBorderX = 5;

    [Range(0, 10)]
    public int mapBorderZ = 5;

    [Range(0, 10)]
    public int regionBorder = 5;

    [Range(1, 4)]
    public int regionCount = 1;

    [Range(0, 100)]
    public int erosionPercentage = 50;

    [Range(0f, 1f)]
	public float evaporationFactor = 0.5f;

    [Range(0f, 1f)]
	public float precipitationFactor = 0.25f;

    struct ClimateData {
		public float clouds, moisture;
	}

    [Range(0f, 1f)]
	public float runoffFactor = 0.25f;

    List<ClimateData> climate = new List<ClimateData>();

    void CreateClimate () {
		climate.Clear();
		ClimateData initialData = new ClimateData();
		for (int cycle = 0; cycle < 40; cycle++) {
			for (int i = 0; i < cellCount; i++) {
				EvolveClimate(i);
			}
		}
	}

    void EvolveClimate (int cellIndex) {
		HexCell cell = grid.GetCell(cellIndex);
		ClimateData cellClimate = climate[cellIndex];
		
		if (cell.IsUnderwater) {
            cellClimate.moisture = 1f;
			cellClimate.clouds += evaporationFactor;
		}
        else {
			float evaporation = cellClimate.moisture * evaporationFactor;
			cellClimate.moisture -= evaporation;
			cellClimate.clouds += evaporation;
		}

        float precipitation = cellClimate.clouds * precipitationFactor;
		cellClimate.clouds -= precipitation;
        cellClimate.moisture += precipitation;


        float cloudDispersal = cellClimate.clouds * (1f / 6f);
        float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if (!neighbor) {
				continue;
			}
			ClimateData neighborClimate = climate[neighbor.Index];
			neighborClimate.clouds += cloudDispersal;
            int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
			if (elevationDelta < 0) {
				cellClimate.moisture -= runoff;
				neighborClimate.moisture += runoff;
			}
			climate[neighbor.Index] = neighborClimate;
		}
		cellClimate.clouds = 0f;

		climate[cellIndex] = cellClimate;
	}


    public void GenerateMap(int x, int z)
    {
        //It first stores the current state of the number generator, 
        // initialized it with a specific seed,
        Random.State originalRandomState = Random.state;

        if (!useFixedSeed)
        {
            //Seed initialize random and generate random maps
            seed = Random.Range(0, int.MaxValue);
            seed ^= (int)System.DateTime.Now.Ticks;
            seed ^= (int)Time.unscaledTime;
            seed &= int.MaxValue;
            Random.InitState(seed);
        }

        //keep track of the amount of cells in HexMapGenerator 
        cellCount = x * z;

        //new map new map created
        grid.CreateMap(x, z);

        //Make sure that the priority queue exists before we will need it.
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }

        //make it explicit which cells are land or water
        for (int i = 0; i < cellCount; i++)
        {
            // setting the water level of all cells to 1.
            grid.GetCell(i).WaterLevel = waterLevel;

            CreateRegions();
        }

        //calculate how many cells have to become land. That amount is our land budget.
        CreateLand();

        //make the terrain look more rough and jagged
        ErodeLand();

        // creating a climate after the land has been eroded and before the terrain types are set
        CreateClimate();

        //set all terrain types once.
        SetTerrainType();

        //After a new map has been created

        for (int i = 0; i < cellCount; i++)
        {
            //search frontier of all cells is zero.
            grid.GetCell(i).SearchPhase = 0;
        }

        // later restores it back to its old state.
        Random.state = originalRandomState;
    }

    //invoke RaiseTerrain as long as there's still land budget to be spent.     
    void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);

        //keep raising land as long as it has budget.
        for (int guard = 0; guard < 10000; guard++)
        {
            bool sink = Random.value < sinkProbability;
            //Each iteration inside the loop should now either raise 
            // or sink a chunk of land, depending on the sink probability.
            for (int i = 0; i < regions.Count; i++)
            {
                MapRegion region = regions[i];

                int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
                if (sink)
                {
                    landBudget = SinkTerrain(chunkSize, landBudget, region);
                }
                else
                {
                    landBudget = RaiseTerrain(chunkSize, landBudget, region);
                    if (landBudget == 0)
                    {
                        return;
                    }
                }
            }
        }
        if (landBudget > 0)
        {
            Debug.LogWarning("Failed to use up " + landBudget + "land budget");
        }
    }

    int RaiseTerrain(int chunkSize, int budget, MapRegion region)
    {
        // search for appropriate cells 
        //increasing the search frontier phase by 1
        searchFrontierPhase += 1;
        //Then initialize the frontier with the first cell by random
        HexCell firstCell = GetRandomCell(region);
        firstCell.SearchPhase = searchFrontierPhase;
        //set its distance and heuristic to zero besides setting its search phase
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        //use the first random cell as the center of the chunk. 
        HexCoordinates center = firstCell.coordinates;

        int rise = Random.value < highRiseProbability ? 2 : 1;
        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0)
        { // Each iteration, dequeue the next cell, set its terrain type, increase the size, 
          // then go through that cell's neighbors

            //All neighbors are simply added to the frontier
            HexCell current = searchFrontier.Dequeue();

            int originalElevation = current.Elevation;
            //ensure that we do not go above the maximum allowed elevation.
            int newElevation = originalElevation + rise;
            if (newElevation > elevationMaximum)
            {
                continue;
            }

            current.Elevation = newElevation;

            //When the current cell's new elevation is by water level, it has 
            // just become land, so the budget decrements,
            //  which could end the chunk's growth.
            if (
                originalElevation < waterLevel &&
                newElevation >= waterLevel && --budget == 0
            )
            {
                break;
            }

            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    //distance of all other cells is relative to first random cell 
                    // as the center of the chunk.
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    //if the next Random.value number is less than some threshold, set that cell's heuristic to 1 instead of 0. Let's
                    // use jitterProbability as the threshold, which means most likely a certain percentage of the cells will be affected.
                    //mess the chunk up to make it random
                    neighbor.SearchHeuristic =
                        Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        //Once done, clear the frontier.
        searchFrontier.Clear();
        return budget;
    }

    int SinkTerrain(int chunkSize, int budget, MapRegion region)
    {
        // search for appropriate cells 
        //increasing the search frontier phase by 1
        searchFrontierPhase += 1;
        //Then initialize the frontier with the first cell by random
        HexCell firstCell = GetRandomCell(region);
        firstCell.SearchPhase = searchFrontierPhase;
        //set its distance and heuristic to zero besides setting its search phase
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        //use the first random cell as the center of the chunk. 
        HexCoordinates center = firstCell.coordinates;

        int sink = Random.value < highRiseProbability ? 2 : 1;
        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0)
        { // Each iteration, dequeue the next cell, set its terrain type, increase the size, 
          // then go through that cell's neighbors

            //All neighbors are simply added to the frontier
            HexCell current = searchFrontier.Dequeue();

            int originalElevation = current.Elevation;

            int newElevation = current.Elevation - sink;
            if (newElevation < elevationMinimum)
            {
                continue;
            }
            current.Elevation = newElevation;

            //When the current cell's new elevation is by water level, it has 
            // just become land, so the budget decrements,
            //  which could end the chunk's growth.
            if (
                originalElevation >= waterLevel &&
                newElevation < waterLevel
            )
            {
                budget += 1;
            }

            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    //distance of all other cells is relative to first random cell 
                    // as the center of the chunk.
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    //if the next Random.value number is less than some threshold, set that cell's heuristic to 1 instead of 0. Let's
                    // use jitterProbability as the threshold, which means most likely a certain percentage of the cells will be affected.
                    //mess the chunk up to make it random
                    neighbor.SearchHeuristic =
                        Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        //Once done, clear the frontier.
        searchFrontier.Clear();
        return budget;
    }

    //determines a random cell index and retrieves the 
    // corresponding cell from the grid.
    HexCell GetRandomCell(MapRegion region)
    {
        return grid.GetCell(Random.Range(region.xMin, region.xMax), Random.Range(region.zMax, region.zMin));
    }

    //to set all terrain types once.
    void SetTerrainType()
    {
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            if (!cell.IsUnderwater)
            {
                //All underwater cells remain sand, as well as the lowest land cells.
                cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
            }

            cell.SetMapData(
                climate[i].moisture
                //(cell.Elevation - elevationMinimum) /
                //(float)(elevationMaximum - elevationMinimum)
            );
        }
    }

    struct MapRegion
    {
        public int xMin, xMax, zMin, zMax;
    }
    List<MapRegion> regions;

    //create the required list or clear it if there already is one
    void CreateRegions()
    {
        if (regions == null)
        {
            regions = new List<MapRegion>();
        }
        else
        {
            regions.Clear();
        }
        MapRegion region;
        switch (regionCount)
        {
            default:
                region.xMin = mapBorderX;
                region.xMax = grid.cellCountX - mapBorderX;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                break;
            case 2:
                if (Random.value < 0.5f)
                {
                    region.xMin = mapBorderX;
                    region.xMax = grid.cellCountX / 2 - regionBorder;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(region);
                    region.xMin = grid.cellCountX / 2 + regionBorder;
                    region.xMax = grid.cellCountX - mapBorderX;
                    regions.Add(region);
                }
                else
                {
                    region.xMin = mapBorderX;
                    region.xMax = grid.cellCountX - mapBorderX;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ / 2 - regionBorder;
                    regions.Add(region);
                    region.xMin = grid.cellCountX / 2 + regionBorder;
                    region.xMax = grid.cellCountX - mapBorderX;
                    regions.Add(region);
                }
                break;
            case 3:
                region.xMin = mapBorderX;
                region.xMax = grid.cellCountX / 3 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                region.xMin = grid.cellCountX / 3 - regionBorder;
                region.xMax = grid.cellCountX * 2 / 3 + regionBorder;
                regions.Add(region);
                region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
                region.xMax = grid.cellCountX - mapBorderX;
                regions.Add(region);
                break;
            case 4:
                region.xMin = mapBorderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ / 2 - regionBorder;
                regions.Add(region);
                region.xMin = grid.cellCountX / 2 + regionBorder;
                region.xMax = grid.cellCountX - mapBorderX;
                regions.Add(region);
                region.zMin = grid.cellCountZ / 2 + regionBorder;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                region.xMin = mapBorderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                regions.Add(region);
                break;
        }
    }

    void ErodeLand()
    {
        List<HexCell> erodibleCells = ListPool<HexCell>.Get();
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            if (IsErodible(cell))
            {
                erodibleCells.Add(cell);
            }
        }
        //making cells no longer erodible
        int targetErodibleCount = (int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);
        while (erodibleCells.Count > targetErodibleCount)
        {
            int index = Random.Range(0, erodibleCells.Count);
            HexCell cell = erodibleCells[index];
            HexCell targetCell = GetErosionTarget(cell);

            cell.Elevation -= 1;
            targetCell.Elevation += 1;

            if (!IsErodible(cell))
            {
                erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
                erodibleCells.RemoveAt(erodibleCells.Count - 1);
            }
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                if (
                    neighbor &&
                    !erodibleCells.Contains(neighbor))
                {
                    erodibleCells.Add(neighbor);
                }
            }

            if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
            {
                erodibleCells.Add(targetCell);
            }

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = targetCell.GetNeighbor(d);
                if (neighbor && neighbor != cell &&
                neighbor.Elevation == targetCell.Elevation + 1 &&
                !IsErodible(neighbor))
                {
                    erodibleCells.Remove(neighbor);
                }
            }

        }
        ListPool<HexCell>.Add(erodibleCells);
    }
    bool IsErodible(HexCell cell)
    {
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                return true;
            }
        }
        return false;
    }
    //erosion lowers one cell and raise its neighbors
    HexCell GetErosionTarget(HexCell cell)
    {
        List<HexCell> candiddates = ListPool<HexCell>.Get();
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                candiddates.Add(neighbor);
            }
        }
        HexCell target = candiddates[Random.Range(0, candiddates.Count)];
        ListPool<HexCell>.Add(candiddates);
        return target;
    }
}