## What is WPF Gesture Recognizer?

Windows Tablets are slowly taking a market and some software developers feel the need to move their applications to a modern UI, similar to those offered by WinRT.

Sadly at the moment, there is no gesture recognition built in for Windows Presentation Foundation applications, and there are applications bound to Windows Desktop Mode. For them, needing to perform Swipes on a tablet on a desktop application there was no support although .net Framework exposes some events that can be used to implement said gestures.

This library wraps those events so you don't have to go through the same process we had to do so implement something as basic as a gesture for a touch device.

## Code Example

Grab [WPFGestureRecognizer from NuGet](https://www.nuget.org/packages/WPFGestureRecognizer/) and add it to your references in your project (On Visual Studio is just add a nuget package to your WPF application).

Then on your xaml page you should add a recognition area. It can be any [UI element](https://msdn.microsoft.com/en-us/library/system.windows.frameworkelement%28v=vs.110%29.aspx) such as a Border or a Grid. Add [`System.Widnows.Interactivity`](https://msdn.microsoft.com/en-us/library/system.windows.interactivity\(v=expression.45\).aspx) and `Org.Interactivity.Recognizer` and off you go.

#### Simple example To start with:
```xml
<Window
  xmlns:i="http://schemas.microsoft.com/expression/2010/interactivity"
  xmlns:ei="http://schemas.microsoft.com/expression/2010/interactions" 
  xmlns:r="clr-namespace:Org.Interactivity.Recognizer;assembly=Org.Interactivity.Recognizer"
  >
  <Grid>
     <i:Interaction.Triggers>
      <r:GestureRecognizer TriggerOnGesture="SwipeDown">
        <ei:ChangePropertyAction PropertyName="Background" Value="DarkBlue" />
      </r:GestureRecognizer>
     </i:Interaction.Triggers>
  </Grid>
</Window>
```

## Contributors

**Want to contribute? Or add more gestures like rotation, skew, and/or zoom?**

Submit your pull request!

**Want a specific gesture?**

Create an issue to be able to track it.


## License
Distributed under Apache License v2.0