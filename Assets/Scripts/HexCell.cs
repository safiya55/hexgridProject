using UnityEngine;
using System.IO;

public class HexCell : MonoBehaviour
{
	[SerializeField]
	bool[] roads;

	public HexCoordinates coordinates;
	private int elevation = int.MinValue;
	public RectTransform uiRect;

	public HexGridChunk chunk;
	//public Color color;

	//river info
	bool hasIncomingRiver;
	bool hasOutgoingRiver;
	HexDirection incomingRiver;
	HexDirection outgoingRiver;

	[SerializeField]
	HexCell[] neighbors;

	int waterLevel;

	int urbanLevel, farmLevel, plantLevel;

	public bool walled = true;

	int specialIndex;

	public int terrainTypeIndex;


	void Refresh()
	{
		if (chunk)
		{
			chunk.Refresh();
			for (int i = 0; i < neighbors.Length; i++)
			{
				HexCell neighbor = neighbors[i];
				if (neighbor != null && neighbor.chunk != chunk)
				{
					neighbor.chunk.Refresh();
				}
			}
		}
	}

	public HexCell GetNeighbor(HexDirection direction)
	{
		return neighbors[(int)direction];
	}

	public void SetNeighbor(HexDirection direction, HexCell cell)
	{
		neighbors[(int)direction] = cell;
		//set the neighbor in the opposite direction as well
		cell.neighbors[(int)direction.Opposite()] = this;
	}

	public int Elevation
	{
		get
		{
			return elevation;
		}
		set
		{
			if (elevation == value)
			{
				return;
			}
			elevation = value;
			RefreshPosition();

			//Preventing Uphill Rivers and remove them
			ValidateRivers();

			// check for roads in all directions. 
			//If an elevation difference has become too great, an existing road has to be removed.
			for (int i = 0; i < roads.Length; i++)
			{
				if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
				{
					SetRoad(i, false);
				}
			}

			Refresh();
		}
	}

	public Color Color
	{
		get
		{
			return HexMetrics.colors[terrainTypeIndex];
		}
		// set
		// {
		// 	if (color == value)
		// 	{
		// 		return;
		// 	}
		// 	color = value;
		// 	Refresh();
		// }
	}

	//to get a cell's edge type in a certain direction.
	public HexEdgeType GetEdgeType(HexDirection direction)
	{
		return HexMetrics.GetEdgeType(
			elevation, neighbors[(int)direction].elevation
		);
	}

	public HexEdgeType GetEdgeType(HexCell otherCell)
	{
		return HexMetrics.GetEdgeType(
			elevation, otherCell.elevation
		);
	}

	public Vector3 Position
	{
		get
		{
			return transform.localPosition;
		}
	}

	public bool HasIncomingRiver
	{
		get
		{
			return hasIncomingRiver;
		}
	}

	public bool HasOutgoingRiver
	{
		get
		{
			return hasOutgoingRiver;
		}
	}

	public HexDirection IncomingRiver
	{
		get
		{
			return incomingRiver;
		}
	}

	public HexDirection OutgoingRiver
	{
		get
		{
			return outgoingRiver;
		}
	}

	public bool HasRiver
	{
		get
		{
			return hasIncomingRiver || hasOutgoingRiver;
		}
	}

	public bool HasRiverBeginOrEnd
	{
		get
		{
			return hasIncomingRiver != hasOutgoingRiver;
		}
	}

	public bool HasRiverThroughEdge(HexDirection direction)
	{
		return
			hasIncomingRiver && incomingRiver == direction ||
			hasOutgoingRiver && outgoingRiver == direction;
	}

