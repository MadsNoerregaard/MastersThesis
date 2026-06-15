using UnityEngine;

public class SecondaryBehaviourManager : MonoBehaviour
{
    [Header("Tracked Hands")]
    public Transform trackedLeftHand;
    public Transform trackedRightHand;

    [Header("Normal Visual Hands")]
    public GameObject normalLeftHandVisual;
    public GameObject normalRightHandVisual;

    [Header("Assist Visual Hands")]
    public Transform assistLeftHandVisualRoot;
    public Transform assistRightHandVisualRoot;

    [Header("Assist Hand Renderers")]
    public SkinnedMeshRenderer[] assistHandRenderers;
    public OVRHand leftOVRHand;
    public OVRHand rightOVRHand;

    [Header("Target")]
    public AutonomyHandAssistTarget currentAssistTarget;

    [Header("Autonomy Mode")]
    public AutonomyBehaviourType leftHandBehaviour = AutonomyBehaviourType.None;
    public AutonomyBehaviourType rightHandBehaviour = AutonomyBehaviourType.None;

    [Header("Autonomous Press")]
    public float snapOnlyPressDistance = 0.4f;
    public bool allowSnapOnlyPress = true;
    public float pressDelay = 0.5f;
    public float snapPressDelay = 0.2f;
    public float snapOnlyHorizontalRadius = 0.5f;
    public float snapOnlyMinHeight = 0f;
    public float snapOnlyMaxHeight = 0.4f;

    [Header("Motion")]
    public float snapBlendSpeed = 12f;
    public float releaseDistanceMultiplier = 1.35f;
    public float autoPressDistance = 0.015f;
    public float handFadeDuration = 0.1f;

    [Header("Button Press Timer")]
    public float buttonAutoPressDelay = 0.2f;

    [Header("Hand Fade Material")]
    public Material assistFadeMaterial;

    private bool _leftAssistActive;
    private bool _rightAssistActive;
    private bool _leftAssistWasVisible;
    private bool _rightAssistWasVisible;
    private float _leftFade;
    private float _rightFade;
    private float _leftAutoPressTimer;
    private float _rightAutoPressTimer;

    private void Start()
    {
        DisableAllAssist();
    }

    private void LateUpdate()
    {
        if (currentAssistTarget == null)
        {
            DisableAllAssist();
            return;
        }

        if (currentAssistTarget.leftAssistAnchor == null || currentAssistTarget.rightAssistAnchor == null)
        {
            Debug.LogWarning("Assist anchors not set on target: " + currentAssistTarget.name);
            DisableAllAssist();
            return;
        }

        UpdateHandAssist(
            true,
            leftHandBehaviour,
            trackedLeftHand,
            assistLeftHandVisualRoot,
            currentAssistTarget.leftAssistAnchor,
            currentAssistTarget.activationRadius,
            ref _leftAssistActive
        );

        UpdateHandAssist(
            false,
            rightHandBehaviour,
            trackedRightHand,
            assistRightHandVisualRoot,
            currentAssistTarget.rightAssistAnchor,
            currentAssistTarget.activationRadius,
            ref _rightAssistActive
        );

        HandleSnapOnlyPress(true);
        HandleSnapOnlyPress(false);
    }

    private void UpdateHandAssist(
    bool isLeft,
    AutonomyBehaviourType behaviour,
    Transform trackedHand,
    Transform assistRoot,
    Transform assistAnchor,
    float activationRadius,
    ref bool assistActive)
    {
        if (trackedHand == null || assistRoot == null || assistAnchor == null || currentAssistTarget == null)
            return;

        if (behaviour == AutonomyBehaviourType.None)
        {
            SetAssistHandVisible(isLeft, false);
            SetNormalHandVisible(isLeft, true);
            ResetHandTimers(isLeft);
            assistActive = false;
            return;
        }

        Transform detectionTarget = currentAssistTarget.pointTarget != null
            ? currentAssistTarget.pointTarget
            : assistAnchor;

        float trackedDistance = Vector3.Distance(trackedHand.position, detectionTarget.position);

        float buttonDelay = behaviour == AutonomyBehaviourType.Press ? pressDelay : snapPressDelay;

        if (!assistActive && trackedDistance <= activationRadius)
            assistActive = true;

        if (assistActive && behaviour != AutonomyBehaviourType.Snap && trackedDistance > activationRadius * releaseDistanceMultiplier)
            assistActive = false;

        if (!assistActive)
        {
            SetAssistHandVisible(isLeft, false);
            SetNormalHandVisible(isLeft, true);
            ResetHandTimers(isLeft);
            return;
        }

        bool snap = ShouldSnap(behaviour);
        bool press = ShouldPress(behaviour);
        bool showAssistHand = true; // all modes except None reach this point

        Vector3 targetPos = currentAssistTarget.GetAssistPosition(isLeft);
        Quaternion targetRot = currentAssistTarget.GetAssistRotation(isLeft);

        if (showAssistHand)
        {
            bool wasVisible = isLeft ? _leftAssistWasVisible : _rightAssistWasVisible;

            if (!wasVisible)
            {
                assistRoot.position = targetPos;
                assistRoot.rotation = targetRot;

                SetAssistHandVisible(isLeft, true);

                if (isLeft)
                {
                    _leftFade = 0f;
                    _leftAssistWasVisible = true;
                }
                else
                {
                    _rightFade = 0f;
                    _rightAssistWasVisible = true;
                }
            }

            SetAssistHandVisible(isLeft, true);
        }

        if (snap)
        {
            assistRoot.position = targetPos;
            assistRoot.rotation = targetRot;

            // Snap modes hide the normal hand.
            SetNormalHandVisible(isLeft, false);
        }
        else
        {
            SetAssistHandVisible(isLeft, false);
            SetNormalHandVisible(isLeft, true);
        }

        if (press)
        {
            float timer = isLeft ? _leftAutoPressTimer : _rightAutoPressTimer;
            timer += Time.deltaTime;

            if (timer >= buttonDelay)
                currentAssistTarget.TriggerAutoPress();

            if (isLeft) _leftAutoPressTimer = timer;
            else _rightAutoPressTimer = timer;
        }
        else
        {
            if (isLeft) _leftAutoPressTimer = 0f;
            else _rightAutoPressTimer = 0f;
        }

        float fade = isLeft ? _leftFade : _rightFade;
        fade += Time.deltaTime / Mathf.Max(handFadeDuration, 0.001f);
        fade = Mathf.Clamp01(fade);

        SetAssistAlpha(isLeft, fade);

        if (isLeft) _leftFade = fade;
        else _rightFade = fade;
    }

