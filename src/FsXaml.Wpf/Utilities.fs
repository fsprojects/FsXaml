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
            member this.Convert (args : EventArgs) param =
                args :> obj }

        // The intent of this function was to be able to pass a line number and line position
        // to this function and it would return the property name and property value at that 
        // position, given the stream.
        // let internal getXMLObjectAt (ms : MemoryStream) (lineNum : int) (linePos : int) =
//        use streamReader = new StreamReader(ms)
//        streamReader.BaseStream.Position <- 0L
//        for i = 1 to lineNum - 1 do
//            streamReader.ReadLine() |> ignore
//        for i = 1 to linePos - 1 do
//            streamReader.Read() |> ignore
          //TODO: Use regex here instead
//        let xmlProp = streamReader.ReadLine().Split(' ').[0]
//        let xmlVal = xmlProp.Split('"').[1]
//        ( xmlProp, xmlVal ) 