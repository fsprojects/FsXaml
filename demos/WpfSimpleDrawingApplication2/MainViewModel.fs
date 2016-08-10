namespace WpfSimpleDrawingApplication2

open System
open System.Collections.ObjectModel

type MainViewModel() =
    let points = ObservableCollection<Point>()

    let onMouseLeftDown = RelayObserver (fun p -> points.Add(p))
    
    member this.OnMouseLeftDown = onMouseLeftDown
    // Our lines for visual binding
    member __.Points = points    
