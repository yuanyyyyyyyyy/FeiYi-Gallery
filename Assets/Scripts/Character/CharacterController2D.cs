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
    public float walkArmSwingAmount = 25f;
    public float interactSpinSpeed = 720f;

    [Header("跳跃设置")]
    public float jumpHeight = 0.8f;
    public float jumpDuration = 0.6f;

    // 状态机
    private CharacterState currentState = CharacterState.Idle;
    private float stateTimer;

    // 角色部件引用
    private Transform bodyTransform;
    private Transform headTransform;
    private Transform leftLegTransform;
    private Transform rightLegTransform;
    private Transform leftArmTransform;
    private Transform rightArmTransform;

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
        // 0=Body, 1=Head, 2=LeftLeg, 3=RightLeg, 4=LeftArm, 5=RightArm
        if (transform.childCount >= 6)
        {
            bodyTransform = transform.GetChild(0);
            headTransform = transform.GetChild(1);
            leftLegTransform = transform.GetChild(2);
            rightLegTransform = transform.GetChild(3);
            leftArmTransform = transform.GetChild(4);
            rightArmTransform = transform.GetChild(5);
        }
        else if (transform.childCount >= 4)
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
            case CharacterState.Jumping:
                UpdateJumping();
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

        // 手臂自然微摆
        if (leftArmTransform != null)
            leftArmTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.5f) * 3f);
        if (rightArmTransform != null)
            rightArmTransform.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(Time.time * 1.5f) * 3f);

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

            // 行走动画：腿部前后摆动
            float swing = Mathf.Sin(Time.time * walkLegSwingSpeed) * walkLegSwingAmount;
            if (leftLegTransform != null)
                leftLegTransform.localRotation = Quaternion.Euler(swing, 0, 0);
            if (rightLegTransform != null)
                rightLegTransform.localRotation = Quaternion.Euler(-swing, 0, 0);

            // 行走动画：手臂前后摆动（与腿反向）
            if (leftArmTransform != null)
                leftArmTransform.localRotation = Quaternion.Euler(-swing * walkArmSwingAmount / walkLegSwingAmount, 0, 0);
            if (rightArmTransform != null)
                rightArmTransform.localRotation = Quaternion.Euler(swing * walkArmSwingAmount / walkLegSwingAmount, 0, 0);

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
        // 交互动画：手臂挥舞
        float wave = Mathf.Sin(Time.time * 10f) * 45f;
        if (leftArmTransform != null)
            leftArmTransform.localRotation = Quaternion.Euler(wave, 0, -30f);
        if (rightArmTransform != null)
            rightArmTransform.localRotation = Quaternion.Euler(-wave, 0, 30f);

        // 身体轻微旋转
        if (bodyTransform != null)
            bodyTransform.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 6f) * 10f, 0);

        // 头部轻微摆动
        if (headTransform != null)
            headTransform.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 8f) * 8f, 0);

        // 1.2秒后回到 Idle
        if (stateTimer > 1.2f)
        {
            ResetParts();
            ChangeState(CharacterState.Idle);
        }
    }

    private void UpdateJumping()
    {
        // 抛物线跳跃：stateTimer 0→jumpDuration
        float t = Mathf.Clamp01(stateTimer / jumpDuration);
        // 抛物线高度：h = 4*jumpHeight*t*(1-t)
        float h = 4f * jumpHeight * t * (1f - t);

        // 腿收起
        if (leftLegTransform != null)
            leftLegTransform.localRotation = Quaternion.Euler(-30f * (1f - t), 0, 0);
        if (rightLegTransform != null)
            rightLegTransform.localRotation = Quaternion.Euler(-30f * (1f - t), 0, 0);

        // 手臂上举
        if (leftArmTransform != null)
            leftArmTransform.localRotation = Quaternion.Euler(-60f * (1f - t), 0, 0);
        if (rightArmTransform != null)
            rightArmTransform.localRotation = Quaternion.Euler(-60f * (1f - t), 0, 0);

        // 身体整体上移
        if (bodyTransform != null)
            bodyTransform.localPosition = new Vector3(0, 0.8f + h, 0);
        if (headTransform != null)
            headTransform.localPosition = new Vector3(0, 1.55f + h, 0);
        if (leftArmTransform != null)
            leftArmTransform.localPosition = new Vector3(-0.35f, 0.85f + h, 0);
        if (rightArmTransform != null)
            rightArmTransform.localPosition = new Vector3(0.35f, 0.85f + h, 0);
        if (leftLegTransform != null)
            leftLegTransform.localPosition = new Vector3(-0.12f, 0.2f + h * 0.3f, 0);
        if (rightLegTransform != null)
            rightLegTransform.localPosition = new Vector3(0.12f, 0.2f + h * 0.3f, 0);

        // 跳跃结束
        if (stateTimer >= jumpDuration)
        {
            ResetParts();
            // 复位位置
            if (bodyTransform != null) bodyTransform.localPosition = new Vector3(0, 0.8f, 0);
            if (headTransform != null) headTransform.localPosition = new Vector3(0, 1.55f, 0);
            if (leftArmTransform != null) leftArmTransform.localPosition = new Vector3(-0.35f, 0.85f, 0);
            if (rightArmTransform != null) rightArmTransform.localPosition = new Vector3(0.35f, 0.85f, 0);
            if (leftLegTransform != null) leftLegTransform.localPosition = new Vector3(-0.12f, 0.2f, 0);
            if (rightLegTransform != null) rightLegTransform.localPosition = new Vector3(0.12f, 0.2f, 0);
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
    /// 触发跳跃动画
    /// </summary>
    public void Jump()
    {
        if (currentState == CharacterState.Jumping) return;
        ChangeState(CharacterState.Jumping);
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

    private void ResetParts()
    {
        ResetLegs();
        if (leftArmTransform != null) leftArmTransform.localRotation = Quaternion.identity;
        if (rightArmTransform != null) rightArmTransform.localRotation = Quaternion.identity;
        if (headTransform != null) headTransform.localRotation = Quaternion.identity;
    }

    #endregion
}
