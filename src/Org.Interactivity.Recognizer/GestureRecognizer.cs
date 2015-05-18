using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Org.Interactivity.Recognizer
{
    public class GestureRecognizer : TriggerBase<FrameworkElement>
    {
        private const double Threshold = 0.1;

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
            if (IsWorthLookingAtThis(e.TotalManipulation.Translation.Length) || //swipe
                IsWorthLookingAtThis(e.FinalVelocities.LinearVelocity.Length)) //flick
            {
                InvokeActions(ToSwipeGesture(e.TotalManipulation.Translation, e.ManipulationOrigin));
            }
            else //tap
            {
                InvokeActions(Gesture.Tap);
            }
        }

        private static bool IsWorthLookingAtThis(double length)
        {
            return length > Threshold;
        }

        private static Gesture ToSwipeGesture(Vector vector, Point origin)
        {
            var isVerticalGesture = Math.Abs(vector.Y) > Math.Abs(vector.X);
            if (isVerticalGesture)
            {
                //vertical swipe
                return vector.Y > 0 ? Gesture.SwipeUp : Gesture.SwipeDown;
            }
            //horizontal
            return vector.X > 0 ? Gesture.SwipeRight : Gesture.SwipeLeft;
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