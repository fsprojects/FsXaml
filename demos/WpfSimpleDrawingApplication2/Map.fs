namespace WpfSimpleDrawingApplication2

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Markup
open FsXaml

// This would typically be defined in a separate project/namespace along with the XAML, as it's "pure view layer" logic
// Converters are used to map from View events -> our F# defined types in the ViewModel or Model layer
module Map =
    let mouseEventArgsToPoint (args : MouseEventArgs) =
        let source = args.OriginalSource :?> IInputElement
        let pt = args.GetPosition(source)
        { X = pt.X; Y = pt.Y }
    let MouseEventArgsToPoint = mouseEventArgsToPoint
