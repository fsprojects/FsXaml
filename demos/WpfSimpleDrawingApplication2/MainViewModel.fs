namespace WpfSimpleDrawingApplication2

open System
open System.Collections.ObjectModel

type MainViewModel() =
    let lines = ObservableCollection<PointPair>()
    let mutable previous = None
    let handleMove p = 
        match previous with 
        | Some p1 -> lines.Add({ Start = p1; End = p } )
        | _ -> ()
        previous <- Some p

    let onMouseMove = RelayObserver handleMove
    
    member this.OnMouseMove = onMouseMove
    // Our lines for visual binding
    member __.Lines = lines    
