using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

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
        /// AllowInertiaOnTranslate  dependency property key. If enabled, allows intertia to continue manipulation.
        /// </summary>
        public static readonly DependencyProperty AllowInertiaOnTranslateProperty = DependencyProperty.Register(
            "AllowInertiaOnTranslate", typeof (bool), typeof (GestureRecognizer), new PropertyMetadata(default(bool), AllowInertiaOnTranslateChanged));

        private static void AllowInertiaOnTranslateChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            var recognizer = obj as GestureRecognizer;
            if (recognizer?.AssociatedObject == null) return;
            var value = (bool) e.NewValue;
            if (value && recognizer.TranslateWithManipulationEnabled) {
                recognizer.AssociatedObject.ManipulationInertiaStarting -= AssociatedObjectOnManipulationStarting;
                recognizer.AssociatedObject.ManipulationInertiaStarting += AssociatedObjectOnManipulationStarting;
            }
            else recognizer.AssociatedObject.ManipulationInertiaStarting -= AssociatedObjectOnManipulationStarting;
        }

        /// <summary>
        /// TranslationWithManipulationEnabled dependency property key. Allows real-time translation/rotation/zoom manipulation.
        /// </summary>
        public static readonly DependencyProperty TranslateWithManipulationEnabledProperty = DependencyProperty.Register(
            "TranslateWithManipulationEnabled", typeof (bool), typeof (GestureRecognizer), new PropertyMetadata(default(bool), HandleTranslateWithManipulationEnabled));

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
        /// Allows intertial momentum to continue translation after the finger touch has let off.
        /// </summary>
        public bool AllowInertiaOnTranslate {
            get { return (bool) GetValue(AllowInertiaOnTranslateProperty); }
            set { SetValue(AllowInertiaOnTranslateProperty, value); }
        }

        /// <summary>
        /// When enabled, touch gestures will manipulate the attached UIElement's translate/rotate/zoom. ScrollViewers will 
        /// translate the offset only.
        /// </summary>
        public bool TranslateWithManipulationEnabled {
            get { return (bool) GetValue(TranslateWithManipulationEnabledProperty); }
            set { SetValue(TranslateWithManipulationEnabledProperty, value); }
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
            if (TranslateWithManipulationEnabled)
            {
                AssociatedObject.ManipulationDelta += AssociatedObjectOnManipulationDelta;
            }
            if (AllowInertiaOnTranslate)
            {
                AssociatedObject.ManipulationInertiaStarting += AssociatedObjectOnManipulationStarting;
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
        
        private static void HandleTranslateWithManipulationEnabled(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            var recognizer = obj as GestureRecognizer;
            if (recognizer?.AssociatedObject == null) return;
            var value = (bool) e.NewValue;
            if (value) {
                if (!recognizer.AssociatedObject.IsManipulationEnabled)
                    recognizer.AssociatedObject.IsManipulationEnabled = true;
                recognizer.AssociatedObject.ManipulationDelta -= AssociatedObjectOnManipulationDelta;
                recognizer.AssociatedObject.ManipulationDelta += AssociatedObjectOnManipulationDelta;
            }
            else recognizer.AssociatedObject.ManipulationDelta -= AssociatedObjectOnManipulationDelta;
        }

        private static void AssociatedObjectOnManipulationStarting(object sender, ManipulationInertiaStartingEventArgs e) {
            e.TranslationBehavior.DesiredDeceleration = 10.0*96.0/(1000.0*1000.0);
            e.ExpansionBehavior.DesiredDeceleration = 0.1*96.0/(1000.0*1000.0);
            e.RotationBehavior.DesiredDeceleration = 720/(1000.0*1000.0);
            e.Handled = true;
        }

        private static void AssociatedObjectOnManipulationDelta(object sender, ManipulationDeltaEventArgs e) {
            if (e.Source is ScrollViewer)
            {
                var scrollviewer = e.Source as ScrollViewer;
                if (scrollviewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                    scrollviewer.ScrollToHorizontalOffset(scrollviewer.HorizontalOffset - e.DeltaManipulation.Translation.X);
                if (scrollviewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    scrollviewer.ScrollToVerticalOffset(scrollviewer.VerticalOffset - e.DeltaManipulation.Translation.Y);
            }
            else if (e.Source is FrameworkElement)
            {
                ManipulationDelta cm = e.CumulativeManipulation;
                ManipulationDelta dm = e.DeltaManipulation;
                FrameworkElement fe = e.Source as FrameworkElement;
                Matrix matrix = ((MatrixTransform) fe.RenderTransform).Matrix;
                Vector pastEdgeVector;

                if (ElementPastBoundary(e.Source as FrameworkElement, out pastEdgeVector) && e.IsInertial)
                {
                    matrix.Translate(-1.0*pastEdgeVector.X, -1.0*pastEdgeVector.Y);
                    fe.RenderTransform = new MatrixTransform(matrix);

                    e.Complete();
                    e.Handled = true;
                    return;
                }

                // Rotate the Rectangle.
                matrix.RotateAt(dm.Rotation,
                                     e.ManipulationOrigin.X,
                                     e.ManipulationOrigin.Y);

                // Resize the Rectangle.  Keep it square 
                // so use only the X value of Scale.
                matrix.ScaleAt(dm.Scale.X,
                                    dm.Scale.X,
                                    e.ManipulationOrigin.X,
                                    e.ManipulationOrigin.Y);

                // Move the Rectangle.
                matrix.Translate(cm.Translation.X,
                                      cm.Translation.Y);

                // Apply the changes to the Rectangle.
                fe.RenderTransform = new MatrixTransform(matrix);
                e.Handled = true;
            }
        }

        private static bool ElementPastBoundary(FrameworkElement fe, out Vector pastEdgeVector) {
            bool pastEdge = false;

            pastEdgeVector = new Vector();

            FrameworkElement feParent = fe.Parent as FrameworkElement;
            if (feParent != null) {
                Rect feRect = fe.TransformToAncestor(feParent).TransformBounds(
                                                                               new Rect(0.0, 0.0, fe.ActualWidth, fe.ActualHeight));

                if (feRect.Right > feParent.ActualWidth)
                    pastEdgeVector.X = feRect.Right - feParent.ActualWidth;

                if (feRect.Left < 0)
                    pastEdgeVector.X = feRect.Left;

                if (feRect.Bottom > feParent.ActualHeight)
                    pastEdgeVector.Y = feRect.Bottom - feParent.ActualHeight;

                if (feRect.Top < 0)
                    pastEdgeVector.Y = feRect.Top;

                if ((pastEdgeVector.X != 0) || (pastEdgeVector.Y != 0))
                    pastEdge = true;
            }

            return pastEdge;
        }

    }
}