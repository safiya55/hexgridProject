using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{

    public Transform featurePrefab;

    public void Clear() { }

    public void Apply() { }

    public void AddFeature(Vector3 position)
    {
        Transform instance = Instantiate(featurePrefab);
        instance.localPosition = HexMetrics.Perturb(position);;
    }
}