### 1.0.4 (Release October 2015)
* Fixed a readme typo in which I typed "what to contribute" as opposed to "want to contribute". (sigh...)
* Included multi-touch handling (up to 5 points) when recognising swipes and taps. By default all gestures will trigger 
  for a single touch point but it can be expanded by using the GestureModifier dependency property.
* Re-wrote GestureRecognizer to delegate event subscription and surface management to an unique instance.
* Used Delorean to travel to October 26, 1985 to tell Doc we don't have hoverboards in 2015.

### 1.0.3 (Released May 2015)
* Change package dependency to an internal dependency to include latest binaries of Microsoft Expression SDK for .net framework 4.6

### 1.0.2 (Released May 2015)
* Expose TapThreshold property to allow tweaking of tap detection.
* Add option to detect taps using mix of translation and velocity.
  Based on analysis performed by Ryan Melman.
* Added ReadMe file to repository with an example.

### 1.0.1 (Released May 2015)

* Fixed nuget packaging issue that didn't add a reference to this library.

### 1.0.0 (Released May 2015)

Initial release of WPF Gesture Recognizer (wrapper of the manipulation events of FrameworkElement exposing Swipe Up, Down, Left and Right, as well as simple Tap) includes:

* GestureRecognizer trigger applied to a FrameworkElement to be used with System.Windows.Interactivity provided by Microsoft.

> It does not respond to mouse interactions. Is meant to be used on touch devices.
