using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Org.Interactivity.Recognizer
{
    public class GestureRecognizer : TriggerBase<FrameworkElement>
    {
        private const int TapThreshold = 40;

        public Gesture TriggerOnGesture { get; set; }

        public GestureRecognizer()
        {
            TriggerOnGesture = Gesture.All;
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ManipulationStarting += HandleManipulationStarting;
            AssociatedObject.ManipulationCompleted += HandleManipulationCompleted;
        }

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

    public enum Gesture : byte
    {
        All = 0,
        SwipeUp,
        SwipeDown,
        SwipeLeft,
        SwipeRight,
        Tap
    }
}