	public void RemoveOutgoingRiver()
	{
		if (!hasOutgoingRiver)
		{
			return;
		}
		hasOutgoingRiver = false;
		RefreshSelfOnly();

		//removes neighboring rivers
		HexCell neighbor = GetNeighbor(outgoingRiver);
		neighbor.hasIncomingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	void RefreshSelfOnly()
	{
		chunk.Refresh();
	}

	//Removing the incoming river works the same way.
	public void RemoveIncomingRiver()
	{
		if (!hasIncomingRiver)
		{
			return;
		}
		hasIncomingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(incomingRiver);
		neighbor.hasOutgoingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	//removing the entire river just means removing both 
	//the outgoing and incoming river parts.
	public void RemoveRiver()
	{
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

	public void SetOutgoingRiver(HexDirection direction)
	{
		//dont do anything if river already exist
		if (hasOutgoingRiver && outgoingRiver == direction)
		{
			return;
		}

		//ensure that there is a neighbor in the desired direction. 
		//Also, rivers cannot flow uphill. 
		//So we'll have to abort if the neighbor has a higher elevation.
		HexCell neighbor = GetNeighbor(direction);
		if (!IsValidRiverDestination(neighbor))
		{
			return;
		}

		//clear previous outgoing river n remove incoming river, 
		//if it overlaps with new outgoing river.
		RemoveOutgoingRiver();
		if (hasIncomingRiver && incomingRiver == direction)
		{
			RemoveIncomingRiver();
		}

		//setting the outgoing river.
		hasOutgoingRiver = true;
		outgoingRiver = direction;
		specialIndex = 0;

		//set the incoming river of the other cell, 
		//after removing its current incoming river, if any.
		neighbor.RemoveIncomingRiver();
		neighbor.hasIncomingRiver = true;
		neighbor.incomingRiver = direction.Opposite();
		neighbor.specialIndex = 0;

		SetRoad((int)direction, false);
	}

	//to retrieve the vertical position of its stream bed.
	public float StreamBedY
	{
		get
		{
			return
				(elevation + HexMetrics.streamBedElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	public float RiverSurfaceY
	{
		get
		{
			return// to retrieve the vertical position of its river's surface.
				(elevation + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	//submerged cell property
	public float WaterSurfaceY
	{
		get
		{
			return
				(waterLevel + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	//check whether the cell has a road in a certain direction.
	public bool HasRoadThroughEdge(HexDirection direction)
	{
		return roads[(int)direction];
	}

	public bool HasRoads
	{ //know whether a cell has at least one road
		get
		{
			for (int i = 0; i < roads.Length; i++)
			{
				if (roads[i])
				{
					return true; //road found
				}
			}
			return false;
		}
	}

	public void AddRoad(HexDirection direction)
	{
		if (
			!roads[(int)direction] && !HasRiverThroughEdge(direction) &&
			!IsSpecial && !GetNeighbor(direction).IsSpecial &&
			GetElevationDifference(direction) <= 1
			)
		{
			SetRoad((int)direction, true);
		}
	}

	public void RemoveRoads()
	{
		for (int i = 0; i < neighbors.Length; i++)
		{
			if (roads[i])
			{
				SetRoad(i, false);
			}
		}
	}

	void SetRoad(int index, bool state)
	{
		roads[index] = state; //remove road
							  //disable the corresponding roads of the cell's neighbors
		neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
		neighbors[index].RefreshSelfOnly(); //refresh cells neighbor
		RefreshSelfOnly(); //cell refresh self
	}

	public int GetElevationDifference(HexDirection direction)
	{
		int difference = elevation - GetNeighbor(direction).elevation;
		return difference >= 0 ? difference : -difference;
	}

	// To make sure that roads don't overlap with the water, 
	//we'll have to push the road center away from the river. To get the direction of the incoming or outgoing river,
	public HexDirection RiverBeginOrEndDirection
	{
		get
		{
			return hasIncomingRiver ? incomingRiver : outgoingRiver;
		}
	}

	//	set up waterlevel
	public int WaterLevel
	{
		get
		{
			return waterLevel;
		}
		set
		{
			if (waterLevel == value)
			{
				return;
			}
			waterLevel = value;
			ValidateRivers();
			Refresh();
		}
	}

	//check whether cell is underwater
	public bool IsUnderwater
	{
		get
		{
			return waterLevel > elevation;
		}
	}

	bool IsValidRiverDestination(HexCell neighbor)
	{
		return neighbor && (
			elevation >= neighbor.elevation || waterLevel == neighbor.elevation
		);
	}

	void ValidateRivers()
	{
		if (
			hasOutgoingRiver &&
			!IsValidRiverDestination(GetNeighbor(outgoingRiver))
		)
		{
			RemoveOutgoingRiver();
		}
		if (
			hasIncomingRiver &&
			!GetNeighbor(incomingRiver).IsValidRiverDestination(this)
		)
		{
			RemoveIncomingRiver();
		}
	}

	//adding features to be editable on the map
	public int UrbanLevel
	{
		get
		{
			return urbanLevel;
		}
		set
		{
			if (urbanLevel != value)
			{
				urbanLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int FarmLevel
	{
		get
		{
			return farmLevel;
		}
		set
		{
			if (farmLevel != value)
			{
				farmLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int PlantLevel
	{
		get
		{
			return plantLevel;
		}
		set
		{
			if (plantLevel != value)
			{
				plantLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	//Setup Wall property
	public bool Walled
	{
		get
		{
			return walled;
		}
		set
		{
			if (walled != value)
			{
				walled = value;
				Refresh();
			}
		}
	}

	public int SpecialIndex
	{
		get
		{
			return specialIndex;
		}
		set
		{
			if (specialIndex != value && !HasRiver)
			{
				specialIndex = value;
				RemoveRoads();
				RefreshSelfOnly();
			}
		}
	}

	public bool IsSpecial
	{
		get
		{
			return specialIndex > 0;
		}
	}

	public int TerrainTypeIndex
	{
		get
		{
			return terrainTypeIndex;
		}
		set
		{
			if (terrainTypeIndex != value)
			{
				terrainTypeIndex = value;
				Refresh();
			}
		}
	}

	public void Save(BinaryWriter writer)
	{
		writer.Write(terrainTypeIndex);
		writer.Write(elevation);
		writer.Write(waterLevel);
		writer.Write(urbanLevel);
		writer.Write(farmLevel);
		writer.Write(plantLevel);
		writer.Write(specialIndex);
		writer.Write(walled);

		writer.Write(hasIncomingRiver);
		writer.Write((int)incomingRiver);

		writer.Write(hasOutgoingRiver);
		writer.Write((int)outgoingRiver);

		for (int i = 0; i < roads.Length; i++) {
			writer.Write(roads[i]);
		}
	}

	public void Load(BinaryReader reader)
	{
		terrainTypeIndex = reader.ReadInt32();
		elevation = reader.ReadInt32();
		RefreshPosition();
		waterLevel = reader.ReadInt32();
		urbanLevel = reader.ReadInt32();
		farmLevel = reader.ReadInt32();
		plantLevel = reader.ReadInt32();
		specialIndex = reader.ReadInt32();
		walled = reader.ReadBoolean();

		hasIncomingRiver = reader.ReadBoolean();
		incomingRiver = (HexDirection)reader.ReadInt32();

		hasOutgoingRiver = reader.ReadBoolean();
		outgoingRiver = (HexDirection)reader.ReadInt32();

		for (int i = 0; i < roads.Length; i++) {
			roads[i] = reader.ReadBoolean();
		}
	}

	void RefreshPosition () {
		Vector3 position = transform.localPosition;
		position.y = elevation * HexMetrics.elevationStep;
		position.y +=
			(HexMetrics.SampleNoise(position).y * 2f - 1f) *
			HexMetrics.elevationPerturbStrength;
		transform.localPosition = position;

		Vector3 uiPosition = uiRect.localPosition;
		uiPosition.z = -position.y;
		uiRect.localPosition = uiPosition;
	}
}
