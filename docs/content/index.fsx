(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"
#r "FsXaml.Wpf.TypeProvider\FsXaml.Wpf.dll"
#r "FsXaml.Wpf.TypeProvider\FsXaml.Wpf.TypeProvider.dll"
(**
FsXaml
======================

Documentation

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The FsXaml library can be <a href="https://nuget.org/packages/FsXaml.Wpf">installed from NuGet</a>:
      <pre>PM> Install-Package FsXaml.Wpf</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Example
-------

This example demonstrates using the XAML type provider to create a UI.

*)
open FsXaml

type MainWindow = XAML<"MainWindow.xaml">

(**

Samples & documentation
-----------------------

 * [Tutorial](tutorial.html) contains a further explanation of this sample library.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. 
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under the Apache 2.0 license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/FsXaml/tree/master/docs/content
  [gh]: https://github.com/fsprojects/FsXaml
  [issues]: https://github.com/fsprojects/FsXaml/issues
  [readme]: https://github.com/fsprojects/FsXaml/blob/master/README.md
  [license]: https://github.com/fsprojects/FsXaml/blob/master/LICENSE.txt
*)
