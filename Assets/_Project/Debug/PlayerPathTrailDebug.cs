using System.Collections.Generic;
using UnityEngine;

namespace NakeDev.Debugging
{
    /// <summary>
    /// Registra visualmente a trajetória de um objeto para homologação de movimento.
    /// Não participa da física nem das regras de gameplay.
    /// </summary>
    public sealed class PlayerPathTrailDebug : MonoBehaviour
    {
        [Header("Homologation Trail")]
        [SerializeField] private bool _trailEnabled = true;
        [SerializeField] private bool _developmentBuildOnly = true;
        [SerializeField, Min(0.01f)] private float _minimumPointDistance = 0.1f;
        [SerializeField, Min(2)] private int _maximumPoints = 500;
        [SerializeField, Min(0.01f)] private float _lineWidth = 0.04f;
        [SerializeField] private Color _lineColor = Color.white;
        [SerializeField] private int _sortingOrder = -10;

        private readonly List<Vector3> _points = new List<Vector3>();
        private LineRenderer _lineRenderer;
        private Material _runtimeMaterial;

        private void Awake()
        {
            if (_developmentBuildOnly && !Application.isEditor && !Debug.isDebugBuild)
            {
                enabled = false;
                return;
            }

            CreateLineRenderer();
            ApplyVisualSettings();
            ClearTrail();
        }

        private void LateUpdate()
        {
            if (!_trailEnabled)
            {
                if (_lineRenderer != null)
                    _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;
            Vector3 currentPosition = transform.position;

            if (_points.Count > 0 &&
                (currentPosition - _points[_points.Count - 1]).sqrMagnitude <
                _minimumPointDistance * _minimumPointDistance)
            {
                return;
            }

            _points.Add(currentPosition);

            if (_points.Count > _maximumPoints)
                _points.RemoveAt(0);

            _lineRenderer.positionCount = _points.Count;
            _lineRenderer.SetPositions(_points.ToArray());
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial == null) return;

            if (Application.isPlaying)
                Destroy(_runtimeMaterial);
            else
                DestroyImmediate(_runtimeMaterial);
        }

        [ContextMenu("Clear Homologation Trail")]
        public void ClearTrail()
        {
            _points.Clear();

            if (_lineRenderer == null) return;

            _lineRenderer.positionCount = 0;
            AddCurrentPosition();
        }

        private void CreateLineRenderer()
        {
            GameObject trailObject = new GameObject("HomologationPathTrail");
            trailObject.transform.SetParent(transform, false);
            _lineRenderer = trailObject.AddComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.textureMode = LineTextureMode.Stretch;
            _lineRenderer.numCapVertices = 2;
            _lineRenderer.numCornerVertices = 2;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader != null)
            {
                _runtimeMaterial = new Material(shader)
                {
                    name = "HomologationPathTrail (Runtime)"
                };
                _lineRenderer.material = _runtimeMaterial;
            }
        }

        private void ApplyVisualSettings()
        {
            _lineRenderer.startWidth = _lineWidth;
            _lineRenderer.endWidth = _lineWidth;
            _lineRenderer.startColor = _lineColor;
            _lineRenderer.endColor = _lineColor;
            _lineRenderer.sortingOrder = _sortingOrder;
        }

        private void AddCurrentPosition()
        {
            _points.Add(transform.position);
            _lineRenderer.positionCount = 1;
            _lineRenderer.SetPosition(0, transform.position);
        }
    }
}
