using UnityEngine;

/// <summary>
/// 角色控制器，使用状态机管理 Idle/Walking/Interacting 三种状态
/// 通过代码驱动动画（旋转、位移动画），无需 Animator Controller 资源文件
/// </summary>
public class CharacterController2D : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 3f;
    public float idleBobSpeed = 2f;
    public float idleBobAmount = 0.05f;

    [Header("动画设置")]
    public float walkLegSwingSpeed = 8f;
    public float walkLegSwingAmount = 30f;
    public float interactSpinSpeed = 720f;

    // 状态机
    private CharacterState currentState = CharacterState.Idle;
    private float stateTimer;

    // 角色部件引用
    private Transform bodyTransform;
    private Transform headTransform;
    private Transform leftLegTransform;
    private Transform rightLegTransform;

    // 移动目标
    private Vector3 targetPosition;
    private bool facingRight = true;

    private void Awake()
    {
        CacheParts();
    }

    private void CacheParts()
    {
        // 查找角色部件（由 MainPanel.Create3DCharacter 创建的几何体小人结构）
        // 身体=第一个子对象(Cylinder), 头=第二个(Sphere), 左腿=第三个(Cylinder), 右腿=第四个(Cylinder)
        if (transform.childCount >= 4)
        {
            bodyTransform = transform.GetChild(0);
            headTransform = transform.GetChild(1);
            leftLegTransform = transform.GetChild(2);
            rightLegTransform = transform.GetChild(3);
        }
    }

    private void Update()
    {
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case CharacterState.Idle:
                UpdateIdle();
                break;
            case CharacterState.Walking:
                UpdateWalking();
                break;
            case CharacterState.Interacting:
                UpdateInteracting();
                break;
        }
    }

    #region State Updates

    private void UpdateIdle()
    {
        // 待机动画：轻微上下浮动
        if (bodyTransform != null)
        {
            float bob = Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmount;
            bodyTransform.localPosition = new Vector3(0, 0.8f + bob, 0);
        }
        if (headTransform != null)
        {
            float bob = Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmount;
            headTransform.localPosition = new Vector3(0, 1.55f + bob, 0);
        }

        // 腿复位
        ResetLegs();
    }

    private void UpdateWalking()
    {
        // 向目标移动
        Vector3 pos = transform.position;
        float dx = targetPosition.x - pos.x;
        float dist = Mathf.Abs(dx);

        if (dist > 0.15f)
        {
            float dir = Mathf.Sign(dx);
            pos.x += dir * moveSpeed * Time.deltaTime;
            transform.position = pos;

            // 朝向
            if (dir > 0 && !facingRight) Flip();
            if (dir < 0 && facingRight) Flip();

            // 行走动画：腿部摆动
            float swing = Mathf.Sin(Time.time * walkLegSwingSpeed) * walkLegSwingAmount;
            if (leftLegTransform != null)
                leftLegTransform.localRotation = Quaternion.Euler(swing, 0, 0);
            if (rightLegTransform != null)
                rightLegTransform.localRotation = Quaternion.Euler(-swing, 0, 0);

            // 身体轻微晃动
            if (bodyTransform != null)
                bodyTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * walkLegSwingSpeed) * 2f);
        }
        else
        {
            // 到达目标，切换回 Idle
            ChangeState(CharacterState.Idle);
        }
    }

    private void UpdateInteracting()
    {
        // 交互动画：身体旋转
        if (bodyTransform != null)
        {
            bodyTransform.Rotate(Vector3.up, interactSpinSpeed * Time.deltaTime);
        }
        if (headTransform != null)
        {
            headTransform.Rotate(Vector3.up, interactSpinSpeed * Time.deltaTime);
        }

        // 手臂摆动效果（用腿代替）
        float wave = Mathf.Sin(Time.time * 10f) * 45f;
        if (leftLegTransform != null)
            leftLegTransform.localRotation = Quaternion.Euler(0, 0, wave);
        if (rightLegTransform != null)
            rightLegTransform.localRotation = Quaternion.Euler(0, 0, -wave);

        // 1秒后回到 Idle
        if (stateTimer > 1f)
        {
            if (bodyTransform != null) bodyTransform.localRotation = Quaternion.identity;
            if (headTransform != null) headTransform.localRotation = Quaternion.identity;
            ResetLegs();
            ChangeState(CharacterState.Idle);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 让角色走向指定位置
    /// </summary>
    public void WalkTo(Vector3 target)
    {
        targetPosition = target;
        targetPosition.y = transform.position.y;
        targetPosition.z = transform.position.z;
        ChangeState(CharacterState.Walking);
    }

    /// <summary>
    /// 触发交互动画
    /// </summary>
    public void Interact()
    {
        ChangeState(CharacterState.Interacting);
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public CharacterState GetCurrentState() => currentState;

    #endregion

    #region Internal

    private void ChangeState(CharacterState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        stateTimer = 0f;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void ResetLegs()
    {
        if (leftLegTransform != null) leftLegTransform.localRotation = Quaternion.identity;
        if (rightLegTransform != null) rightLegTransform.localRotation = Quaternion.identity;
        if (bodyTransform != null) bodyTransform.localRotation = Quaternion.identity;
    }

    #endregion
}