        public void ApplyAssistHandMaterial(Material possessedMaterial)
        {
            if (possessedMaterial == null || assistFadeMaterial == null || assistHandRenderers == null)
                return;

            Color sourceColor = Color.white;

            if (possessedMaterial.HasProperty("_BaseColor"))
                sourceColor = possessedMaterial.GetColor("_BaseColor");
            else if (possessedMaterial.HasProperty("_Color"))
                sourceColor = possessedMaterial.GetColor("_Color");

            foreach (var r in assistHandRenderers)
            {
                if (!r) continue;

                Material instance = new Material(assistFadeMaterial);

                if (instance.HasProperty("_BaseColor"))
                {
                    Color c = sourceColor;
                    c.a = 0f;
                    instance.SetColor("_BaseColor", c);
                }

                if (instance.HasProperty("_Color"))
                {
                    Color c = sourceColor;
                    c.a = 0f;
                    instance.SetColor("_Color", c);
                }

                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = instance;

                r.materials = mats;
            }
        }

    public void DisableAllAssist()
    {
        _leftAssistActive = false;
        _rightAssistActive = false;
        _leftAssistWasVisible = false;
        _rightAssistWasVisible = false;

        _leftAutoPressTimer = 0f;
        _rightAutoPressTimer = 0f;
        _leftFade = 0f;
        _rightFade = 0f;

        SetAssistHandVisible(true, false);
        SetAssistHandVisible(false, false);

        SetNormalHandVisible(true, true);
        SetNormalHandVisible(false, true);
    }

    private void SetNormalHandVisible(bool isLeft, bool visible)
    {
        GameObject go = isLeft ? normalLeftHandVisual : normalRightHandVisual;
        if (go != null)
            go.SetActive(visible);
    }

    private void SetAssistHandVisible(bool isLeft, bool visible)
    {
        Transform t = isLeft ? assistLeftHandVisualRoot : assistRightHandVisualRoot;
        if (t != null)
            t.gameObject.SetActive(visible);
    }

    void SetRendererAlpha(SkinnedMeshRenderer r, float alpha)
    {
        if (!r) return;

        foreach (var mat in r.materials)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }

            if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                c.a = alpha;
                mat.SetColor("_Color", c);
            }
        }
    }

    void SetAssistAlpha(bool isLeft, float alpha)
    {
        foreach (var r in assistHandRenderers)
        {
            if (!r) continue;

            // Optional: split left/right by name if needed
            SetRendererAlpha(r, alpha);
        }
    }

    private bool ShouldSnap(AutonomyBehaviourType mode)
    {
        return mode == AutonomyBehaviourType.Snap ||
            mode == AutonomyBehaviourType.SnapAndPress;
    }

    private bool ShouldPress(AutonomyBehaviourType mode)
    {
        return mode == AutonomyBehaviourType.Press ||
            mode == AutonomyBehaviourType.SnapAndPress;
    }

    private void ResetHandTimers(bool isLeft)
    {
        if (isLeft)
        {
            _leftAutoPressTimer = 0f;
            _leftFade = 0f;
            _leftAssistWasVisible = false;
            _leftAssistActive = false;
        }
        else
        {
            _rightAutoPressTimer = 0f;
            _rightFade = 0f;
            _rightAssistWasVisible = false;
            _rightAssistActive = false;
        }
    }

    public void ApplyAutonomyModes(SwapTarget target)
    {
        if (target == null)
        {
            leftHandBehaviour = AutonomyBehaviourType.None;
            rightHandBehaviour = AutonomyBehaviourType.None;
            return;
        }

        leftHandBehaviour = target.leftHandBehaviour;
        rightHandBehaviour = target.rightHandBehaviour;

        ResetHandTimers(true);
        ResetHandTimers(false);
    }

    private bool IsIndexGestureActive(bool isLeft)
    {
        OVRHand hand = isLeft ? leftOVRHand : rightOVRHand;

        if (hand == null)
            return false;

        return hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
    }

    private void HandleSnapOnlyPress(bool isLeft)
    {
        if (currentAssistTarget == null)
            return;
        
        AutonomyBehaviourType behaviour = isLeft ? leftHandBehaviour : rightHandBehaviour;

        if (behaviour != AutonomyBehaviourType.Snap)
            return;

        if (!allowSnapOnlyPress)
            return;

        bool assistVisible = isLeft ? _leftAssistWasVisible : _rightAssistWasVisible;

        if (!assistVisible)
            return;

        if (IsIndexGestureActive(isLeft))
        {
            currentAssistTarget.TriggerAutoPress();
        }
    }
}