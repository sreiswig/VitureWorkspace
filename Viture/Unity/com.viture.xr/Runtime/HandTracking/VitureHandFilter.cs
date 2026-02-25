#if INCLUDE_UNITY_XR_HANDS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Viture.XR
{
    internal static class VitureHandFilter
    {
        internal static void SetMode(VitureHandFilterMode mode)
        {
            if (s_CurrentMode == mode)
                return;
            
            s_CurrentMode = mode;
            RefreshAllParameters();
        }
        
        internal static void ProcessJoint(Handedness handedness, VitureHandJointID jointId, ref Vector3 position, ref Quaternion rotation)
        {
            if (s_CurrentMode == VitureHandFilterMode.None)
                return;

            var filter = GetOrCreateJointFilter(handedness, jointId);
            filter.Filter(ref position, ref rotation, Time.time);
        }
        
        private static VitureHandFilterMode s_CurrentMode = VitureHandFilterMode.None;
        private static readonly Dictionary<(Handedness, VitureHandJointID), JointFilter> s_JointFilters = new();

        private static JointFilter GetOrCreateJointFilter(Handedness handedness, VitureHandJointID jointId)
        {
            var key = (handedness, jointId);

            if (!s_JointFilters.TryGetValue(key, out var filter))
            {
                var (minCutoff, beta) = GetFilterParams(jointId);
                filter = new JointFilter(minCutoff, beta);
                s_JointFilters[key] = filter;
            }

            return filter;
        }
        
        private static void RefreshAllParameters()
        {
            foreach (var (key, filter) in s_JointFilters)
            {
                var (minCutoff, beta) = GetFilterParams(key.Item2);
                filter.UpdateParams(minCutoff, beta);
            }
        }

        private static (float minCutoff, float beta) GetFilterParams(VitureHandJointID jointId)
        {
            var level = GetJointLevel(jointId);
            return GetParamsForLevel(s_CurrentMode, level);
        }

        private enum JointLevel
        {
            Wrist,
            Palm,
            Proximal,
            Intermediate,
            Distal,
            Tip
        }
        private static JointLevel GetJointLevel(VitureHandJointID jointId)
        {
            return jointId switch
            {
                VitureHandJointID.Wrist => JointLevel.Wrist,
                VitureHandJointID.Palm => JointLevel.Palm,
                
                VitureHandJointID.ThumbTip => JointLevel.Tip,
                VitureHandJointID.IndexTip => JointLevel.Tip,
                VitureHandJointID.MiddleTip => JointLevel.Tip,
                VitureHandJointID.RingTip => JointLevel.Tip,
                VitureHandJointID.LittleTip => JointLevel.Tip,
                
                VitureHandJointID.ThumbDistal => JointLevel.Distal,
                VitureHandJointID.IndexDistal => JointLevel.Distal,
                VitureHandJointID.MiddleDistal => JointLevel.Distal,
                VitureHandJointID.RingDistal => JointLevel.Distal,
                VitureHandJointID.LittleDistal => JointLevel.Distal,
                
                VitureHandJointID.IndexIntermediate => JointLevel.Intermediate,
                VitureHandJointID.MiddleIntermediate => JointLevel.Intermediate,
                VitureHandJointID.RingIntermediate => JointLevel.Intermediate,
                VitureHandJointID.LittleIntermediate => JointLevel.Intermediate,
                
                _ => JointLevel.Proximal
            };
        }

        private static (float minCutoff, float beta) GetParamsForLevel(VitureHandFilterMode mode, JointLevel level)
        {
            return mode switch
            {
                VitureHandFilterMode.Responsive => level switch
                {
                    JointLevel.Wrist => (1.0f, 1.2f),
                    JointLevel.Palm => (1.4f, 1.6f),
                    JointLevel.Proximal => (1.8f, 1.9f),
                    JointLevel.Intermediate => (1.9f, 2.1f),
                    JointLevel.Distal => (2f, 2.2f),
                    JointLevel.Tip => (2.3f, 2.4f),
                    _ => (1000f, 0f)
                },

                VitureHandFilterMode.Stable => level switch
                {
                    JointLevel.Wrist => (0.5f, 0.9f),
                    JointLevel.Palm => (0.8f, 1.0f),
                    JointLevel.Proximal => (0.9f, 1.1f),
                    JointLevel.Intermediate => (1.0f, 1.2f),
                    JointLevel.Distal => (1.2f, 1.3f),
                    JointLevel.Tip => (1.5f, 1.4f),
                    _ => (1000f, 0f)
                },

                _ => (1000f, 0f)
            };
        }

        private class JointFilter
        {
            private readonly OneEuroFilter m_PosX, m_PosY, m_PosZ;
            private readonly OneEuroFilter m_RotX, m_RotY, m_RotZ, m_RotW;

            private const float k_Frequency = 75f;

            internal JointFilter(float minCutoff, float beta)
            {
                m_PosX = new OneEuroFilter(minCutoff, beta, k_Frequency);
                m_PosY = new OneEuroFilter(minCutoff, beta, k_Frequency);
                m_PosZ = new OneEuroFilter(minCutoff, beta, k_Frequency);
                m_RotX = new OneEuroFilter(minCutoff, beta, k_Frequency);
                m_RotY = new OneEuroFilter(minCutoff, beta, k_Frequency);
                m_RotZ = new OneEuroFilter(minCutoff, beta, k_Frequency);
                m_RotW = new OneEuroFilter(minCutoff, beta, k_Frequency);
            }

            internal void Filter(ref Vector3 position, ref Quaternion rotation, float time)
            {
                position = new Vector3(
                    m_PosX.Filter(position.x, time),
                    m_PosY.Filter(position.y, time),
                    m_PosZ.Filter(position.z, time));
                
                rotation = new Quaternion(
                    m_RotX.Filter(rotation.x, time),
                    m_RotY.Filter(rotation.y, time),
                    m_RotZ.Filter(rotation.z, time),
                    m_RotW.Filter(rotation.w, time)).normalized;
            }

            internal void UpdateParams(float minCutoff, float beta)
            {
                m_PosX.UpdateParams(minCutoff, beta);
                m_PosY.UpdateParams(minCutoff, beta);
                m_PosZ.UpdateParams(minCutoff, beta);
                m_RotX.UpdateParams(minCutoff, beta);
                m_RotY.UpdateParams(minCutoff, beta);
                m_RotZ.UpdateParams(minCutoff, beta);
                m_RotW.UpdateParams(minCutoff, beta);
            }
        }

        private class OneEuroFilter
        {
            private float m_MinCutoff;
            private float m_Beta;
            private readonly float m_Frequency;
            private readonly LowPassFilter m_ValueFilter;
            private readonly LowPassFilter m_DerivativeFilter;
            private float m_LastTime = -1f;

            private const float k_DerivativeCutoff = 1f;

            internal OneEuroFilter(float minCutoff, float beta, float frequency = 0f)
            {
                m_MinCutoff = minCutoff;
                m_Beta = beta;
                m_Frequency = frequency;
                m_ValueFilter = new LowPassFilter();
                m_DerivativeFilter = new LowPassFilter();
            }

            internal void UpdateParams(float minCutoff, float beta)
            {
                m_MinCutoff = minCutoff;
                m_Beta = beta;
            }

            internal float Filter(float value, float timestamp = -1)
            {
                if (Mathf.Approximately(timestamp, -1)) timestamp = Time.time;

                if (Mathf.Approximately(m_LastTime, -1))
                {
                    m_LastTime = timestamp;
                    return m_ValueFilter.Apply(value, ComputeAlpha(value, 0));
                }

                var dt = timestamp - m_LastTime;
                m_LastTime = timestamp;

                float rate;
                if (m_Frequency > 0)
                    rate = m_Frequency;
                else
                    rate = (dt > 0) ? 1.0f / dt : 0f;

                var effectiveDt = (m_Frequency > 0) ? (1.0f / m_Frequency) : dt;
                var dx = (value - m_ValueFilter.lastValue) / effectiveDt;

                var edx = m_DerivativeFilter.Apply(dx, ComputeAlpha(rate, k_DerivativeCutoff));
                var cutoff = m_MinCutoff + m_Beta * Mathf.Abs(edx);

                return m_ValueFilter.Apply(value, ComputeAlpha(rate, cutoff));
            }

            private static float ComputeAlpha(float rate, float cutoff)
            {
                if (rate <= 0) return 1.0f;
                var tau = 1.0f / (2 * Mathf.PI * cutoff);
                var te = 1.0f / rate;
                return 1.0f / (1.0f + tau / te);
            }

            private class LowPassFilter
            {
                internal float lastValue { get; private set; }
                private bool m_Initialized;

                internal float Apply(float value, float alpha)
                {
                    if (!m_Initialized)
                    {
                        lastValue = value;
                        m_Initialized = true;
                        return value;
                    }

                    var newValue = lastValue + alpha * (value - lastValue);
                    lastValue = newValue;
                    return newValue;
                }
            }
        }
    }
}
#endif
