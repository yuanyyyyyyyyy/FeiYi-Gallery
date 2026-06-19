using UnityEngine;

/// <summary>
/// Q版白色小猫控制器 — 连续胶囊体（头身一体）、三角耳、竖椭圆眼
/// 部件: 0=Body 1=Head 2=LeftEar 3=RightEar 4=LeftEye 5=RightEye 6=Nose(隐藏) 7=Scarf 8=Tail 9=LeftLeg(隐藏) 10=RightLeg(隐藏)
/// </summary>
public class CharacterController2D : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 1.2f;
    public float idleBobSpeed = 2.5f;
    public float idleBobAmount = 0.02f;

    [Header("动画设置")]
    public float walkHopSpeed = 8f;
    public float walkHopAmount = 0.05f;

    private CharacterState currentState = CharacterState.Idle;
    private float stateTimer;

    // 部件
    private Transform bodyTransform;
    private Transform headTransform;
    private Transform leftEarTransform;
    private Transform rightEarTransform;
    private Transform leftEyeTransform;
    private Transform rightEyeTransform;
    private Transform noseTransform;
    private Transform scarfTransform;
    private Transform tailTransform;
    private Transform leftLegTransform;
    private Transform rightLegTransform;

    private Vector3 targetPosition;
    private bool facingRight = true;
    private float blinkTimer;
    private float nextBlinkDelay = 3f;

    // 默认位置 — 连续胶囊体，头身一体
    private static readonly Vector3 BodyPos   = new Vector3(0, 0.28f, 0);
    private static readonly Vector3 BodyScale = new Vector3(0.22f, 0.40f, 0.22f);
    private static readonly Vector3 HeadPos  = new Vector3(0, 0.58f, -0.02f);
    private static readonly Vector3 HeadScale = new Vector3(0.24f, 0.24f, 0.24f);
    private static readonly Vector3 LEarPos  = new Vector3(-0.10f, 0.68f, -0.02f);
    private static readonly Vector3 REarPos  = new Vector3(0.10f, 0.68f, -0.02f);
    private static readonly Vector3 LEyePos  = new Vector3(-0.08f, 0.56f, -0.14f);
    private static readonly Vector3 REyePos  = new Vector3(0.08f, 0.56f, -0.14f);
    private static readonly Vector3 LEyeScale = new Vector3(0.04f, 0.08f, 0.02f);
    private static readonly Vector3 NosePos  = new Vector3(0, 0.46f, -0.16f);
    private static readonly Vector3 ScarfPos = new Vector3(0, 0.38f, 0.0f);
    private static readonly Vector3 TailPos  = new Vector3(0.12f, 0.18f, 0.04f);
    private static readonly Vector3 LLegPos  = new Vector3(0, 0, 0);
    private static readonly Vector3 RLegPos  = new Vector3(0, 0, 0);

    private void Awake() => CacheParts();

    private void CacheParts()
    {
        if (transform.childCount >= 11)
        {
            bodyTransform      = transform.GetChild(0);
            headTransform      = transform.GetChild(1);
            leftEarTransform   = transform.GetChild(2);
            rightEarTransform  = transform.GetChild(3);
            leftEyeTransform   = transform.GetChild(4);
            rightEyeTransform  = transform.GetChild(5);
            noseTransform      = transform.GetChild(6);
            scarfTransform     = transform.GetChild(7);
            tailTransform      = transform.GetChild(8);
            leftLegTransform   = transform.GetChild(9);
            rightLegTransform  = transform.GetChild(10);
        }
    }

    private void Update()
    {
        stateTimer += Time.deltaTime;
        UpdateBlink();
        switch (currentState)
        {
            case CharacterState.Idle:      UpdateIdle();      break;
            case CharacterState.Walking:    UpdateWalking();    break;
            case CharacterState.Speaking:   UpdateSpeaking();   break;
            case CharacterState.Sitting:    UpdateSitting();    break;
            case CharacterState.LyingDown:  UpdateLyingDown();  break;
            case CharacterState.Waving:     UpdateWaving();     break;
            case CharacterState.Dancing:    UpdateDancing();    break;
        }
    }

    private void UpdateBlink()
    {
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= nextBlinkDelay)
        {
            StartCoroutine(BlinkOnce());
            blinkTimer = 0f;
            nextBlinkDelay = Random.Range(2.5f, 5f);
        }
    }

    private System.Collections.IEnumerator BlinkOnce()
    {
        float duration = 0.15f;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float s = Mathf.Sin((t / duration) * Mathf.PI);
            float eyeY = Mathf.Lerp(LEyeScale.y, 0.01f, s);
            if (leftEyeTransform != null)
                leftEyeTransform.localScale = new Vector3(LEyeScale.x, eyeY, LEyeScale.z);
            if (rightEyeTransform != null)
                rightEyeTransform.localScale = new Vector3(LEyeScale.x, eyeY, LEyeScale.z);
            yield return null;
        }
        if (leftEyeTransform != null) leftEyeTransform.localScale = LEyeScale;
        if (rightEyeTransform != null) rightEyeTransform.localScale = LEyeScale;
    }

    #region State Updates

    private void UpdateIdle()
    {
        float bob = Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmount;
        if (bodyTransform != null)
            bodyTransform.localPosition = BodyPos + new Vector3(0, bob, 0);
        MoveHeadGroup(HeadPos + new Vector3(0, bob, 0));

        if (leftEarTransform != null)
            leftEarTransform.localRotation = Quaternion.Euler(0, 0, 45f + Mathf.Sin(Time.time * 3f) * 3f);
        if (rightEarTransform != null)
            rightEarTransform.localRotation = Quaternion.Euler(0, 0, -45f - Mathf.Sin(Time.time * 3f) * 3f);

        if (tailTransform != null)
            tailTransform.localRotation = Quaternion.Euler(0, 0, -50f + Mathf.Sin(Time.time * 2f) * 15f);

        if (scarfTransform != null)
        {
            scarfTransform.localPosition = ScarfPos + new Vector3(0, bob * 0.5f, 0);
            scarfTransform.localRotation = Quaternion.Euler(90, 0, Mathf.Sin(Time.time * 2f) * 2f);
        }
    }

    private void UpdateWalking()
    {
        Vector3 pos = transform.position;
        float dx = targetPosition.x - pos.x;
        float dist = Mathf.Abs(dx);

        if (dist > 0.12f)
        {
            float dir = Mathf.Sign(dx);
            pos.x += dir * moveSpeed * Time.deltaTime;
            float hop = Mathf.Abs(Mathf.Sin(Time.time * walkHopSpeed)) * walkHopAmount;
            pos.y = hop;
            transform.position = pos;

            if (dir > 0 && !facingRight) Flip();
            if (dir < 0 && facingRight) Flip();

            if (tailTransform != null)
                tailTransform.localRotation = Quaternion.Euler(0, 0, -20f + Mathf.Sin(Time.time * 6f) * 15f);

            if (bodyTransform != null)
                bodyTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * walkHopSpeed) * 3f);
        }
        else
        {
            ResetParts();
            ResetPositions();
            transform.position = new Vector3(pos.x, 0, pos.z);
            ChangeState(CharacterState.Idle);
        }
    }

    private void UpdateSpeaking()
    {
        float nod = Mathf.Sin(Time.time * 4f) * 4f;
        if (headTransform != null)
            headTransform.localRotation = Quaternion.Euler(nod, 0, 0);

        if (tailTransform != null)
            tailTransform.localRotation = Quaternion.Euler(0, 0, -50f + Mathf.Sin(Time.time * 5f) * 25f);

        if (leftEarTransform != null)
            leftEarTransform.localRotation = Quaternion.Euler(0, 0, 45f);
        if (rightEarTransform != null)
            rightEarTransform.localRotation = Quaternion.Euler(0, 0, -45f);

        if (scarfTransform != null)
            scarfTransform.localRotation = Quaternion.Euler(90, 0, Mathf.Sin(Time.time * 4f) * 3f);
    }

    private void UpdateSitting()
    {
        // 坐姿：整个身体下压变扁，头身一起压
        if (bodyTransform != null)
        {
            bodyTransform.localPosition = new Vector3(0, 0.16f, 0);
            bodyTransform.localScale = new Vector3(0.26f, 0.30f, 0.22f);
        }
        MoveHeadGroup(new Vector3(0, 0.40f, -0.02f));

        if (tailTransform != null)
            tailTransform.localRotation = Quaternion.Euler(0, 0, 80f);

        if (scarfTransform != null)
            scarfTransform.localPosition = new Vector3(0, 0.26f, 0f);

        float breath = Mathf.Sin(Time.time * 1.5f) * 0.01f;
        if (bodyTransform != null)
            bodyTransform.localScale += new Vector3(0, breath, 0);
    }

    private void UpdateLyingDown()
    {
        // 躺姿：身体贴地变扁
        if (bodyTransform != null)
        {
            bodyTransform.localPosition = new Vector3(0, 0.08f, 0);
            bodyTransform.localScale = new Vector3(0.30f, 0.16f, 0.22f);
        }
        MoveHeadGroup(new Vector3(0, 0.16f, -0.10f));

        if (tailTransform != null)
        {
            tailTransform.localPosition = new Vector3(0.18f, 0.06f, 0.04f);
            tailTransform.localRotation = Quaternion.Euler(0, 0, -15f);
        }

        if (scarfTransform != null)
            scarfTransform.localPosition = new Vector3(0, 0.14f, 0f);

        float breath = Mathf.Sin(Time.time * 1.2f) * 0.015f;
        if (bodyTransform != null)
            bodyTransform.localScale += new Vector3(0, breath, 0);
    }

    private void UpdateWaving()
    {
        // 开心摇摆（无手臂，用身体晃动替代）
        float sway = Mathf.Sin(Time.time * 6f) * 8f;
        if (bodyTransform != null)
            bodyTransform.localRotation = Quaternion.Euler(0, 0, sway);

        if (headTransform != null)
            headTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 4f) * 5f);

        if (tailTransform != null)
            tailTransform.localRotation = Quaternion.Euler(0, 0, -50f + Mathf.Sin(Time.time * 6f) * 25f);

        if (leftEarTransform != null)
            leftEarTransform.localRotation = Quaternion.Euler(0, 0, 45f + Mathf.Sin(Time.time * 6f) * 8f);
        if (rightEarTransform != null)
            rightEarTransform.localRotation = Quaternion.Euler(0, 0, -45f - Mathf.Sin(Time.time * 6f) * 8f);

        if (stateTimer > 2f)
        {
            ResetParts();
            ResetPositions();
            ChangeState(CharacterState.Idle);
        }
    }

    private void UpdateDancing()
    {
        float beat = Mathf.Sin(Time.time * 6f);
        float beat2 = Mathf.Cos(Time.time * 6f);

        if (bodyTransform != null)
        {
            bodyTransform.localPosition = BodyPos + new Vector3(0, Mathf.Abs(beat) * 0.08f, 0);
            bodyTransform.localRotation = Quaternion.Euler(0, beat * 10f, beat2 * 5f);
        }
        float danceBob = Mathf.Abs(beat) * 0.08f;
        MoveHeadGroup(HeadPos + new Vector3(0, danceBob, 0));
        if (headTransform != null)
            headTransform.localRotation = Quaternion.Euler(0, beat * 6f, beat2 * 4f);

        if (tailTransform != null)
            tailTransform.localRotation = Quaternion.Euler(0, 0, -50f + beat * 35f);

        if (leftEarTransform != null)
            leftEarTransform.localRotation = Quaternion.Euler(0, 0, 45f + beat * 6f);
        if (rightEarTransform != null)
            rightEarTransform.localRotation = Quaternion.Euler(0, 0, -45f - beat * 6f);

        if (scarfTransform != null)
            scarfTransform.localRotation = Quaternion.Euler(90, 0, beat2 * 5f);

        if (stateTimer > 3f)
        {
            ResetParts();
            ResetPositions();
            ChangeState(CharacterState.Idle);
        }
    }

    #endregion

    #region Public Methods

    public void WalkTo(Vector3 target)
    {
        targetPosition = target;
        targetPosition.y = transform.position.y;
        targetPosition.z = transform.position.z;
        ChangeState(CharacterState.Walking);
    }

    public CharacterState GetCurrentState() => currentState;

    public void Speak()
    {
        if (currentState == CharacterState.Jumping) return;
        ChangeState(CharacterState.Speaking);
    }

    public void StopSpeaking()
    {
        if (currentState == CharacterState.Speaking)
        {
            ResetParts();
            ResetPositions();
            ChangeState(CharacterState.Idle);
        }
    }

    public void ReturnToIdle()
    {
        ResetParts();
        ResetPositions();
        ChangeState(CharacterState.Idle);
    }

    public void Sit()     => ChangeState(CharacterState.Sitting);
    public void LieDown() => ChangeState(CharacterState.LyingDown);
    public void Wave()    => ChangeState(CharacterState.Waving);
    public void Dance()   => ChangeState(CharacterState.Dancing);

    #endregion

    #region Internal

    private void ChangeState(CharacterState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        stateTimer = 0f;
    }

    /// <summary>
    /// 移动头部及所有面部特征（眼睛/耳朵）保持相对位置
    /// </summary>
    private void MoveHeadGroup(Vector3 newHeadPos)
    {
        Vector3 delta = newHeadPos - HeadPos;
        if (headTransform != null)
        {
            headTransform.localPosition = newHeadPos;
            headTransform.localScale = HeadScale;
        }
        if (leftEyeTransform != null) leftEyeTransform.localPosition = LEyePos + delta;
        if (rightEyeTransform != null) rightEyeTransform.localPosition = REyePos + delta;
        if (leftEarTransform != null) leftEarTransform.localPosition = LEarPos + delta;
        if (rightEarTransform != null) rightEarTransform.localPosition = REarPos + delta;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        var scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void ResetParts()
    {
        if (headTransform != null) headTransform.localRotation = Quaternion.identity;
        if (leftEarTransform != null) leftEarTransform.localRotation = Quaternion.Euler(0, 0, 45f);
        if (rightEarTransform != null) rightEarTransform.localRotation = Quaternion.Euler(0, 0, -45f);
        if (tailTransform != null) tailTransform.localRotation = Quaternion.Euler(0, 0, -50f);
        if (bodyTransform != null) bodyTransform.localRotation = Quaternion.identity;
        if (scarfTransform != null) scarfTransform.localRotation = Quaternion.Euler(90, 0, 0);
        if (leftEyeTransform != null) leftEyeTransform.localScale = LEyeScale;
        if (rightEyeTransform != null) rightEyeTransform.localScale = LEyeScale;
    }

    public void ResetPositions()
    {
        if (bodyTransform != null) { bodyTransform.localPosition = BodyPos; bodyTransform.localScale = BodyScale; }
        if (headTransform != null) { headTransform.localPosition = HeadPos; headTransform.localScale = HeadScale; }
        if (leftEarTransform != null) leftEarTransform.localPosition = LEarPos;
        if (rightEarTransform != null) rightEarTransform.localPosition = REarPos;
        if (leftEyeTransform != null) leftEyeTransform.localPosition = LEyePos;
        if (rightEyeTransform != null) rightEyeTransform.localPosition = REyePos;
        if (noseTransform != null) noseTransform.localPosition = NosePos;
        if (scarfTransform != null) scarfTransform.localPosition = ScarfPos;
        if (tailTransform != null) tailTransform.localPosition = TailPos;
    }

    #endregion
}
