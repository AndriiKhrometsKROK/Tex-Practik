// Автоматично повертає тимчасовий об'єкт до пулу після завершення його часу життя.
using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public GameObject SourcePrefab { get; private set; }
    public bool IsInPool { get; private set; }

    public void Initialize(GameObject sourcePrefab)
    {
        SourcePrefab = sourcePrefab;
    }

    public void MarkSpawned()
    {
        IsInPool = false;
    }

    public void MarkReturned()
    {
        IsInPool = true;
    }
}
