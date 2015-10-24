using System.Windows;
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
    public sealed class GestureRecognizer : TriggerBase<FrameworkElement>, IGestureRecognitionObserver
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

            AssociatedObject.Loaded += (sender, args) => RecognitionCentral.Default.AddObserver(this, AssociatedObject);
        }

        /// <summary>
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            RecognitionCentral.Default.RemoveObserver(this, AssociatedObject);
        }

        private static void HandleAutoManipulationEnabled(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as GestureRecognizer;
            if (instance?.AssociatedObject != null)
            {
                instance.AssociatedObject.IsManipulationEnabled = instance.AutoManipulationEnabled;
            }
        }

        /// <summary>
        /// Explicit implementation of the GestureRecognitionObserver, handling a detected gesture to determine if an action needs to be raised.
        /// </summary>
        /// <param name="gesture">Detected gesture</param>
        /// <param name="modifier">Detected (option) modifier.</param>
        void IGestureRecognitionObserver.OnGestureDetected(Gesture gesture, Option<GestureModifier> modifier)
        {
            // Raising actions if is any gesture with any modifier (or a matching modifier).
            if (TriggerOnGesture == Gesture.All && (GestureModifier == GestureModifier.Any || modifier == GestureModifier.ToOption()))
            {
                InvokeActions(gesture);
            }
            // otherwise raising actions for taps if modifier could not be identifier (tapping does not generate deltas) or it has a matching modifier.
            else if (TriggerOnGesture == Gesture.Tap && gesture == Gesture.Tap && (GestureModifier == GestureModifier.Any || modifier.IsEmpty() || modifier == GestureModifier.ToOption()))
            {
                InvokeActions(gesture);
            }
            // finally, raising for any other gesture with the appropriate modifier.
            else if (TriggerOnGesture == gesture && (GestureModifier == GestureModifier.Any || modifier == GestureModifier.ToOption()))
            {
                InvokeActions(gesture);
            }
        }
    }
}