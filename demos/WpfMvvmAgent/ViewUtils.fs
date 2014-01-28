namespace Views

open FsXaml
open ViewModels

open System
open System.Windows
open System.Windows.Input


type MouseToPointConverter() =
    interface IEventArgsConverter with
        member this.Convert (ea : EventArgs) obj =
            let args = ea :?> MouseEventArgs
            let source = args.OriginalSource :?> FrameworkElement
            let pt = args.GetPosition(source)
            { X = pt.X; Y = pt.Y } :> obj


