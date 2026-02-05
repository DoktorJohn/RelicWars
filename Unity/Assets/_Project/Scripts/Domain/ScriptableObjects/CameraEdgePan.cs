using UnityEngine;
using UnityEngine.InputSystem;

public class CameraEdgePan : MonoBehaviour
{
    [Header("Referencer")]
    [SerializeField] private Camera _associatedCamera;

    [Header("Bevægelse Indstillinger (Pan)")]
    [SerializeField] private float _panSpeed = 15f;
    [SerializeField] private float _edgeBoundary = 25f;

    [Header("Zoom Indstillinger (Hurtigere)")]
    [Range(1f, 50f)]
    [SerializeField] private float _zoomSensitivity = 15f; // Skruet op fra 2 til 15
    [SerializeField] private float _zoomInterpolationSpeed = 50f; // Skruet op fra 10 til 50
    [SerializeField] private float _minOrthographicSize = 5f;
    [SerializeField] private float _maxOrthographicSize = 40f;

    [Header("Kort Grænser (1000x1000)")]
    [SerializeField] private bool _useLimits = true;
    [SerializeField] private float _minX = 0f;
    [SerializeField] private float _maxX = 1000f;
    [SerializeField] private float _minY = 0f;
    [SerializeField] private float _maxY = 1000f;

    private float _targetOrthographicSize;

    private void Awake()
    {
        if (_associatedCamera == null)
        {
            _associatedCamera = GetComponent<Camera>();
        }

        if (_associatedCamera != null)
        {
            _targetOrthographicSize = _associatedCamera.orthographicSize;
        }
    }

    private void Update()
    {
        if (_associatedCamera == null) return;

        ExecuteCameraPanLogic();
        ExecuteCameraZoomLogic();
    }

    private void ExecuteCameraPanLogic()
    {
        if (Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 currentPosition = transform.position;
        float moveX = 0f;
        float moveY = 0f;

        if (mousePosition.x >= Screen.width - _edgeBoundary)
            moveX += _panSpeed * Time.deltaTime;
        if (mousePosition.x <= _edgeBoundary)
            moveX -= _panSpeed * Time.deltaTime;
        if (mousePosition.y >= Screen.height - _edgeBoundary)
            moveY += _panSpeed * Time.deltaTime;
        if (mousePosition.y <= _edgeBoundary)
            moveY -= _panSpeed * Time.deltaTime;

        Vector3 newPosition = currentPosition + new Vector3(moveX, moveY, 0);

        if (_useLimits)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, _minX, _maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, _minY, _maxY);
        }

        transform.position = newPosition;
    }

    private void ExecuteCameraZoomLogic()
    {
        if (Mouse.current == null) return;

        // Hent scroll-input
        float scrollDelta = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            // Vi normaliserer scrollDelta (typisk 120/-120) til 1/-1 og ganger med sensitivity
            float zoomDirection = Mathf.Sign(scrollDelta);
            _targetOrthographicSize -= zoomDirection * _zoomSensitivity;

            // Hold værdien inden for de tilladte grænser
            _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
        }

        // Flyt kameraets størrelse hurtigt mod målet for en responsiv følelse
        if (Mathf.Abs(_associatedCamera.orthographicSize - _targetOrthographicSize) > 0.001f)
        {
            _associatedCamera.orthographicSize = Mathf.Lerp(
                _associatedCamera.orthographicSize,
                _targetOrthographicSize,
                _zoomInterpolationSpeed * Time.deltaTime
            );
        }
    }
}