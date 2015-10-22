using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Org.Interactivity.Recognizer
{
    /// <summary>
    /// Interaction trigger that performs actions when specific gestures are recognised.
    /// <note type="note">
    /// The implementation will handle up to 5 digits (touch inputs) as there are lots of tablets with that limit.
    /// But it can easily be expanded to 10 digits if that is required,
    /// although I am not sure how useful it would be to do a 10 finger swipe.
    /// </note>
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
        /// GestureModifier dependency property key.
        /// </summary>
        public static readonly DependencyProperty GestureModifierProperty = DependencyProperty.Register(
            "GestureModifier", typeof(GestureModifier), typeof(GestureRecognizer), new PropertyMetadata(GestureModifier.OneFinger));

        private readonly IDictionary<int, int> _touchRegistry = new Dictionary<int, int>();

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
        /// Can be used to increase the number of points to be track to trigger a gesture (swipe with one finger, two or more).
        /// By default a GestureRecognizer instance will be tracking a single digit.
        /// </summary>
        public GestureModifier GestureModifier
        {
            get { return (GestureModifier)GetValue(GestureModifierProperty); }
            set { SetValue(GestureModifierProperty, value); }
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
                AssociatedObject.ManipulationDelta += HandleManipulationDelta;
            };
        }

        /// <summary>
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.ManipulationStarting -= HandleManipulationStarting;
            AssociatedObject.ManipulationCompleted -= HandleManipulationCompleted;
            AssociatedObject.ManipulationDelta -= HandleManipulationDelta;
        }

        private static void HandleAutoManipulationEnabled(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as GestureRecognizer;
            if (instance?.AssociatedObject != null)
            {
                instance.AssociatedObject.IsManipulationEnabled = instance.AutoManipulationEnabled;
            }
        }

        private void HandleManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = AssociatedObject;
            e.Mode = ManipulationModes.All;
            ClearRegisteredTouches();
        }

        private void HandleManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            var gesture = ToSwipeGesture(e.TotalManipulation.Translation, e.FinalVelocities.LinearVelocity);
            var modifier = GetMostCommonNumberOfTouchInputs(_touchRegistry).Map(TouchesToGestureModifier);

            // Raising actions if is any gesture with any modifier (or a matching modifier).
            if (TriggerOnGesture == Gesture.All && (GestureModifier == GestureModifier.Any || modifier == GestureModifier.ToOption()))
            {
                InvokeActions(gesture);
            }
            // otherwise raising actions for taps if modifier could not be identifier (tapping does not generate deltas) or it has a matching modifier.
            else if (TriggerOnGesture == Gesture.Tap && gesture == Gesture.Tap && (modifier.IsEmpty() || modifier == GestureModifier.ToOption()))
            {
                InvokeActions(gesture);
            }
            // finally, raising for any other gesture with the appropriate modifier.
            else if (TriggerOnGesture == gesture && modifier == GestureModifier.ToOption())
            {
                InvokeActions(gesture);
            }
        }

        private void HandleManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var element = e.ManipulationContainer as UIElement;
            RegisterNumberOfTouchesOn(element);
        }

        private void ClearRegisteredTouches()
        {
            _touchRegistry.Clear();
        }

        private void RegisterNumberOfTouchesOn(UIElement element)
        {
            if (element != null)
            {
                var touches = element.TouchesOver.Count();
                if (!_touchRegistry.ContainsKey(touches))
                {
                    _touchRegistry[touches] = 1;
                }
                else
                {
                    _touchRegistry[touches]++;
                }
            }
        }

        private static GestureModifier TouchesToGestureModifier(int numberOfTouchInputs)
        {
            switch (numberOfTouchInputs)
            {
                case 1: return GestureModifier.OneFinger;
                case 2: return GestureModifier.TwoFingers;
                case 3: return GestureModifier.ThreeFingers;
                case 4: return GestureModifier.FourFingers;
                default: return GestureModifier.FiveFingers;
            }
        }

        private static Option<int> GetMostCommonNumberOfTouchInputs(IDictionary<int, int> touchRegistry)
        {
            if (touchRegistry.Count > 0)
            {
                var touchesOrderedByAppearance = touchRegistry.OrderByDescending(pair => pair.Value);
                return Option.Full(touchesOrderedByAppearance.First().Key);
            }
            return Option.Empty();
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
            var regressionScore = translationLengthCoeff * translation.Length + inertiaLengthCoeff * linearVelocity.Length +
                                    intercept;
            var inverseProbablity = 1 + Math.Exp(-regressionScore);
            return 1 / inverseProbablity > 0.5;
        }
    }
}