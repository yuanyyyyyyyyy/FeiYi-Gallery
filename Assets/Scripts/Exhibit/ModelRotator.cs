using UnityEngine;

/// <summary>
/// 3D 模型旋转/缩放控制器，支持鼠标拖拽旋转和滚轮缩放
/// </summary>
public class ModelRotator : MonoBehaviour
{
    [Header("旋转设置")]
    public float rotateSpeed = 5f;
    public bool autoRotate = true;
    public float autoRotateSpeed = 30f;

    [Header("缩放设置")]
    public float zoomSpeed = 2f;
    public float minScale = 0.3f;
    public float maxScale = 3f;

    private float currentScale = 1f;
    private bool isDragging;
    private Vector3 lastMousePos;

    private void Update()
    {
        // 自动旋转
        if (autoRotate && !isDragging)
        {
            transform.Rotate(Vector3.up, autoRotateSpeed * Time.deltaTime);
        }

        // 鼠标拖拽旋转
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            transform.Rotate(Vector3.up, -delta.x * rotateSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, delta.y * rotateSpeed * Time.deltaTime, Space.World);
            lastMousePos = Input.mousePosition;
        }

        // 滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentScale += scroll * zoomSpeed * Time.deltaTime * 10f;
            currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
            transform.localScale = Vector3.one * currentScale;
        }
    }
}
