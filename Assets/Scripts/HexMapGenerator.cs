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
        for(int guard = 0; guard < 10000; guard++)
        {
            bool sink = Random.value < sinkProbability;
            //Each iteration inside the loop should now either raise 
            // or sink a chunk of land, depending on the sink probability.
            for(int i = 0; i < regions.Count; i++){
                MapRegion region = regions[i];
            
                int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
                if (sink)
                {
                    landBudget = SinkTerrain(chunkSize, landBudget, region);
                }
                else
                {
                    landBudget = RaiseTerrain(chunkSize, landBudget, region);
                    if (landBudget == 0){
                        return;
                    }
                }
            }
        }
        if(landBudget > 0){
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
        }
    }

    struct MapRegion{
        public int xMin, xMax, zMin, zMax;
    }
    List<MapRegion> regions;

    //create the required list or clear it if there already is one
    void CreateRegions(){
        if (regions == null){
            regions = new List<MapRegion>();
        }
        else{
            regions.Clear();
        }
        MapRegion region;
        if(Random.value < 0.5f){
            region.xMin = mapBorderX;
            region.xMax = grid.cellCountX / 2 - regionBorder;
            region.zMin = mapBorderZ;
            region.zMax = grid.cellCountZ - mapBorderZ;
            regions.Add(region);
            region.xMin = grid.cellCountX / 2 + regionBorder;
            region.xMax = grid.cellCountX - mapBorderX;
            regions.Add(region);
        }
        else{
            region.xMin = mapBorderX;
            region.xMax = grid.cellCountX - mapBorderX;
            region.zMin = mapBorderZ;
            region.zMax = grid.cellCountZ / 2 - regionBorder;
            regions.Add(region);
            region.xMin = grid.cellCountX / 2 + regionBorder;
            region.xMax = grid.cellCountX - mapBorderX;
            regions.Add(region);
        }
    }
}