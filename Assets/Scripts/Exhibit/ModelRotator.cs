using UnityEngine;

/// <summary>
/// 3D 模型旋转/缩放控制器，支持鼠标拖拽旋转和滚轮缩放
/// 点击模型触发弹跳动画并自动打开详情抽屉
/// </summary>
public class ModelRotator : MonoBehaviour
{
    [Header("旋转设置")]
    public float rotateSpeed = 0.35f;
    public bool autoRotate = true;
    public float autoRotateSpeed = 30f;

    [Header("缩放设置")]
    public float zoomSpeed = 3f;
    public float minScale = 0.3f;
    public float maxScale = 3f;

    [Header("点击交互")]
    public float clickThreshold = 8f;

    private float currentScale = 1f;
    private bool isDragging;
    private Vector3 lastFrameMousePos;
    private Vector3 mouseDownPos;
    private bool wasClick;

    /// <summary>
    /// 点击模型时触发的回调
    /// </summary>
    public System.Action onModelClicked;

    private void Update()
    {
        // 自动旋转
        if (autoRotate && !isDragging)
        {
            transform.Rotate(Vector3.up, autoRotateSpeed * Time.deltaTime);
        }

        // 鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            wasClick = true;
            mouseDownPos = Input.mousePosition;
            lastFrameMousePos = Input.mousePosition;
        }

        // 鼠标拖拽旋转
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 delta = currentMousePos - mouseDownPos;
            if (delta.magnitude > clickThreshold)
                wasClick = false;

            Vector3 frameDelta = currentMousePos - lastFrameMousePos;
            if (frameDelta.magnitude > 0.1f)
            {
                transform.Rotate(Vector3.up, -frameDelta.x * rotateSpeed, Space.World);
                transform.Rotate(Vector3.right, frameDelta.y * rotateSpeed, Space.World);
            }
            lastFrameMousePos = currentMousePos;
        }

        // 鼠标松开 — 检测点击
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            if (wasClick && IsMouseOnModel())
            {
                onModelClicked?.Invoke();
                StartCoroutine(BounceAnimation());
            }
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

    /// <summary>
    /// 射线检测鼠标是否指向当前模型
    /// </summary>
    private bool IsMouseOnModel()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            // 检测命中是否属于当前模型的子对象
            return hit.transform.IsChildOf(transform) || hit.transform == transform;
        }
        return false;
    }

    /// <summary>
    /// 点击弹跳动画
    /// </summary>
    private System.Collections.IEnumerator BounceAnimation()
    {
        float duration = 0.3f;
        float t = 0;
        Vector3 origScale = Vector3.one * currentScale;
        Vector3 peakScale = origScale * 1.15f;

        while (t < duration * 0.5f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(origScale, peakScale, t / (duration * 0.5f));
            yield return null;
        }
        t = 0;
        while (t < duration * 0.5f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(peakScale, origScale, t / (duration * 0.5f));
            yield return null;
        }
        transform.localScale = origScale;
    }
}
