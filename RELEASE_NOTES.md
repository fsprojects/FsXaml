#### 2.0.0 - May 16 2016
* Major update which changes the way code behind is accessed and generated.  
* This provides more efficiently generated types, as well as direct support for code behind via inheritance. 
* Simplifies accessing child controls, and eliminates the view controllers.  
* All blend behaviors have moved to FsXaml.Wpf.Blend, which is now a separate NuGet package

#### 2.1.0 - May 19 2016
* Change in code generation to more closely match C# generation
* Generated types now implement System.Windows.Markup.IComponentConnector
* Generated types include virtual OnInitialize method to simplify wiring up code behind
* Resolved issues with ElementName bindings and logical tree disconnects

#### 2.2.0 - Dec 14 2016
* Improved error messages on parse failures
* Updated Blend Libs to new Expression SDK for .NET 4+
* Added ability to filter converted options in EventToCommand

#### 3.0.0 - Feb 1 2017
* Rewrote XAML parsing to be more in line with runtime loading
* Added initial support for events
* Removed OnInitialized support, as Loaded event can be triggered in XAML instead
* Fixed issue with EventToMailbox

#### 3.1.0 - Feb 2 2017
* Changed event handlers to be abstract, providing better compile time experience
* Improved error messages when XAML is poorly formed





