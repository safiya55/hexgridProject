using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    Transform container;

    public Transform featurePrefab;

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

    public void AddFeature(Vector3 position)
    {
        Transform instance = Instantiate(featurePrefab);
        instance.localPosition = HexMetrics.Perturb(position); ;
    }
}