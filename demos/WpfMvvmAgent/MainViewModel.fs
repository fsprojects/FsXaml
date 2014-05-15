namespace ViewModels

open System
open System.Threading
open System.Collections.ObjectModel
open System.Windows
open System.Windows.Input

open FsXaml

open FSharp.ViewModule.Core.ViewModel

[<StructuredFormatDisplay("{X}:{Y}")>]
type Point  = 
    { X: float; Y: float }
    override x.ToString() = sprintf "(%.1f : %.1f)" x.X x.Y
 
type MainViewModel() as me = 
    inherit ViewModelBase()

    let ui = SynchronizationContext.Current

    let trackPositions = me.Factory.Backing(<@ me.TrackPositions @>, true)
    let positions = ObservableCollection<Point>()
    let maxPositions = 20
 
    let agent = MailboxProcessor.Start(fun inbox ->
               async {
                    while true do
                        let! pt = inbox.Receive()

                        if me.TrackPositions then
                            // Update our UI
                            do! Async.SwitchToContext ui
                            if positions.Count > maxPositions then positions.RemoveAt 0
                            positions.Add(pt)
               })

    let clear ui = async {
            me.TrackPositions <- false
            while positions.Count > 0 do
                do! Async.Sleep 100
                do! Async.SwitchToContext ui
                positions.RemoveAt(positions.Count - 1)
            
            me.TrackPositions <- true
        }

    let clearCommand = me.Factory.CommandAsync(clear)
    
    member this.MoveAgent = agent
    member this.Positions = positions
    member this.TrackPositions with get() = trackPositions.Value and set(v) = trackPositions.Value <- v
    member this.Clear = clearCommand
