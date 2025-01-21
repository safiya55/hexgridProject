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
		walls.Clear();
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
		EdgeVertices far, HexCell farCell
	)
	{
		if (nearCell.Walled != farCell.Walled)
		{
			AddWallSegment(near.v1, far.v1, near.v5, far.v5);
		}
	}

	void AddWallSegment(
		Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight
	)
	{
		Vector3 left = Vector3.Lerp(nearLeft, farLeft, 0.5f);
		Vector3 right = Vector3.Lerp(nearRight, farRight, 0.5f);

		Vector3 leftThicknessOffset =
			HexMetrics.WallThicknessOffset(nearLeft, farLeft);
		Vector3 rightThicknessOffset =
			HexMetrics.WallThicknessOffset(nearRight, farRight);

		Vector3 v1, v2, v3, v4;
		v1 = v3 = left - leftThicknessOffset;
		v2 = v4 = right - rightThicknessOffset;
		v3.y = v4.y = left.y + HexMetrics.wallHeight;
		walls.AddQuad(v1, v2, v3, v4);

		v1 = v3 = left + leftThicknessOffset;
		v2 = v4 = right + rightThicknessOffset;
		v3.y = v4.y = left.y + HexMetrics.wallHeight;
		walls.AddQuad(v2, v1, v4, v3);
	}
}