namespace Org.Interactivity.Recognizer
{
    /// <summary>
    /// Modifier to be applied to a <see cref="Gesture"/> to be trigger upon recognition.
    /// </summary>
    public enum GestureModifier : byte
    {
        /// <summary>
        /// Gesture will be triggered regardless of the number of fingers detected.
        /// </summary>
        Any = 0,
        /// <summary>
        /// Gesture will be triggered when is performed with a single digit.
        /// </summary>
        OneFinger = 1,
        /// <summary>
        /// Gesture will be triggered when is performed with two fingers.
        /// </summary>
        TwoFingers = 2,
        /// <summary>
        /// Gesture will be triggered when is performed with three fingers.
        /// </summary>
        ThreeFingers = 3,
        /// <summary>
        /// Gesture will be triggered when is performed with four fingers.
        /// </summary>
        FourFingers = 4,
        /// <summary>
        /// Gesture will be triggered when is performed with five fingers.
        /// </summary>
        FiveFingers = 5
    }
}