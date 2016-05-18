(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"
#r "FsXaml.Wpf.TypeProvider\FsXaml.Wpf.dll"
#r "FsXaml.Wpf.TypeProvider\FsXaml.Wpf.TypeProvider.dll"

(**
FsXaml
========================

The FsXaml for WPF library and type provider allows usage of XAML for WPF projects directly from F#. It eliminates the need
to create C# projects when writing WPF Applications or libraries.

This is done via a XAML type provider:

*)

open FsXaml

type MainWindow = XAML<"MainWindow.xaml">

(**

Usage and instructions to come shortly.

*)
