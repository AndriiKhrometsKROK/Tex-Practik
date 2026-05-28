using UnityEngine;

public class TowerRangeIndicator : MonoBehaviour
{
    private const int SegmentCount = 96;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        EnsureLineRenderer();
        Hide();
    }

    public void Show(Vector3 position, float radius, Color color)
    {
        if (radius <= 0f)
        {
            Hide();
            return;
        }

        EnsureLineRenderer();

        transform.position = position;
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
        _lineRenderer.positionCount = SegmentCount + 1;

        for (int i = 0; i <= SegmentCount; i++)
        {
            float angle = i / (float)SegmentCount * Mathf.PI * 2f;
            Vector3 point = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            _lineRenderer.SetPosition(i, point);
        }

        _lineRenderer.enabled = true;
    }

    public void Hide()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }
    }

    private void EnsureLineRenderer()
    {
        if (_lineRenderer != null) return;

        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        _lineRenderer.loop = true;
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.widthMultiplier = 0.045f;
        _lineRenderer.numCapVertices = 4;
        _lineRenderer.numCornerVertices = 4;
        _lineRenderer.sortingOrder = 50;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }
}
