using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;

public class PoseAvatarDriver : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PoseDetectionRunner poseDetectionRunner;

    [Header("IK Hand Targets")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("IK Foot Targets")]
    [SerializeField] private Transform leftFootTarget;
    [SerializeField] private Transform rightFootTarget;

    [Header("Avatar Root Wrapper")]
    [SerializeField] private Transform avatarRoot;

    [Header("Avatar Left Arm")]
    [SerializeField] private Transform avatarLeftShoulder;
    [SerializeField] private Transform avatarLeftElbow;
    [SerializeField] private Transform avatarLeftHandBone;

    [Header("Avatar Right Arm")]
    [SerializeField] private Transform avatarRightShoulder;
    [SerializeField] private Transform avatarRightElbow;
    [SerializeField] private Transform avatarRightHandBone;

    [Header("Avatar Left Leg")]
    [SerializeField] private Transform avatarLeftHip;
    [SerializeField] private Transform avatarLeftKnee;
    [SerializeField] private Transform avatarLeftFootBone;

    [Header("Avatar Right Leg")]
    [SerializeField] private Transform avatarRightHip;
    [SerializeField] private Transform avatarRightKnee;
    [SerializeField] private Transform avatarRightFootBone;

    [Header("Arm Tuning")]
    [SerializeField] private float xGain = 1.3f;
    [SerializeField] private float yGain = 1.25f;
    [SerializeField] private float zGain = 0.1f;

    [Header("Arm Base Bias")]
    [SerializeField] private float lateralBias = 0.25f;
    [SerializeField] private float verticalBias = -0.12f;

    [Header("Leg Tuning")]
    [SerializeField] private float legXGain = 1.0f;
    [SerializeField] private float legYGain = 1.0f;
    [SerializeField] private float legZGain = 0.03f;

    [Header("Root Follow")]
    [SerializeField] private bool enableRootFollow = true;
    [SerializeField] private float rootXGain = 3.0f;
    [SerializeField] private float rootYGain = 2.0f;
    [SerializeField] private float rootZGain = 0.0f;
    [SerializeField] private Vector3 rootOffset = Vector3.zero;

    [Header("Offsets")]
    [SerializeField] private Vector3 leftHandOffset = Vector3.zero;
    [SerializeField] private Vector3 rightHandOffset = Vector3.zero;
    [SerializeField] private Vector3 leftFootOffset = Vector3.zero;
    [SerializeField] private Vector3 rightFootOffset = Vector3.zero;

    [Header("General")]
    [SerializeField] private float smoothSpeed = 12f;
    [SerializeField] private bool mirrorX = true;
    [SerializeField] private bool swapLeftRight = false;

    [Header("Debug")]
    [SerializeField] private bool drawDebugLogs = false;

    private Vector3? calibratedBodyCenter = null;
    private Vector3 avatarRootStartPos;

    private const int LEFT_SHOULDER = 11;
    private const int RIGHT_SHOULDER = 12;
    private const int LEFT_ELBOW = 13;
    private const int RIGHT_ELBOW = 14;
    private const int LEFT_WRIST = 15;
    private const int RIGHT_WRIST = 16;

    private const int LEFT_HIP = 23;
    private const int RIGHT_HIP = 24;
    private const int LEFT_KNEE = 25;
    private const int RIGHT_KNEE = 26;
    private const int LEFT_ANKLE = 27;
    private const int RIGHT_ANKLE = 28;

    private void Start()
    {
        if (avatarRoot != null)
        {
            avatarRootStartPos = avatarRoot.position;
        }
    }

    public void ResetCalibration()
    {
        calibratedBodyCenter = null;

        if (avatarRoot != null)
        {
            avatarRootStartPos = avatarRoot.position;
        }

        if (drawDebugLogs)
        {
            Debug.Log("[PoseAvatarDriver] Calibration reset");
        }
    }

    private void Update()
    {
        if (poseDetectionRunner == null)
            return;

        if (!AvatarReferencesValid())
            return;

        if (!poseDetectionRunner.TryGetLatestPoseResult(out PoseLandmarkerResult result, out _))
            return;

        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            return;

        var landmarks = result.poseLandmarks[0].landmarks;
        if (landmarks == null || landmarks.Count <= RIGHT_ANKLE)
            return;

        int leftShoulderIndex = swapLeftRight ? RIGHT_SHOULDER : LEFT_SHOULDER;
        int leftElbowIndex = swapLeftRight ? RIGHT_ELBOW : LEFT_ELBOW;
        int leftWristIndex = swapLeftRight ? RIGHT_WRIST : LEFT_WRIST;

        int rightShoulderIndex = swapLeftRight ? LEFT_SHOULDER : RIGHT_SHOULDER;
        int rightElbowIndex = swapLeftRight ? LEFT_ELBOW : RIGHT_ELBOW;
        int rightWristIndex = swapLeftRight ? LEFT_WRIST : RIGHT_WRIST;

        int leftHipIndex = swapLeftRight ? RIGHT_HIP : LEFT_HIP;
        int leftKneeIndex = swapLeftRight ? RIGHT_KNEE : LEFT_KNEE;
        int leftAnkleIndex = swapLeftRight ? RIGHT_ANKLE : LEFT_ANKLE;

        int rightHipIndex = swapLeftRight ? LEFT_HIP : RIGHT_HIP;
        int rightKneeIndex = swapLeftRight ? LEFT_KNEE : RIGHT_KNEE;
        int rightAnkleIndex = swapLeftRight ? LEFT_ANKLE : RIGHT_ANKLE;

        Vector3 playerLeftShoulder = ToPlayerSpace(landmarks[leftShoulderIndex]);
        Vector3 playerLeftElbow = ToPlayerSpace(landmarks[leftElbowIndex]);
        Vector3 playerLeftWrist = ToPlayerSpace(landmarks[leftWristIndex]);

        Vector3 playerRightShoulder = ToPlayerSpace(landmarks[rightShoulderIndex]);
        Vector3 playerRightElbow = ToPlayerSpace(landmarks[rightElbowIndex]);
        Vector3 playerRightWrist = ToPlayerSpace(landmarks[rightWristIndex]);

        Vector3 playerLeftHip = ToPlayerSpace(landmarks[leftHipIndex]);
        Vector3 playerLeftKnee = ToPlayerSpace(landmarks[leftKneeIndex]);
        Vector3 playerLeftAnkle = ToPlayerSpace(landmarks[leftAnkleIndex]);

        Vector3 playerRightHip = ToPlayerSpace(landmarks[rightHipIndex]);
        Vector3 playerRightKnee = ToPlayerSpace(landmarks[rightKneeIndex]);
        Vector3 playerRightAnkle = ToPlayerSpace(landmarks[rightAnkleIndex]);

        if (enableRootFollow)
        {
            Vector3 playerShoulderMid = 0.5f * (playerLeftShoulder + playerRightShoulder);
            Vector3 playerHipMid = 0.5f * (playerLeftHip + playerRightHip);
            Vector3 playerBodyCenter = 0.5f * (playerShoulderMid + playerHipMid);

            UpdateAvatarRoot(playerBodyCenter);
        }

        UpdateLeftHand(playerLeftShoulder, playerLeftElbow, playerLeftWrist);
        UpdateRightHand(playerRightShoulder, playerRightElbow, playerRightWrist);

        UpdateLeftFoot(playerLeftHip, playerLeftKnee, playerLeftAnkle);
        UpdateRightFoot(playerRightHip, playerRightKnee, playerRightAnkle);
    }

    private void UpdateAvatarRoot(Vector3 playerBodyCenter)
    {
        if (avatarRoot == null)
            return;

        if (!calibratedBodyCenter.HasValue)
        {
            calibratedBodyCenter = playerBodyCenter;
            avatarRootStartPos = avatarRoot.position;

            if (drawDebugLogs)
            {
                Debug.Log("[PoseAvatarDriver] Body centre calibrated");
            }
        }

        Vector3 delta = playerBodyCenter - calibratedBodyCenter.Value;

        Vector3 mappedDelta = new Vector3(
            delta.x * rootXGain,
            delta.y * rootYGain,
            delta.z * rootZGain
        );

        Vector3 targetRootPos = avatarRootStartPos + mappedDelta + rootOffset;

        avatarRoot.position = Vector3.Lerp(
            avatarRoot.position,
            targetRootPos,
            Time.deltaTime * smoothSpeed
        );
    }

    private void UpdateLeftHand(Vector3 playerShoulder, Vector3 playerElbow, Vector3 playerWrist)
    {
        if (leftHandTarget == null)
            return;

        float playerArmLength = GetLimbLength(playerShoulder, playerElbow, playerWrist);
        if (playerArmLength < 0.0001f)
            return;

        float avatarArmLength = GetLimbLength(
            avatarLeftShoulder.position,
            avatarLeftElbow.position,
            avatarLeftHandBone.position
        );

        if (avatarArmLength < 0.0001f)
            return;

        Vector3 wristRelativeToShoulder = playerWrist - playerShoulder;
        Vector3 wristNormalized = wristRelativeToShoulder / playerArmLength;

        Vector3 avatarOffset = ConvertNormalizedArmOffset(wristNormalized, avatarArmLength);

        float outwardAmount = Mathf.Clamp01(Mathf.Abs(wristNormalized.x));
        Vector3 baseBias = new Vector3(-lateralBias * outwardAmount, verticalBias, 0f);

        Vector3 targetPos = avatarLeftShoulder.position + baseBias + avatarOffset + leftHandOffset;

        leftHandTarget.position = Vector3.Lerp(
            leftHandTarget.position,
            targetPos,
            Time.deltaTime * smoothSpeed
        );
    }

    private void UpdateRightHand(Vector3 playerShoulder, Vector3 playerElbow, Vector3 playerWrist)
    {
        if (rightHandTarget == null)
            return;

        float playerArmLength = GetLimbLength(playerShoulder, playerElbow, playerWrist);
        if (playerArmLength < 0.0001f)
            return;

        float avatarArmLength = GetLimbLength(
            avatarRightShoulder.position,
            avatarRightElbow.position,
            avatarRightHandBone.position
        );

        if (avatarArmLength < 0.0001f)
            return;

        Vector3 wristRelativeToShoulder = playerWrist - playerShoulder;
        Vector3 wristNormalized = wristRelativeToShoulder / playerArmLength;

        Vector3 avatarOffset = ConvertNormalizedArmOffset(wristNormalized, avatarArmLength);

        float outwardAmount = Mathf.Clamp01(Mathf.Abs(wristNormalized.x));
        Vector3 baseBias = new Vector3(lateralBias * outwardAmount, verticalBias, 0f);

        Vector3 targetPos = avatarRightShoulder.position + baseBias + avatarOffset + rightHandOffset;

        rightHandTarget.position = Vector3.Lerp(
            rightHandTarget.position,
            targetPos,
            Time.deltaTime * smoothSpeed
        );
    }

    private void UpdateLeftFoot(Vector3 playerHip, Vector3 playerKnee, Vector3 playerAnkle)
    {
        if (leftFootTarget == null)
            return;

        float playerLegLength = GetLimbLength(playerHip, playerKnee, playerAnkle);
        if (playerLegLength < 0.0001f)
            return;

        float avatarLegLength = GetLimbLength(
            avatarLeftHip.position,
            avatarLeftKnee.position,
            avatarLeftFootBone.position
        );

        if (avatarLegLength < 0.0001f)
            return;

        Vector3 ankleRelativeToHip = playerAnkle - playerHip;
        Vector3 ankleNormalized = ankleRelativeToHip / playerLegLength;

        Vector3 avatarOffset = ConvertNormalizedLegOffset(ankleNormalized, avatarLegLength);
        Vector3 targetPos = avatarLeftHip.position + avatarOffset + leftFootOffset;

        leftFootTarget.position = Vector3.Lerp(
            leftFootTarget.position,
            targetPos,
            Time.deltaTime * smoothSpeed
        );
    }

    private void UpdateRightFoot(Vector3 playerHip, Vector3 playerKnee, Vector3 playerAnkle)
    {
        if (rightFootTarget == null)
            return;

        float playerLegLength = GetLimbLength(playerHip, playerKnee, playerAnkle);
        if (playerLegLength < 0.0001f)
            return;

        float avatarLegLength = GetLimbLength(
            avatarRightHip.position,
            avatarRightKnee.position,
            avatarRightFootBone.position
        );

        if (avatarLegLength < 0.0001f)
            return;

        Vector3 ankleRelativeToHip = playerAnkle - playerHip;
        Vector3 ankleNormalized = ankleRelativeToHip / playerLegLength;

        Vector3 avatarOffset = ConvertNormalizedLegOffset(ankleNormalized, avatarLegLength);
        Vector3 targetPos = avatarRightHip.position + avatarOffset + rightFootOffset;

        rightFootTarget.position = Vector3.Lerp(
            rightFootTarget.position,
            targetPos,
            Time.deltaTime * smoothSpeed
        );
    }

    private Vector3 ToPlayerSpace(NormalizedLandmark lm)
    {
        float x = lm.x - 0.5f;
        float y = -(lm.y - 0.5f);
        float z = lm.z;

        if (mirrorX)
            x = -x;

        return new Vector3(x, y, z);
    }

    private float GetLimbLength(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Distance(a, b) + Vector3.Distance(b, c);
    }

    private Vector3 ConvertNormalizedArmOffset(Vector3 normalizedOffset, float avatarArmLength)
    {
        float x = normalizedOffset.x * avatarArmLength * xGain;
        float y = normalizedOffset.y * avatarArmLength * yGain;
        float z = normalizedOffset.z * avatarArmLength * zGain;

        return new Vector3(x, y, z);
    }

    private Vector3 ConvertNormalizedLegOffset(Vector3 normalizedOffset, float avatarLegLength)
    {
        float x = normalizedOffset.x * avatarLegLength * legXGain;
        float y = normalizedOffset.y * avatarLegLength * legYGain;
        float z = normalizedOffset.z * avatarLegLength * legZGain;

        return new Vector3(x, y, z);
    }

    private bool AvatarReferencesValid()
    {
        return avatarRoot != null &&
               avatarLeftShoulder != null &&
               avatarLeftElbow != null &&
               avatarLeftHandBone != null &&
               avatarRightShoulder != null &&
               avatarRightElbow != null &&
               avatarRightHandBone != null &&
               avatarLeftHip != null &&
               avatarLeftKnee != null &&
               avatarLeftFootBone != null &&
               avatarRightHip != null &&
               avatarRightKnee != null &&
               avatarRightFootBone != null;
    }
}