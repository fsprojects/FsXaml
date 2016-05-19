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


