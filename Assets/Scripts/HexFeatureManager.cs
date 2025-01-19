using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    Transform container;

    public Transform[] urbanPrefabs;

    public void Clear()
    {
        //prevent duplicates when chunk refresh. get rid of old feature when chunk is cleared
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }

    public void Apply() { }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        // hash grid to obtain a value. Once we use that to set the rotation, 
        //our features will remain motionless when we edit the terrain.
        HexHash hash = HexMetrics.SampleHashGrid(position);

        //bail if 
        if (hash.a >= 0.25f) {
			return;
		}
        
        Transform instance = Instantiate(urbanPrefabs[cell.UrbanLevel - 1]);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        

        //add random rotation to object
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.b, 0f);
        instance.SetParent(container, false);
    }
}