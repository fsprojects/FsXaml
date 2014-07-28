namespace WpfSimpleDrawingApplication

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Markup
open FsXaml

// This would typically be defined in a separate project/namespace along with the XAML, as it's "pure view layer" logic
// Converters are used to map from View events -> our F# defined types in the ViewModel or Model layer
module internal MouseConverters =
    let moveConverter (args : MouseEventArgs) =
        let source = args.OriginalSource :?> IInputElement
        let captured = if System.Object.ReferenceEquals(Mouse.Captured, source) then CaptureStatus.Captured else CaptureStatus.Released
        let pt = args.GetPosition(source)
        PositionChanged(captured, { X = pt.X; Y = pt.Y })

    let captureConverter (args : MouseButtonEventArgs) =
            match args.ButtonState with
            | MouseButtonState.Pressed ->
                let source = args.OriginalSource :?> UIElement
                source.CaptureMouse() |> ignore
                CaptureChanged(Captured)
            | _ ->
                let source = args.OriginalSource :?> UIElement
                source.ReleaseMouseCapture()
                CaptureChanged(Released)

    let Default = CaptureChanged(Released)

// The converters to convert from MouseEventArgs and MouseButtonEventArgs -> MoveEvent
type MoveConverter() =
    inherit EventArgsConverter<MouseEventArgs,MoveEvent>(MouseConverters.moveConverter, MouseConverters.Default)

type ButtonCaptureConverter() =
    inherit EventArgsConverter<MouseButtonEventArgs,MoveEvent>(MouseConverters.captureConverter, MouseConverters.Default)
