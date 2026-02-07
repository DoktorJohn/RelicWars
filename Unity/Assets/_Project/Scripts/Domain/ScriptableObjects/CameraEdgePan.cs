using Project.Scripts.Modules.Map;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Kort Grænser (1000x1000)")]
    [SerializeField] private bool _useLimits = true;
    [SerializeField] private float _minX = 0f;
    [SerializeField] private float _maxX = 1000f;
    [SerializeField] private float _minY = 0f;
    [SerializeField] private float _maxY = 1000f;

    private float _targetOrthographicSize;

    private void Awake()
    {
        if (_associatedCamera == null) _associatedCamera = GetComponent<Camera>();
        if (_associatedCamera != null) _targetOrthographicSize = _associatedCamera.orthographicSize;
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
            newPosition.x = Mathf.Clamp(newPosition.x, _minX, _maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, _minY, _maxY);
        }

        transform.position = newPosition;
    }

    private void ExecuteCameraZoomLogic()
    {
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
        }
    }
}