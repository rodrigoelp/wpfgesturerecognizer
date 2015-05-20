using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Org.Interactivity.Recognizer
{
    /// <summary>
    /// Interaction trigger that performs actions when specific gestures are recognised.
    /// </summary>
    public class GestureRecognizer : TriggerBase<FrameworkElement>
    {
        private const int TapThreshold = 40;

        /// <summary>
        /// Gesture that will trigger associated actions.
        /// </summary>
        public Gesture TriggerOnGesture { get; set; }

        /// <summary>
        /// Create GestureRecognizer that triggers on all gestures.
        /// </summary>
        public GestureRecognizer()
        {
            TriggerOnGesture = Gesture.All;
        }

        /// <summary>
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ManipulationStarting += HandleManipulationStarting;
            AssociatedObject.ManipulationCompleted += HandleManipulationCompleted;
        }

        /// <summary>
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.ManipulationStarting -= HandleManipulationStarting;
            AssociatedObject.ManipulationCompleted -= HandleManipulationCompleted;
            base.OnDetaching();
        }

        private void HandleManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = AssociatedObject;
            e.Mode = ManipulationModes.All;
        }

        private void HandleManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
                InvokeActions(ToSwipeGesture(e.TotalManipulation.Translation));
        }

        private static Gesture ToSwipeGesture(Vector translation)
        {
            var deltaX = translation.X;
            var deltaY = translation.Y;
            var distX = Math.Abs(deltaX);
            var distY = Math.Abs(deltaY);
            if (deltaX <= TapThreshold && deltaY <= TapThreshold)
            {
                return Gesture.Tap;
            }
            else if (distY >= distX) // bias towards vertical swipe over horizontal if distances are equal
            {
                return deltaY > 0 ? Gesture.SwipeDown : Gesture.SwipeUp;
            }
            else
            {
                return deltaX > 0 ? Gesture.SwipeRight : Gesture.SwipeLeft;
            }
        }
    }

    /// <summary>
    /// Recognisable gestures. Has <see cref="FlagsAttribute">FlagsAttribute</see> to allow combinations
    /// of values (e.g. <code>Gesture.SwipeUp | Gesture.SwipeDown</code>).
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