namespace ViewModels

open System
open System.Threading
open System.Collections.ObjectModel
open System.Windows
open System.Windows.Input

open FsXaml

[<StructuredFormatDisplay("{X}:{Y}")>]
type Point  = 
    { X: float; Y: float }
    override x.ToString() = sprintf "(%.1f : %.1f)" x.X x.Y
 
type Agent<'T> = MailboxProcessor<'T>
 
type MainViewModel() as me = 
    inherit ViewModelBase()

    let ui = SynchronizationContext.Current

    let executing = me.Factory.Backing(<@ me.Executing @>, false)
    let positions = ObservableCollection<Point>()
    let maxPositions = 20
 
    let agent = Agent.Start(fun inbox ->
               async {
                    while true do
                        let! pt = inbox.Receive()
                        
                        match me.Executing with
                        | false ->
                            // Update our UI
                            do! Async.SwitchToContext ui
                            if positions.Count > maxPositions then positions.RemoveAt 0
                            positions.Add(pt)
                            do! Async.SwitchToThreadPool()
                        | true -> ()
               })

    let clear ui = async { 
            me.Executing <- true            
            while positions.Count > 0 do
                do! Async.Sleep 100
                do! Async.SwitchToContext ui
                positions.RemoveAt(positions.Count - 1)
            
            me.Executing <- false
        }

    let clearCommand = me.Factory.CommandAsync(clear)
    
    member this.MoveAgent = agent
    member this.Positions = positions
    member this.Executing with get() = executing.Value and set(v) = executing.Value <- v
    member this.Clear = clearCommand
