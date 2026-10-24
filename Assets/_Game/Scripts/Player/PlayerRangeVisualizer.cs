using UnityEngine;

namespace KillThemAll.Player
{
    [ExecuteAlways]
    public class PlayerRangeVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private float range = 8f;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.4f);
        [SerializeField] private bool showInGameView = true;
        [SerializeField] private int circleSegments = 64;
        [SerializeField] private float lineWidth = 0.05f;

        private LineRenderer _lineRenderer;

        public float Range => range;

        private void Awake()
        {
            SetupLineRenderer();
        }

        private void OnEnable()
        {
            SetupLineRenderer();
            UpdateCircleMesh();
        }

        public void SetRange(float newRange)
        {
            range = Mathf.Max(0.5f, newRange);
            UpdateCircleMesh();
        }

        private void SetupLineRenderer()
        {
            if (!showInGameView) return;

            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.positionCount = circleSegments;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = gizmoColor;
            _lineRenderer.endColor = gizmoColor;
            
            // Unlit URP material oluştur
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader != null && _lineRenderer.sharedMaterial == null)
            {
                Material mat = new Material(unlitShader);
                mat.color = gizmoColor;
                _lineRenderer.material = mat;
            }
        }

        private void UpdateCircleMesh()
        {
            if (_lineRenderer == null) return;

            float angleStep = 360f / circleSegments;
            for (int i = 0; i < circleSegments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * range;
                float z = Mathf.Cos(angle) * range;
                _lineRenderer.SetPosition(i, new Vector3(x, 0.05f, z));
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, range);
        }

        private void OnValidate()
        {
            if (Application.isPlaying || _lineRenderer != null)
            {
                SetupLineRenderer();
                UpdateCircleMesh();
            }
        }
    }
}
