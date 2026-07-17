using Project.Scripts.Modules.Map;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Tilemaps;

public class CameraEdgePan : MonoBehaviour
{
    [Header("Referencer")]
    [SerializeField] private Camera _associatedCamera;

    [Header("Bevægelse Indstillinger (Pan)")]
    [SerializeField] private float _panSpeed = 3f;

    // Objektiv OBS: 150f er en meget stor grænse. 
    // Hvis den ignorerer UI, vil den panorerer så snart musen er i nærheden af vinduet.
    // Overvej at sætte denne til 10-20 for en mere præcis "Edge" følelse.
    [SerializeField] private float _edgeBoundary = 20f;

    [Header("Zoom Indstillinger")]
    [Range(1f, 50f)]
    [SerializeField] private float _zoomSensitivity = 15f;
    [SerializeField] private float _zoomInterpolationSpeed = 50f;
    [SerializeField] private float _minOrthographicSize = 5f;
    [SerializeField] private float _maxOrthographicSize = 40f;

    [Header("Kort Grænser (fallback indtil world-data er loadet)")]
    [SerializeField] private bool _useLimits = true;
    [SerializeField] private float _minX = 0f;
    [SerializeField] private float _maxX = 1000f;
    [SerializeField] private float _minY = 0f;
    [SerializeField] private float _maxY = 1000f;

    private float _targetOrthographicSize;
    private bool _hasDynamicMapBounds;
    private float _mapMinX;
    private float _mapMaxX;
    private float _mapMinY;
    private float _mapMaxY;
    private bool _hasPreviousTouchPosition;
    private Vector2 _previousTouchPosition;
    private float _previousPinchDistance;

    private void Awake()
    {
        if (_associatedCamera == null) _associatedCamera = GetComponent<Camera>();
        if (_associatedCamera != null) _targetOrthographicSize = _associatedCamera.orthographicSize;
    }

    public void ConfigureMapBounds(Tilemap tilemap, int worldWidth, int worldHeight)
    {
        if (tilemap == null || worldWidth <= 0 || worldHeight <= 0) return;

        int minimumCellX = -worldWidth / 2;
        int maximumCellX = minimumCellX + worldWidth - 1;
        int minimumCellY = -worldHeight / 2;
        int maximumCellY = minimumCellY + worldHeight - 1;

        Vector3[] corners =
        {
            tilemap.GetCellCenterWorld(new Vector3Int(minimumCellX, minimumCellY, 0)),
            tilemap.GetCellCenterWorld(new Vector3Int(minimumCellX, maximumCellY, 0)),
            tilemap.GetCellCenterWorld(new Vector3Int(maximumCellX, minimumCellY, 0)),
            tilemap.GetCellCenterWorld(new Vector3Int(maximumCellX, maximumCellY, 0))
        };

        _mapMinX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        _mapMaxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        _mapMinY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        _mapMaxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        _hasDynamicMapBounds = true;

        transform.position = ClampPositionToMap(transform.position);
    }

    private void Update()
    {
        if (_associatedCamera == null) return;

        ExecuteCameraPanLogic();
        ExecuteCameraZoomLogic();
    }

