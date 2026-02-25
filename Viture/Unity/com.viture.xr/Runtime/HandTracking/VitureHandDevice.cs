#if INCLUDE_UNITY_XR_HANDS
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace Viture.XR.InputDevices
{
    /// <summary>
    /// Input state for VITURE hand tracking device.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VitureHandState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('V', 'H', 'N', 'D');

        [InputControl(name = "pinchPosition", layout = "Vector3")]
        public Vector3 pinchPosition;

        [InputControl(name = "aimPosition", layout = "Vector3")]
        public Vector3 aimPosition;

        [InputControl(name = "aimRotation", layout = "Quaternion")]
        public Quaternion aimRotation;

        [InputControl(name = "pokePosition", layout = "Vector3")]
        public Vector3 pokePosition;

        [InputControl(name = "pokeRotation", layout = "Quaternion")]
        public Quaternion pokeRotation;
        
        [InputControl(name = "select", layout = "Button")]
        public bool select;

        [InputControl(name = "gesture", layout = "Integer")]
        public int gesture;

        [InputControl(name = "palmFacing", layout = "Integer")]
        public int palmFacing;
    }
    
    /// <summary>
    /// VITURE hand tracking input device for Unity Input System integration.
    /// </summary>
    [InputControlLayout(
        stateType = typeof(VitureHandState),
        displayName = "Viture Hand Device",
        commonUsages = new[] { "LeftHand", "RightHand" }
    )]
    public class VitureHandDevice : TrackedDevice
    {
        public static VitureHandDevice LeftHand {get; private set;}
        public static VitureHandDevice RightHand {get; private set;}
        
        public Vector3Control pinchPosition { get; private set; }
        public Vector3Control aimPosition { get; private set; }
        public QuaternionControl aimRotation { get; private set; }
        public Vector3Control pokePosition { get; private set; }
        public QuaternionControl pokeRotation { get; private set; }
        public ButtonControl select { get; private set; }
        public IntegerControl gesture { get; private set; }
        public IntegerControl palmFacing { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();
            
            pinchPosition = GetChildControl<Vector3Control>("pinchPosition");
            aimPosition = GetChildControl<Vector3Control>("aimPosition");
            aimRotation = GetChildControl<QuaternionControl>("aimRotation");
            pokePosition = GetChildControl<Vector3Control>("pokePosition");
            pokeRotation = GetChildControl<QuaternionControl>("pokeRotation");
            select = GetChildControl<ButtonControl>("select");
            gesture = GetChildControl<IntegerControl>("gesture");
            palmFacing = GetChildControl<IntegerControl>("palmFacing");
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeDevices()
        {
            try
            {
                InputSystem.RegisterLayout<VitureHandDevice>(
                    matches: new InputDeviceMatcher().WithInterface("VitureHand"));
            }
            catch (System.InvalidOperationException ex)
            {
                Debug.Log($"VitureHandDevice layout already registered: {ex.Message}");
            }

            if (LeftHand == null)
            {
                LeftHand = InputSystem.AddDevice<VitureHandDevice>("Viture Left Hand");
                if (LeftHand != null)
                {
                    InputSystem.SetDeviceUsage(LeftHand, UnityEngine.InputSystem.CommonUsages.LeftHand);
                }
            }

            if (RightHand == null)
            {
                RightHand = InputSystem.AddDevice <VitureHandDevice>("Viture Right Hand");
                if (RightHand != null)
                {
                    InputSystem.SetDeviceUsage(RightHand, UnityEngine.InputSystem.CommonUsages.RightHand);
                }
            }
        }

        public static void UpdateDeviceState(Handedness handedness, bool isTracked, InputTrackingState trackingState,
            Vector3 devicePosition, Quaternion deviceRotation, VitureHandState state)
        {
            var device = handedness == Handedness.Left ? LeftHand : RightHand;
            if (device != null)
            {
                InputSystem.QueueDeltaStateEvent(device.isTracked, isTracked);
                InputSystem.QueueDeltaStateEvent(device.trackingState, trackingState);
                InputSystem.QueueDeltaStateEvent(device.devicePosition, devicePosition);
                InputSystem.QueueDeltaStateEvent(device.deviceRotation, deviceRotation);
                
                InputSystem.QueueStateEvent(device, state);
            }
        }
    }
}
#endif
