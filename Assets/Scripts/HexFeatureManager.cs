using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
	Transform container;


	public HexFeatureCollection[]
		urbanCollections, farmCollections, plantCollections;

	public HexMesh walls;

	public void Clear()
	{
		//prevent duplicates when chunk refresh. get rid of old feature when chunk is cleared
		if (container)
		{
			Destroy(container.gameObject);
		}
		container = new GameObject("Features Container").transform;
		container.SetParent(transform, false);
		walls.Clear();
	}

	public void Apply()
	{
		walls.Apply();
	}


	//uses a level and hash value to select a prefab. If the level is larger than zero, we retrieve the thresholds using the level decreased by one. Then we loop through the thresholds until one exceeds 
	//the hash value. That means we found a prefab. If we didn't, we return null.
	Transform PickPrefab(
		HexFeatureCollection[] collection,
		int level, float hash, float choice
	)
	{
		if (level > 0)
		{
			float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
			for (int i = 0; i < thresholds.Length; i++)
			{
				if (hash < thresholds[i])
				{
					return collection[i].Pick(choice);
				}
			}
		}
		return null;
	}

	public void AddFeature(HexCell cell, Vector3 position)
	{
		// hash grid to obtain a value. Once we use that to set the rotation, 
		//our features will remain motionless when we edit the terrain.
		HexHash hash = HexMetrics.SampleHashGrid(position);

		//minimize population of features by certain percent
		Transform prefab = PickPrefab(
			urbanCollections, cell.UrbanLevel, hash.a, hash.d
		);
		Transform otherPrefab = PickPrefab(
			farmCollections, cell.FarmLevel, hash.b, hash.d
		);
		float usedHash = hash.a;
		if (prefab)
		{
			if (otherPrefab && hash.b < hash.a)
			{
				prefab = otherPrefab;
				usedHash = hash.b;
			}
		} //have farm feature appear
		else if (otherPrefab)
		{
			prefab = otherPrefab;
			usedHash = hash.b;
		}
		otherPrefab = PickPrefab(
			plantCollections, cell.PlantLevel, hash.c, hash.d
		);
		if (prefab)
		{
			if (otherPrefab && hash.c < usedHash)
			{
				prefab = otherPrefab;
			}
		}
		else if (otherPrefab)
		{
			prefab = otherPrefab;
		}
		else
		{
			return;
		}

		Transform instance = Instantiate(prefab);
		position.y += instance.localScale.y * 0.5f;
		instance.localPosition = HexMetrics.Perturb(position);


		//add random rotation to object
		instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
		instance.SetParent(container, false);
	}

	public void AddWall(
		EdgeVertices near, HexCell nearCell,
		EdgeVertices far, HexCell farCell,
		bool hasRiver, bool hasRoad
	)
	{
		if (nearCell.Walled != farCell.Walled &&
				!nearCell.IsUnderwater && !farCell.IsUnderwater &&
				nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff
			)
		{
			AddWallSegment(near.v1, far.v1, near.v2, far.v2);
			if (hasRiver || hasRoad)
			{
				// Leave a gap.
				AddWallCap(near.v2, far.v2);
				AddWallCap(far.v4, near.v4);
			}
			else
			{
				AddWallSegment(near.v2, far.v2, near.v3, far.v3);
				AddWallSegment(near.v3, far.v3, near.v4, far.v4);
			}
			AddWallSegment(near.v4, far.v4, near.v5, far.v5);
		}
	}

	void AddWallSegment(
		Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight
	)
	{
		nearLeft = HexMetrics.Perturb(nearLeft);
		farLeft = HexMetrics.Perturb(farLeft);
		nearRight = HexMetrics.Perturb(nearRight);
		farRight = HexMetrics.Perturb(farRight);

		Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
		Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

		Vector3 leftThicknessOffset =
			HexMetrics.WallThicknessOffset(nearLeft, farLeft);
		Vector3 rightThicknessOffset =
			HexMetrics.WallThicknessOffset(nearRight, farRight);
		//keeps the Y coordinates of the left and right top vertices separate. to close gap
		float leftTop = left.y + HexMetrics.wallHeight;
		float rightTop = right.y + HexMetrics.wallHeight;

		Vector3 v1, v2, v3, v4;
		v1 = v3 = left - leftThicknessOffset;
		v2 = v4 = right - rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);

		//makse the thickness of the walls visible from above
		Vector3 t1 = v3, t2 = v4;

		v1 = v3 = left + leftThicknessOffset;
		v2 = v4 = right + rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v2, v1, v4, v3);

		////makse the thickness of the walls visible from above
		walls.AddQuadUnperturbed(t1, t2, v3, v4);
	}

	void AddWallSegment(
		Vector3 pivot, HexCell pivotCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	)
	{
		//Eliminating unwanted corner segments that is underwater
		if (pivotCell.IsUnderwater) {
			return;
		}

		bool hasLeftWall = !leftCell.IsUnderwater &&
			pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
		bool hasRighWall = !rightCell.IsUnderwater &&
			pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

		if (hasLeftWall && hasRighWall) {
			AddWallSegment(pivot, left, pivot, right);
		}
	}

	//to figure out which corner is the pivot,
	public void AddWall(
		Vector3 c1, HexCell cell1,
		Vector3 c2, HexCell cell2,
		Vector3 c3, HexCell cell3
	)
	{
		if (cell1.Walled)
		{
			if (cell2.Walled)
			{
				if (!cell3.Walled)
				{
					AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
				}
			}
			else if (cell3.Walled)
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
			else
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
		}
		else if (cell2.Walled)
		{
			if (cell3.Walled)
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
			else
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
		}
		else if (cell3.Walled)
		{
			AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
		}
	}

	void AddWallCap(Vector3 near, Vector3 far)
	{
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = center.y + HexMetrics.wallHeight;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);
	}
}