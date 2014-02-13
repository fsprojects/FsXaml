namespace ViewModels

open System
open System.Threading
open System.Collections.ObjectModel
open System.Windows
open System.Windows.Input

[<StructuredFormatDisplay("{X}:{Y}")>]
type Point  = 
    { X: float; Y: float }
    override x.ToString() = sprintf "(%.1f : %.1f)" x.X x.Y
 
type Agent<'T> = MailboxProcessor<'T>
 
type MainViewModel() = 
    let ui = SynchronizationContext.Current

    let mutable trackPositions = true
    let positions = ObservableCollection<Point>()
    let maxPositions = 20
 
    let agent = Agent.Start(fun inbox ->
               async {
                    while true do
                        let! pt = inbox.Receive()
                        
                        if trackPositions then
                            // Update our UI
                            do! Async.SwitchToContext ui
                            if positions.Count > maxPositions then positions.RemoveAt 0
                            positions.Add(pt)
               })
    
    member this.MoveAgent = agent
    member this.Positions = positions
