using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Viture.XR.Samples.StarterAssets
{
    /// <summary>
    /// Quick Actions UI that appears when user looks up, providing system-level controls
    /// with hand tracking interaction and visual feedback.
    /// </summary>
    public class VitureQuickActions : MonoBehaviour
    {
        #region Inspector Configuration

        [Header("Activation Settings")]
        [SerializeField] private GameObject m_QuickActionsPanel;
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField, Range(10f, 40f)] private float m_LookUpAngleThreshold = 18f;
        [SerializeField, Range(10f, 40f)] private float m_LookDownAngleThreshold = 15f;
        [SerializeField, Range(0.1f, 2f)] private float m_ActivationDelay = 0.3f;
        [SerializeField, Range(0.1f, 2f)] private float m_DeactivationDelay = 0.3f;
        [SerializeField, Range(0.1f, 2f)] private float m_FadeInDuration = 0.4f;
        [SerializeField, Range(0.1f, 2f)] private float m_FadeOutDuration = 0.4f;
        [SerializeField, Range(0.3f, 2f)] private float m_UIDistanceFromCamera = 0.55f;

        [Header("Button Configuration")]
        [SerializeField] private List<Transform> m_ButtonTransforms;
        [SerializeField] private Transform m_DebugHandTransform;

        [Header("Interaction Distances")]
        [SerializeField, Range(0.1f, 0.5f)] private float m_HoverDistance = 0.3f;
        [SerializeField, Range(0.05f, 0.3f)] private float m_ScaleDistance = 0.15f;
        [SerializeField, Range(0.01f, 0.05f)] private float m_TouchDistance = 0.035f;
        [SerializeField, Range(0f, 1f)] private float m_DirectionThreshold = 0.85f;

        [Header("Animation Settings")]
        [SerializeField, Range(0f, -0.2f)] private float m_HoverZPosition = -0.1f;
        [SerializeField, Range(1f, 20f)] private float m_HoverLerpSpeed = 8f;
        [SerializeField, Range(1f, 2f)] private float m_MaxHoverScale = 1.2f;
        [SerializeField, Range(1f, 20f)] private float m_ScaleLerpSpeed = 10f;
        [SerializeField, Range(5f, 25f)] private float m_ColorLerpSpeed = 16f;
        [SerializeField, Range(1.2f, 2f)] private float m_MaxTouchScale = 1.4f;
        [SerializeField, Range(0.1f, 0.5f)] private float m_TouchAnimationDuration = 0.3f;
        [SerializeField, Range(5f, 25f)] private float m_LabelFadeSpeed = 16f;
        [SerializeField, Range(0.3f, 2f)] private float m_RecordingDisplayDuration = 0.8f;

        [Header("Visual Assets")]
        [SerializeField] private Material m_ButtonMaterial;
        [SerializeField] private ButtonTextures m_HomeButton;
        [SerializeField] private ButtonTextures m_StartRecordButton;
        [SerializeField] private ButtonTextures m_StopRecordButton;
        [SerializeField] private CountdownTextures m_Countdown;
        [SerializeField] private ButtonLabels m_Labels;

        [Header("Double Click Recenter")]
        [SerializeField] private bool m_EnableDoubleClickRecenter = true;
        [SerializeField, Range(0.1f, 1f)] private float m_DoubleClickTimeWindow = 0.4f;

        #endregion

        #region Runtime State

        // Core Components
        private Camera m_MainCamera;
        private XRHandSubsystem m_HandSubsystem;

        // State Management
        private bool m_IsActive;
        private bool m_IsAnimating;
        private bool m_IsLookingUp;
        private int m_HoveredButtonIndex = -1;
        private bool m_RequiresLookDownReset;

        // Timing
        private float m_LastLookUpTime;
        private float m_LastLookDownTime;

        // Hand Data
        private bool m_HasValidHandPosition;
        private Vector3 m_CurrentHandPosition;

        // Button Data
        private List<Vector3> m_OriginalButtonPositions;
        private float[] m_ButtonToHandDistances;
        private Material[] m_ButtonMaterialInstances;
        private Image[] m_ButtonImages;

        // Label Animation
        private CanvasRenderer[] m_LabelRenderers;

        // Double Click Recenter
        private float m_LastClickTime;
        private int m_ClickCount;

        // Constants
        private float m_CanvasScaleFactor = 1000f;
        private static readonly int s_NormalTexProperty = Shader.PropertyToID("_NormalTex");
        private static readonly int s_HoverTexProperty = Shader.PropertyToID("_HoverTex");
        private static readonly int s_LerpProperty = Shader.PropertyToID("_Lerp");

        #endregion

        #region Data Structures

        [Serializable]
        private struct ButtonTextures
        {
            public Texture NormalTexture;
            public Texture HoverTexture;
        }

        [Serializable]
        private struct CountdownTextures
        {
            public Texture Three;
            public Texture Two;
            public Texture One;
        }

        [Serializable]
        private struct ButtonLabels
        {
            public Image[] LabelImages;
            public Sprite HomeText;
            public Sprite StartRecordingText;
            public Sprite StopRecordingText;
        }

        private enum ButtonType
        {
            Record = 0,
            Home = 1
        }

        #endregion

        private void Start()
        {
            InitializeUI();
            InitializeButtonSystem();

#if !UNITY_EDITOR
            if (m_DebugHandTransform != null)
                m_DebugHandTransform.gameObject.SetActive(false);
#endif
        }

        private void OnDestroy()
        {
            if (m_ButtonMaterialInstances != null)
            {
                foreach (var material in m_ButtonMaterialInstances)
                {
                    if (material != null)
                        DestroyImmediate(material);
                }
            }
        }

        private void InitializeUI()
        {
            m_QuickActionsPanel.SetActive(false);
            m_CanvasGroup.alpha = 0f;
            m_CanvasScaleFactor = 1f / m_CanvasGroup.transform.localScale.x;
        }

        private void InitializeButtonSystem()
        {
            CreateButtonMaterials();
            SetupButtonLabels();
            m_OriginalButtonPositions = new List<Vector3>();
            m_ButtonToHandDistances = new float[m_ButtonTransforms.Count];
        }

        private void CreateButtonMaterials()
        {
            m_ButtonImages = new Image[m_ButtonTransforms.Count];
            m_ButtonMaterialInstances = new Material[m_ButtonTransforms.Count];

            for (int i = 0; i < m_ButtonTransforms.Count; i++)
            {
                if (m_ButtonTransforms[i] != null)
                {
                    m_ButtonImages[i] = m_ButtonTransforms[i].GetComponent<Image>();
                    m_ButtonImages[i].sprite = null;

                    Material material = new Material(m_ButtonMaterial);
                    m_ButtonMaterialInstances[i] = material;
                    m_ButtonImages[i].material = material;

                    switch ((ButtonType)i)
                    {
                        case ButtonType.Record:
                            material.SetTexture(s_NormalTexProperty, m_StartRecordButton.NormalTexture);
                            material.SetTexture(s_HoverTexProperty, m_StartRecordButton.HoverTexture);
                            break;
                        case ButtonType.Home:
                            material.SetTexture(s_NormalTexProperty, m_HomeButton.NormalTexture);
                            material.SetTexture(s_HoverTexProperty, m_HomeButton.HoverTexture);
                            break;
                    }

                    m_ButtonMaterialInstances[i].SetFloat(s_LerpProperty, 0f);
                }
            }
        }

        private void SetupButtonLabels()
        {
            if (m_Labels.LabelImages == null)
                return;

            m_LabelRenderers = new CanvasRenderer[m_Labels.LabelImages.Length];

            for (int i = 0; i < m_Labels.LabelImages.Length; i++)
            {
                if (m_Labels.LabelImages[i] != null)
                {
                    m_LabelRenderers[i] = m_Labels.LabelImages[i].GetComponent<CanvasRenderer>();

                    switch ((ButtonType)i)
                    {
                        case ButtonType.Record:
                            m_Labels.LabelImages[i].sprite = VitureXR.Capture.isRecording ?
                                m_Labels.StopRecordingText : m_Labels.StartRecordingText;
                            break;
                        case ButtonType.Home:
                            m_Labels.LabelImages[i].sprite = m_Labels.HomeText;
                            break;
                    }
                }
            }
        }

        private void Update()
        {
            EnsureCoreReferences();
            UpdateLookUpDetection();
            UpdateUIAppearance();

            if (m_IsActive)
            {
                UpdateHandPosition();
                UpdateButtonInteraction();
                AnimateAllButtons();
            }

            if (m_EnableDoubleClickRecenter)
                CheckDoubleClickRecenter();
        }

        private void EnsureCoreReferences()
        {
            if (m_MainCamera == null)
                m_MainCamera = Camera.main;
        }

        private void UpdateLookUpDetection()
        {
            if (m_MainCamera == null)
                return;

            Vector3 cameraForward = m_MainCamera.transform.forward;
            float pitchAngle = Mathf.Asin(cameraForward.y) * Mathf.Rad2Deg;
            bool currentlyLookingUp;
            if (m_IsLookingUp)
            {
                // Currently looking up - need to go below look-down threshold to stop
                currentlyLookingUp = pitchAngle > m_LookDownAngleThreshold;
            }
            else
            {
                // Currently looking up - need to go above look-up threshold to start
                currentlyLookingUp = pitchAngle > m_LookUpAngleThreshold;
            }

            // Handle look up/down state transitions
            if (currentlyLookingUp && !m_IsLookingUp)
            {
                m_IsLookingUp = true;
                m_LastLookUpTime = Time.time;
            }
            else if (!currentlyLookingUp && m_IsLookingUp)
            {
                m_IsLookingUp = false;
                m_LastLookDownTime = Time.time;

                if (m_RequiresLookDownReset)
                    m_RequiresLookDownReset = false;
            }
        }

        private void UpdateUIAppearance()
        {
            if (m_IsAnimating)
                return;

            if (!m_IsActive && m_IsLookingUp && !m_RequiresLookDownReset && Time.time - m_LastLookUpTime > m_ActivationDelay)
            {
                StartCoroutine(ActivateUI());
            }
            else if (m_IsActive && !m_IsLookingUp && Time.time - m_LastLookDownTime > m_DeactivationDelay)
            {
                StartCoroutine(DeactivateUI());
            }
        }

        private IEnumerator ActivateUI()
        {
            m_IsAnimating = true;
            m_QuickActionsPanel.SetActive(true);

            ResetUIPosition();
            ResetOriginalButtonPositions();
            UpdateRecordingButtonState();
            ResetLabelAlphas();

            yield return StartCoroutine(FadeUI(0f, 1f, m_FadeInDuration));

            m_IsActive = true;
            m_IsAnimating = false;
        }

        private void ResetUIPosition()
        {
            if (m_QuickActionsPanel == null || m_MainCamera == null)
                return;

            Vector3 cameraPosition = m_MainCamera.transform.position;
            transform.position = cameraPosition + m_MainCamera.transform.forward * m_UIDistanceFromCamera;

            Vector3 directionToCamera = (cameraPosition - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }

        private void ResetOriginalButtonPositions()
        {
            m_OriginalButtonPositions.Clear();
            foreach (var button in m_ButtonTransforms)
            {
                m_OriginalButtonPositions.Add(button != null ? button.position : Vector3.zero);
            }
        }

        private void UpdateRecordingButtonState()
        {
            int recordIndex = (int)ButtonType.Record;

            if (m_ButtonMaterialInstances?[recordIndex] == null)
                return;

            Material material = m_ButtonMaterialInstances[recordIndex];
            bool isRecording = VitureXR.Capture.isRecording;

            var textures = isRecording ?
                (m_StopRecordButton.NormalTexture, m_StopRecordButton.HoverTexture) :
                (m_StartRecordButton.NormalTexture, m_StartRecordButton.HoverTexture);

            material.SetTexture(s_NormalTexProperty, textures.Item1);
            material.SetTexture(s_HoverTexProperty, textures.Item2);

            if (m_Labels.LabelImages?[recordIndex] != null)
            {
                m_Labels.LabelImages[recordIndex].sprite =
                    isRecording ? m_Labels.StopRecordingText : m_Labels.StartRecordingText;
            }
        }

        private void ResetLabelAlphas()
        {
            if (m_LabelRenderers == null)
                return;

            foreach (var label in m_LabelRenderers)
                label.SetAlpha(1f);
        }

        private IEnumerator FadeUI(float fromAlpha, float toAlpha, float duration)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsedTime / duration);
                m_CanvasGroup.alpha = alpha;
                yield return null;
            }

            m_CanvasGroup.alpha = toAlpha;
        }

        private IEnumerator DeactivateUI()
        {
            m_IsAnimating = true;
            m_IsActive = false;

            yield return StartCoroutine(FadeUI(1f, 0f, m_FadeOutDuration));

            m_QuickActionsPanel.SetActive(false);
            ResetAllButtonStates();
            m_IsAnimating = false;
        }

        private void ResetAllButtonStates()
        {
            m_HoveredButtonIndex = -1;

            for (int i = 0; i < m_ButtonTransforms.Count; i++)
            {
                // Reset button transform
                if (m_ButtonTransforms[i] != null)
                {
                    Transform buttonTransform = m_ButtonTransforms[i];
                    Vector3 localPos = buttonTransform.localPosition;
                    buttonTransform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
                    buttonTransform.localScale = Vector3.one;
                }

                // Reset button material
                m_ButtonMaterialInstances?[i]?.SetFloat(s_LerpProperty, 0f);
            }

            m_HasValidHandPosition = false;
        }

        private void UpdateHandPosition()
        {
#if UNITY_EDITOR
            if (m_DebugHandTransform != null)
            {
                m_CurrentHandPosition = m_DebugHandTransform.position;
                m_HasValidHandPosition = true;
                return;
            }
#endif

            m_HasValidHandPosition = false;

            if (m_HandSubsystem == null)
            {
                m_HandSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?
                    .GetLoadedSubsystem<XRHandSubsystem>();
            }

            if (m_HandSubsystem == null)
                return;

            bool leftValid = TryGetHandPosition(m_HandSubsystem.leftHand, out Vector3 leftPos);
            bool rightValid = TryGetHandPosition(m_HandSubsystem.rightHand, out Vector3 rightPos);

            if (leftValid && rightValid)
            {
                float leftDist = Vector3.Distance(leftPos, transform.position);
                float rightDist = Vector3.Distance(rightPos, transform.position);
                m_CurrentHandPosition = leftDist < rightDist ? leftPos : rightPos;
                m_HasValidHandPosition = true;
            }
            else if (leftValid)
            {
                m_CurrentHandPosition = leftPos;
                m_HasValidHandPosition = true;
            }
            else if (rightValid)
            {
                m_CurrentHandPosition = rightPos;
                m_HasValidHandPosition = true;
            }
        }

        private bool TryGetHandPosition(XRHand hand, out Vector3 position)
        {
            position = Vector3.zero;

            if (!hand.isTracked)
                return false;

            var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
            if (indexTip.TryGetPose(out Pose pose))
            {
                position = pose.position;
                return true;
            }

            return false;
        }

        private void UpdateButtonInteraction()
        {
            if (!m_HasValidHandPosition)
                return;

            int newHoveredIndex = FindHoveredButton();

            if (newHoveredIndex != -1 && newHoveredIndex == m_HoveredButtonIndex &&
                m_ButtonToHandDistances[newHoveredIndex] < m_TouchDistance)
            {
                StartCoroutine(OnButtonTouched(newHoveredIndex));
                return;
            }

            m_HoveredButtonIndex = newHoveredIndex;
        }

        private int FindHoveredButton()
        {
            int closestIndex = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < m_ButtonTransforms.Count; i++)
            {
                Vector3 buttonOriginalPos = m_OriginalButtonPositions[i];
                float distance = Vector3.Distance(m_CurrentHandPosition, buttonOriginalPos);
                m_ButtonToHandDistances[i] = Vector3.Distance(m_CurrentHandPosition, m_ButtonTransforms[i].position);

                if (distance < m_HoverDistance && IsHandInFrontOfButton(i, buttonOriginalPos))
                {
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIndex = i;
                    }
                }
            }

            return closestIndex;
        }

        private bool IsHandInFrontOfButton(int buttonIndex, Vector3 buttonPos)
        {
            Vector3 buttonForward = -m_ButtonTransforms[buttonIndex].forward;
            Vector3 buttonToHand = (m_CurrentHandPosition - buttonPos).normalized;

            return Vector3.Dot(buttonForward, buttonToHand) > m_DirectionThreshold;
        }

        private void AnimateAllButtons()
        {
            for (int i = 0; i < m_ButtonTransforms.Count; i++)
                AnimateButton(i);
        }

        private void AnimateButton(int buttonIndex)
        {
            if (buttonIndex >= m_ButtonTransforms.Count || m_ButtonTransforms[buttonIndex] == null)
                return;

            Transform buttonTransform = m_ButtonTransforms[buttonIndex];
            bool isHovered = buttonIndex == m_HoveredButtonIndex;

            // Animate button position
            Vector3 localPos = buttonTransform.localPosition;
            float targetZ = isHovered ? m_HoverZPosition * m_CanvasScaleFactor : 0f;
            Vector3 targetPos = new Vector3(localPos.x, localPos.y, targetZ);
            buttonTransform.localPosition = Vector3.Lerp(buttonTransform.localPosition, targetPos, m_HoverLerpSpeed * Time.deltaTime);

            // Animate label alpha
            if (m_LabelRenderers != null && buttonIndex < m_LabelRenderers.Length)
            {
                var labelRenderer = m_LabelRenderers[buttonIndex];
                if (labelRenderer != null)
                {
                    float labelTargetAlpha = isHovered ? 0f : 1f;
                    float targetAlpha = Mathf.Lerp(labelRenderer.GetAlpha(), labelTargetAlpha,
                        m_LabelFadeSpeed * Time.deltaTime);
                    labelRenderer.SetAlpha(targetAlpha);
                }
            }

            // Animate button scale
            float targetScale = 1f;
            float targetColorLerp = 0f;
            if (isHovered && buttonIndex < m_ButtonToHandDistances.Length)
            {
                float distance = m_ButtonToHandDistances[buttonIndex];
                if (distance < m_ScaleDistance)
                {
                    float normalizedDistance = Mathf.Clamp01(distance / m_ScaleDistance);
                    targetScale = Mathf.Lerp(m_MaxHoverScale, 1f, normalizedDistance);
                    targetColorLerp = Mathf.Lerp(1f, 0f, normalizedDistance);
                }
            }
            float currentScale = buttonTransform.localScale.x;
            float newScale = Mathf.Lerp(currentScale, targetScale, m_ScaleLerpSpeed * Time.deltaTime);
            buttonTransform.localScale = Vector3.one * newScale;

            // Animate button color
            if (m_ButtonMaterialInstances == null || buttonIndex >= m_ButtonMaterialInstances.Length)
                return;
            Material material = m_ButtonMaterialInstances[buttonIndex];
            if (material != null)
            {
                float finalLerp = Mathf.Lerp(material.GetFloat(s_LerpProperty), targetColorLerp, m_ColorLerpSpeed * Time.deltaTime);
                material.SetFloat(s_LerpProperty, finalLerp);
            }
        }

        private IEnumerator OnButtonTouched(int buttonIndex)
        {
            m_IsActive = false;
            m_RequiresLookDownReset = true;

            yield return StartCoroutine(PlayTouchAnimation(buttonIndex));

            bool wasRecording = VitureXR.Capture.isRecording;

            switch ((ButtonType)buttonIndex)
            {
                case ButtonType.Record:
                    StartCoroutine(VitureXR.Capture.isRecording ? HandleStopRecording() : HandleStartRecording());
                    break;
                case ButtonType.Home:
                    Application.Quit();
                    yield break;
            }

            if (buttonIndex == (int)ButtonType.Record)
            {
                float waitTime = wasRecording ? m_RecordingDisplayDuration : 3f + m_RecordingDisplayDuration;
                yield return new WaitForSeconds(waitTime);
            }

            yield return StartCoroutine(DeactivateUI());
        }

        private IEnumerator PlayTouchAnimation(int buttonIndex)
        {
            Transform buttonTransform = m_ButtonTransforms[buttonIndex];
            Vector3 originalScale = buttonTransform.localScale;

            float elapsedTime = 0f;
            while (elapsedTime < m_TouchAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / m_TouchAnimationDuration;

                float targetScale = progress < 0.4f
                    ? Mathf.Lerp(originalScale.x, m_MaxTouchScale, progress / 0.4f)
                    : Mathf.Lerp(m_MaxTouchScale, 1f, (progress - 0.4f) / 0.6f);

                buttonTransform.localScale = Vector3.one * targetScale;
                yield return null;
            }

            buttonTransform.localScale = Vector3.one;
        }

        private IEnumerator HandleStartRecording()
        {
            Material recordMaterial = m_ButtonMaterialInstances[(int)ButtonType.Record];
            if (recordMaterial == null)
                yield break;

            HideRecordLabel();

            recordMaterial.SetTexture(s_NormalTexProperty, m_Countdown.Three);
            recordMaterial.SetFloat(s_LerpProperty, 0f);
            yield return new WaitForSeconds(1f);

            recordMaterial.SetTexture(s_NormalTexProperty, m_Countdown.Two);
            yield return new WaitForSeconds(1f);

            recordMaterial.SetTexture(s_NormalTexProperty, m_Countdown.One);
            yield return new WaitForSeconds(1f);

            VitureXR.Capture.StartRecording();
            UpdateRecordingButtonState();

            yield return new WaitForSeconds(m_RecordingDisplayDuration);
        }

        private void HideRecordLabel()
        {
            int recordIndex = (int)ButtonType.Record;
            if (m_LabelRenderers != null && recordIndex < m_LabelRenderers.Length)
            {
                m_LabelRenderers[recordIndex]?.SetAlpha(0f);
            }
        }

        private IEnumerator HandleStopRecording()
        {
            VitureXR.Capture.StopRecording();
            UpdateRecordingButtonState();
            yield return new WaitForSeconds(m_RecordingDisplayDuration);
        }

        private void CheckDoubleClickRecenter()
        {
            if (Keyboard.current.f4Key.wasPressedThisFrame)
            {
                if (m_ClickCount == 0)
                {
                    m_ClickCount = 1;
                    m_LastClickTime = Time.time;
                }
                else if (m_ClickCount == 1 && Time.time - m_LastClickTime < m_DoubleClickTimeWindow)
                {
                    m_ClickCount = 0;
                    VitureXR.HeadTracking.ResetOrigin();
                }
            }

            if (m_ClickCount > 0 && Time.time - m_LastClickTime > m_DoubleClickTimeWindow)
                m_ClickCount = 0;
        }
    }
}