    private void ExecuteCameraPanLogic()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            ExecuteTouchPanAndPinch();
            return;
        }

        _hasPreviousTouchPosition = false;
        _previousPinchDistance = 0f;

        if (Mouse.current == null) return;

        // FIX: Vi fjerner checken for IsMouseOverUI her. 
        // Det gør at kameraet ALTID lytter til skærmens kanter, uanset om der er vinduer.

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 currentPosition = transform.position;
        float moveX = 0f;
        float moveY = 0f;

        // Pan mod højre
        if (mousePosition.x >= Screen.width - _edgeBoundary) moveX += _panSpeed * Time.deltaTime;
        // Pan mod venstre
        else if (mousePosition.x <= _edgeBoundary) moveX -= _panSpeed * Time.deltaTime;

        // Pan mod top
        if (mousePosition.y >= Screen.height - _edgeBoundary) moveY += _panSpeed * Time.deltaTime;
        // Pan mod bund
        else if (mousePosition.y <= _edgeBoundary) moveY -= _panSpeed * Time.deltaTime;

        Vector3 newPosition = currentPosition + new Vector3(moveX, moveY, 0);

        if (_useLimits)
        {
            newPosition = ClampPositionToMap(newPosition);
        }

        transform.position = newPosition;
    }

    private void ExecuteCameraZoomLogic()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return;
        }

        if (Mouse.current == null) return;

        // VIGTIGT: Vi BEHOLDER checken for Zoom.
        // Hvis du fjerner den her, vil kortet zoome ind/ud når du scroller i din enhedsliste.
        if (WorldMapInteractionHandler.Instance != null && WorldMapInteractionHandler.Instance.IsMouseOverUI) return;

        float scrollDelta = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            float zoomDirection = Mathf.Sign(scrollDelta);
            _targetOrthographicSize -= zoomDirection * _zoomSensitivity;
            _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
        }

        if (Mathf.Abs(_associatedCamera.orthographicSize - _targetOrthographicSize) > 0.001f)
        {
            _associatedCamera.orthographicSize = Mathf.Lerp(
                _associatedCamera.orthographicSize,
                _targetOrthographicSize,
                _zoomInterpolationSpeed * Time.deltaTime
            );

            if (_useLimits) transform.position = ClampPositionToMap(transform.position);
        }
    }

    private Vector3 ClampPositionToMap(Vector3 position)
    {
        float minimumX = _hasDynamicMapBounds ? _mapMinX : _minX;
        float maximumX = _hasDynamicMapBounds ? _mapMaxX : _maxX;
        float minimumY = _hasDynamicMapBounds ? _mapMinY : _minY;
        float maximumY = _hasDynamicMapBounds ? _mapMaxY : _maxY;

        if (_hasDynamicMapBounds && _associatedCamera != null)
        {
            float halfHeight = _associatedCamera.orthographicSize;
            float halfWidth = halfHeight * _associatedCamera.aspect;
            ClampAxisToViewport(ref minimumX, ref maximumX, halfWidth);
            ClampAxisToViewport(ref minimumY, ref maximumY, halfHeight);
        }

        position.x = Mathf.Clamp(position.x, minimumX, maximumX);
        position.y = Mathf.Clamp(position.y, minimumY, maximumY);
        return position;
    }

    private static void ClampAxisToViewport(ref float minimum, ref float maximum, float halfViewportSize)
    {
        if (maximum - minimum <= halfViewportSize * 2f)
        {
            float center = (minimum + maximum) * 0.5f;
            minimum = center;
            maximum = center;
            return;
        }

        minimum += halfViewportSize;
        maximum -= halfViewportSize;
    }

    private void ExecuteTouchPanAndPinch()
    {
        if (WorldMapInteractionHandler.Instance != null && WorldMapInteractionHandler.Instance.IsMouseOverUI)
        {
            _hasPreviousTouchPosition = false;
            _previousPinchDistance = 0f;
            return;
        }

        int activeTouches = GetActiveTouchPositions(out Vector2 firstTouch, out Vector2 secondTouch);
        if (activeTouches == 0)
        {
            _hasPreviousTouchPosition = false;
            _previousPinchDistance = 0f;
            return;
        }

        Vector2 currentPanPosition = activeTouches > 1
            ? (firstTouch + secondTouch) * 0.5f
            : firstTouch;

        if (_hasPreviousTouchPosition)
        {
            Vector2 screenDelta = _previousTouchPosition - currentPanPosition;
            float worldUnitsPerScreenPixel = (_associatedCamera.orthographicSize * 2f) / Mathf.Max(Screen.height, 1);
            Vector3 nextPosition = transform.position + new Vector3(
                screenDelta.x * worldUnitsPerScreenPixel,
                screenDelta.y * worldUnitsPerScreenPixel,
                0f);

            transform.position = _useLimits ? ClampPositionToMap(nextPosition) : nextPosition;
        }

        _previousTouchPosition = currentPanPosition;
        _hasPreviousTouchPosition = true;

        if (activeTouches < 2)
        {
            _previousPinchDistance = 0f;
            return;
        }

        float currentPinchDistance = Vector2.Distance(firstTouch, secondTouch);
        if (_previousPinchDistance > 0f)
        {
            float pinchDelta = currentPinchDistance - _previousPinchDistance;
            _targetOrthographicSize -= pinchDelta / Mathf.Max(Screen.height, 1) * _zoomSensitivity;
            _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
        }

        _previousPinchDistance = currentPinchDistance;
    }

    private static int GetActiveTouchPositions(out Vector2 firstTouch, out Vector2 secondTouch)
    {
        firstTouch = default;
        secondTouch = default;
        int activeTouchCount = 0;

        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed)
            {
                continue;
            }

            if (activeTouchCount == 0)
            {
                firstTouch = touch.position.ReadValue();
            }
            else if (activeTouchCount == 1)
            {
                secondTouch = touch.position.ReadValue();
            }

            activeTouchCount++;
            if (activeTouchCount == 2)
            {
                break;
            }
        }

        return activeTouchCount;
    }
}
