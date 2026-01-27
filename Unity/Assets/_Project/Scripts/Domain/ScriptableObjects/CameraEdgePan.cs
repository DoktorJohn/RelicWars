using UnityEngine;
using UnityEngine.InputSystem; // Det her er den vigtige tilføjelse!

public class CameraEdgePan : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _panSpeed = 10f;
    [SerializeField] private float _edgeBoundary = 20f;

    [Header("Map Limits (1000x1000)")]
    [SerializeField] private bool _useLimits = true;
    [SerializeField] private float _minX = 0f;
    [SerializeField] private float _maxX = 1000f;
    [SerializeField] private float _minY = 0f;
    [SerializeField] private float _maxY = 1000f;

    private void Update()
    {
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
}