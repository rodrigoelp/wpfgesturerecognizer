using System;

namespace Org.Interactivity.Recognizer
{
    /// <summary>
    /// Recognisable gestures. Has <see cref="FlagsAttribute">FlagsAttribute</see> to allow combinations
    /// of values (e.g. <code>Gesture.SwipeUp | Gesture.SwipeDown</code>).
    /// Recognised gestures will be affected by <see cref="GestureModifier"/>.
    /// </summary>
    [Flags]
    public enum Gesture : byte
    {
        /// <summary>None </summary>
        None = 0,
        /// <summary>Swipe up</summary>
        SwipeUp = 1,
        /// <summary>Swipe down</summary>
        SwipeDown = 2,
        /// <summary>Swipe left</summary>
        SwipeLeft = 4,
        /// <summary>Swipe right</summary>
        SwipeRight = 8,
        /// <summary>Tap</summary>
        Tap = 16,
        /// <summary>All gestures</summary>
        All = SwipeUp | SwipeDown | SwipeLeft | SwipeRight | Tap
    }
}