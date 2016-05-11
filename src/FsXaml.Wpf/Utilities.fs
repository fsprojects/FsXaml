namespace FsXaml

open System
open System.IO
open System.Windows

module internal Utilities =
    let internal castAs<'T when 'T : null> (o:obj) = 
        match o with
        | :? 'T as res -> res
        | _ -> null

    let internal downcastAndCreateOption<'T> (o: obj) =
        match o with
        | :? 'T as res -> Some res
        | _ -> None
    
    let defaultEventArgsConverter =
        {new IEventArgsConverter with
            member __.Convert (args : EventArgs) param =
                args :> obj }