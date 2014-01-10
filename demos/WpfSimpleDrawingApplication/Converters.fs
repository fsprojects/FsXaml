namespace WpfSimpleDrawingApplication

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Markup
open FsXaml

[<StructuredFormatDisplay("{X}:{Y}")>]
type Point  = 
    { X: float; Y: float }
    override x.ToString() = sprintf "(%f : %f)" x.X x.Y

type MouseCaptureState =
    | Captured
    | Released

// The converters to convert from MouseEventArgs and MouseButtonEventArgs -> Point
type MoveConverter() =
    interface IEventArgsConverter with
        member this.Convert (ea : EventArgs) obj =
            let args = ea :?> MouseEventArgs
            let source = args.OriginalSource :?> FrameworkElement
            let pt = args.GetPosition(source)
            { X = pt.X; Y = pt.Y } :> obj

type ButtonCaptureConverter() =
    interface IEventArgsConverter with
        member this.Convert (ea : EventArgs) obj =
            let args = ea :?> MouseButtonEventArgs
            match args.ButtonState with
            | MouseButtonState.Pressed ->
                let source = args.OriginalSource :?> FrameworkElement
                source.CaptureMouse() |> ignore
                let pt = args.GetPosition(source)
                (Captured, { X = pt.X; Y = pt.Y }) :> obj
            | _ ->
                let source = args.OriginalSource :?> FrameworkElement
                source.ReleaseMouseCapture()
                let pt = args.GetPosition(source)
                (Released, { X = pt.X; Y = pt.Y }) :> obj