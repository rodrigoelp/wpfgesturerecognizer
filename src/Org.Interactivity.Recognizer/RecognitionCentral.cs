using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Org.Interactivity.Recognizer
{
    /// <summary>
    /// The RecognitionCentral is a unified place in which to handle subscriptions a given <see cref="UIElement" /> (or set of) associated to a GestureRecognizer,
    /// decreasing the load on the number of elements inspecting the state of the <see cref="UIElement"/>.
    /// </summary>
    internal class RecognitionCentral
    {
        private readonly Dictionary<UIElement, HashSet<IGestureRecognitionObserver>> _observersOfElement = new Dictionary<UIElement, HashSet<IGestureRecognitionObserver>>();
        private readonly Dictionary<UIElement, Dictionary<int, int>> _gestureSurfaceTouchRegistry = new Dictionary<UIElement, Dictionary<int, int>>();

        /// <summary>
        /// Default instance of the RecognitionCentral.
        /// </summary>
        internal static RecognitionCentral Default = new RecognitionCentral();

        /// <summary>
        /// Adds a subscription to the element manipulation API of a <see cref="UIElement"/> to then
        /// be able to notify an observer of any gesture detected.
        /// </summary>
        /// <param name="observer">Observer associated to a gesture surface, to process any gesture detected.</param>
        /// <param name="gestureSurface">Surface associated with the observer in which the gesture will take place.</param>
        internal void AddObserver(IGestureRecognitionObserver observer, UIElement gestureSurface)
        {
            // Register the surface first, using the fact the observer registry (_observersOfElement) doesn't have a reference
            // to the surface.
            RegisterGestureSurface(gestureSurface);

            // Then you register the observer.
            var observers = GetObserversFor(gestureSurface);
            observers = IncludeObserver(observers, observer);
            _observersOfElement[gestureSurface] = observers;
        }

        /// <summary>
        /// Drops the subscription to the gesture surface and its associated observer.
        /// </summary>
        /// <param name="observer">Observer associated to a gesture surface, to process any gesture detected.</param>
        /// <param name="gestureSurface">Surface associated with the observer in which the gesture will take place.</param>
        internal void RemoveObserver(IGestureRecognitionObserver observer, UIElement gestureSurface)
        {
            // Removing the observer from the subscriptions
            var observers = GetObserversFor(gestureSurface);
            observers.RemoveWhere(o => o == observer);
            _observersOfElement[gestureSurface] = observers;

            // If there is nobody observing the element, let's drop it.
            if (!_observersOfElement[gestureSurface].Any())
            {
                // clearing the subscriptions
                gestureSurface.ManipulationStarting -= HandleManipulationStarting;
                gestureSurface.ManipulationCompleted -= HandleManipulationCompleted;
                gestureSurface.ManipulationDelta -= HandleManipulationDelta;
                // removing everything else.
                _observersOfElement.Remove(gestureSurface);
            }
        }

        private static HashSet<IGestureRecognitionObserver> IncludeObserver(HashSet<IGestureRecognitionObserver> observers, IGestureRecognitionObserver observer)
        {
            observers.Add(observer);
            return observers;
        }

        private void RegisterGestureSurface(UIElement gestureSurface)
        {
            // skip pre-existing subscriptions.
            if (_observersOfElement.ContainsKey(gestureSurface))
                return;

            gestureSurface.ManipulationStarting += HandleManipulationStarting;
            gestureSurface.ManipulationCompleted += HandleManipulationCompleted;
            gestureSurface.ManipulationDelta += HandleManipulationDelta;

            _gestureSurfaceTouchRegistry[gestureSurface] = new Dictionary<int, int>();
        }

        private void HandleManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            sender.AsOption<UIElement>().Do(
                el =>
                {
                    e.ManipulationContainer = el;
                    e.Mode = ManipulationModes.All;
                    e.Handled = true;

                    if (_gestureSurfaceTouchRegistry.ContainsKey(el))
                    {
                        _gestureSurfaceTouchRegistry[el] = new Dictionary<int, int>();
                    }
                });
        }

        private void HandleManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            sender.AsOption<UIElement>().Do(
                el =>
                {
                    var touches = el.TouchesOver.Count();
                    var touchRegistry = _gestureSurfaceTouchRegistry[el];
                    if (!touchRegistry.ContainsKey(touches))
                    {
                        touchRegistry[touches] = 1;
                    }
                    else
                    {
                        touchRegistry[touches]++;
                    }
                });
        }

        private void HandleManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            sender.AsOption<UIElement>().Do(
                element =>
                {
                    e.Handled = true;
                    var modifier =
                        GetMostRepresentativeNumberOfTouchPoints(_gestureSurfaceTouchRegistry[element])
                            .Map(ToGestureModifier);

                    var totalTranslation = e.TotalManipulation.Translation;
                    var finalLinearVelocity = e.FinalVelocities.LinearVelocity;
                    var observers = _observersOfElement[element];
                    var observersPairedWithGesture =
                        observers.Select(
                            o =>
                                new
                                {
                                    Observer = o,
                                    Gesture =
                                        ToSwipeGesture(totalTranslation, finalLinearVelocity,
                                            o.UseVelocityForTapDetection, o.TapThreshold),
                                    Modifier = modifier
                                });

                    foreach (var observerWithGesture in observersPairedWithGesture)
                    {
                        observerWithGesture.Observer.OnGestureDetected(observerWithGesture.Gesture, observerWithGesture.Modifier);
                    }
                });
        }

        private HashSet<IGestureRecognitionObserver> GetObserversFor(UIElement gestureSurface)
        {
            return _observersOfElement.ContainsKey(gestureSurface) ? _observersOfElement[gestureSurface] : new HashSet<IGestureRecognitionObserver>();
        }

        private static GestureModifier ToGestureModifier(int numberOfTouchInputs)
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

        private static Option<int> GetMostRepresentativeNumberOfTouchPoints(IDictionary<int, int> touchRegistry)
        {
            if (touchRegistry.Any())
            {
                var touchesOrderedByAppearance = touchRegistry.OrderByDescending(pair => pair.Value);
                return Option.Full(touchesOrderedByAppearance.First().Key);
            }
            return Option.Empty();
        }

        private static Gesture ToSwipeGesture(Vector translation, Vector linearVelocity, bool useVelocityForTapDetection, int tapThreshold)
        {
            var deltaX = translation.X;
            var deltaY = translation.Y;
            var distX = Math.Abs(deltaX);
            var distY = Math.Abs(deltaY);
            var isTap = useVelocityForTapDetection
                ? DetectTapFromVelocity(translation, linearVelocity)
                : distX <= tapThreshold && distY <= tapThreshold;

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

    /// <summary>
    /// Observer to be associated with a gesture surface (<see cref="UIElement"/>).
    /// The observer provides the minimum characteristics that will determine which gesture is going to be recognised for itself.
    /// </summary>
    internal interface IGestureRecognitionObserver
    {

        /// <summary>
        /// Use velocity for tap detection, rather than just gesture translation size. Defaults to <code>false</code>.
        /// </summary>
        bool UseVelocityForTapDetection { get; }
        /// <summary>
        /// Set the tap threshold. If swipes are being recognised as taps lowering this value may help.
        /// </summary>
        int TapThreshold { get; }
        /// <summary>
        /// Method in which the observer is going to be notified when a gesture is detected.
        /// </summary>
        /// <param name="gesture">Recognised gesture</param>
        /// <param name="modifier">Recognised modifier. It can be <see cref="Option.Empty"/> if there is not enough information to determine the modifier, usually related to a gentle tap.</param>
        void OnGestureDetected(Gesture gesture, Option<GestureModifier> modifier);
    }
}