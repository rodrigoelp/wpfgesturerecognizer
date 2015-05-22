using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Org.Interactivity.Recognizer
{
    /// <summary>
    /// Interaction trigger that performs actions when specific gestures are recognised.
    /// </summary>
    public sealed class GestureRecognizer : TriggerBase<FrameworkElement>
    {
        //This magic number corresponds to observations of what makes a tap versus a swipe or flick.
        private const int DefaultTapThreshold = 40;

        /// <summary>
        /// AutoManipulationEnabled property dependency property key.
        /// </summary>
        public static readonly DependencyProperty AutoManipulationEnabledProperty = DependencyProperty.Register(
            "AutoManipulationEnabled", typeof(bool), typeof(GestureRecognizer), new PropertyMetadata(true, HandleAutoManipulationEnabled));

        /// <summary>
        /// TriggerOnGesture property dependency property key.
        /// </summary>
        public static readonly DependencyProperty TriggerOnGestureProperty = DependencyProperty.Register(
            "TriggerOnGesture", typeof(Gesture), typeof(GestureRecognizer), new PropertyMetadata(Gesture.All));

        /// <summary>
        /// Set the tap threshold. If swipes are being recognised as taps lowering this value may help.
        /// </summary>
        public static readonly DependencyProperty TapThresholdProperty = DependencyProperty.Register(
            "TapThreshold", typeof(int), typeof(GestureRecognizer), new PropertyMetadata(DefaultTapThreshold));

        /// <summary>
        /// Use velocity for tap detection, rather than just gesture translation size. Defaults to off.
        /// </summary>
        public static readonly DependencyProperty UseVelocityForTapDetectionProperty = DependencyProperty.Register(
            "UseVelocityForTapDetection", typeof(bool), typeof(GestureRecognizer), new PropertyMetadata(false));


        /// <summary>
        /// When turned on, it sets the <see cref="UIElement.IsManipulationEnabled"/> property on the <see cref="TriggerBase{T}.AssociatedObject"/> to detect gestures.
        /// By default is set to True.
        /// </summary>
        public bool AutoManipulationEnabled
        {
            get { return (bool)GetValue(AutoManipulationEnabledProperty); }
            set { SetValue(AutoManipulationEnabledProperty, value); }
        }

        /// <summary>
        /// Gesture that will trigger associated actions.
        /// </summary>
        public Gesture TriggerOnGesture
        {
            get { return (Gesture)GetValue(TriggerOnGestureProperty); }
            set { SetValue(TriggerOnGestureProperty, value); }
        }

        /// <summary>
        /// Set the tap threshold. If swipes are being recognised as taps lowering this value may help.
        /// </summary>
        public int TapThreshold
        {
            get { return (int)GetValue(TapThresholdProperty); }
            set { SetValue(TapThresholdProperty, value); }
        }

        /// <summary>
        /// Use velocity for tap detection, rather than just gesture translation size. Defaults to <code>false</code>.
        /// </summary>
        public bool UseVelocityForTapDetection
        {
            get { return (bool)GetValue(UseVelocityForTapDetectionProperty); }
            set { SetValue(UseVelocityForTapDetectionProperty, value); }
        }

        /// <summary>
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            if (AutoManipulationEnabled)
            {
                AssociatedObject.IsManipulationEnabled = AutoManipulationEnabled;
            }

            AssociatedObject.Loaded += (sender, args) =>
            {
                AssociatedObject.ManipulationStarting += HandleManipulationStarting;
                AssociatedObject.ManipulationCompleted += HandleManipulationCompleted;
            };
        }

        /// <summary>
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.ManipulationStarting -= HandleManipulationStarting;
            AssociatedObject.ManipulationCompleted -= HandleManipulationCompleted;
        }

        private static void HandleAutoManipulationEnabled(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as GestureRecognizer;
            if (instance != null && instance.AssociatedObject != null)
            {
                instance.AssociatedObject.IsManipulationEnabled = instance.AutoManipulationEnabled;
            }
        }

        private void HandleManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = AssociatedObject;
            e.Mode = ManipulationModes.All;
        }

        private void HandleManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            var gesture = ToSwipeGesture(e.TotalManipulation.Translation, e.FinalVelocities.LinearVelocity);
            if (TriggerOnGesture == Gesture.All || TriggerOnGesture == gesture)
            {
                InvokeActions(gesture);
            }
        }

        private Gesture ToSwipeGesture(Vector translation, Vector linearVelocity)
        {
            var deltaX = translation.X;
            var deltaY = translation.Y;
            var distX = Math.Abs(deltaX);
            var distY = Math.Abs(deltaY);
            var isTap = UseVelocityForTapDetection
                ? DetectTapFromVelocity(translation, linearVelocity)
                : distX <= TapThreshold && distY <= TapThreshold;
            if (isTap)
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

        /// <summary>
        /// Try to detect tap from a mix of translation and velocity.
        /// </summary>
        /// <remarks>
        /// Based on logistical regression over 600 samples. Analysis done by Ryan Melman.
        /// </remarks>
        /// <param name="translation"></param>
        /// <param name="linearVelocity"></param>
        /// <returns></returns>
        private static bool DetectTapFromVelocity(Vector translation, Vector linearVelocity)
        {
            const double translationLengthCoeff = -0.029;
            const double inertiaLengthCoeff = -0.029;
            const double intercept = 1.638;
            var regressionScore = translationLengthCoeff*translation.Length + inertiaLengthCoeff*linearVelocity.Length +
                                    intercept;
            var inverseProbablity = 1 + Math.Exp(-regressionScore);
            return 1/inverseProbablity > 0.5;

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