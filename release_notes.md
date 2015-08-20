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