namespace Viture.XR
{
    /// <summary>
    /// Model types of VITURE glasses.
    /// </summary>
    public enum VitureGlassesModel
    {
        /// <summary>
        /// Unknown glasses model.
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// Includes VITURE One and VITURE One Lite glasses.
        /// </summary>
        One = 1,
        
        /// <summary>
        /// VITURE One Pro glasses.
        /// </summary>
        Pro = 2,
        
        /// <summary>
        /// Includes VITURE Luma and VITURE Luma Pro glasses.
        /// </summary>
        Luma = 3,
        
        /// <summary>
        /// VITURE Luma Ultra glasses.
        /// </summary>
        LumaUltra = 4,
        
        /// <summary>
        /// VITURE Beast glasses.
        /// </summary>
        Beast = 5
    }
    
    /// <summary>
    /// Head tracking capabilities supported by different VITURE glasses models.
    /// </summary>
    public enum VitureHeadTrackingCapability
    {
        /// <summary>
        /// 3 degrees of freedom tracking (rotation only)
        /// </summary>
        ThreeDoF = 0,
        
        /// <summary>
        /// 6 degrees of freedom tracking (rotation and position)
        /// </summary>
        SixDoF = 1
    }

    /// <summary>
    /// Defines which VITURE glasses models the application supports.
    /// Used by the system to verify compatibility with the connected glasses.
    /// </summary>
    public enum VitureAppGlassesSupport
    {
        /// <summary>
        /// Application supports only 6DoF glasses.
        /// </summary>
        [UnityEngine.InspectorName("6DoF Glasses Only")]
        SixDoFOnly = 0,
        
        /// <summary>
        /// Application supports both 3DoF and 6DoF glasses.
        /// </summary>
        [UnityEngine.InspectorName("Both 3DoF and 6DoF Glasses")]
        Both = 1
    }
    
    /// <summary>
    /// Hand gestures recognized by VITURE hand tracking system.
    /// </summary>
    public enum VitureGesture
    {
        /// <summary>
        /// No specific gesture detected.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Pinch gesture (thumb and index finger touching).
        /// Commonly used for selection and interaction.
        /// </summary>
        Pinch = 1,
        
        /// <summary>
        /// Closed fist gesture.
        /// </summary>
        Fist = 2
    }
    
    /// <summary>
    /// Direction the palm is facing relative to gravity.
    /// </summary>
    public enum ViturePalmFacing
    {
        /// <summary>
        /// Palm is facing downward (toward the ground).
        /// </summary>
        Down = 0,
        
        /// <summary>
        /// Palm is facing upward (away from the ground).
        /// </summary>
        Up = 1
    }
    
    /// <summary>
    /// Hand tracking filter modes that balance responsiveness and stability.
    /// </summary>
    public enum VitureHandFilterMode
    {
        /// <summary>
        /// No filtering applied. Uses raw hand tracking data.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Lower latency with faster response to hand movement.
        /// May exhibit visible jitter.
        /// </summary>
        Responsive = 1,
        
        /// <summary>
        /// Reduced jitter with smoother hand visualization.
        /// Has higher latency.
        /// </summary>
        Stable = 2,
    }
}
