namespace FsXaml

open System
open System.IO
open System.Reflection
open System.Windows
open System.Windows.Markup
open System.Xaml

[<assembly: AssemblyKeyFile(@"..\..\FsXaml.snk")>]
do()

type internal RootNodeType =
    | FrameworkElement
    | ResourceDictionary
    | Application

type internal XamlInfo = { RootType : XamlType ; RootNodeType : RootNodeType ; Members : (string*XamlType) list ; Events : (string*XamlType) list }
    
module internal XamlParser =
    let parseXaml fileName (strm : Stream) =
        use reader = new XamlXmlReader(strm, XamlReader.GetWpfSchemaContext())
        
        try
            let mutable root : (XamlType * RootNodeType) option = None
            let mutable namedMembers : (string*XamlType) list = []
            let mutable eventHandlers : (string*XamlType) list = []

            let rec moveToObject (reader : XamlXmlReader) =
                match reader.NodeType with
                | XamlNodeType.StartObject -> Some reader
                | _ -> 
                    if not(reader.Read()) then
                        None
                    else
                        moveToObject reader

            let rec moveToMember (reader : XamlXmlReader) =
                match reader.NodeType with
                | XamlNodeType.StartMember -> Some reader
                | XamlNodeType.StartObject -> None
                | XamlNodeType.EndObject -> None
                | _ -> 
                    if not(reader.Read()) then
                        None
                    else
                        moveToMember reader

            let mutable currentObject : XamlType = null

            let rec processMember reader rootType =
                let t = moveToMember reader

                match t with
                | None -> 
                    false
                | Some t ->
                    match rootType, t.Member.IsDirective, t.Member.Name, t.Member.IsEvent with
                    | RootNodeType.FrameworkElement, _, "Name", false
                    | RootNodeType.Application, true, "Key", false 
                    | RootNodeType.ResourceDictionary, true, "Key", false -> 
                        if reader.Read() then
                            let v = string reader.Value
                            if not(String.IsNullOrWhiteSpace(v)) then
                                namedMembers <- (v, currentObject) :: namedMembers                    
                    | RootNodeType.FrameworkElement, false, _, true ->
                        let xt = t.Member.Type
                        if reader.Read() then
                            let v = string reader.Value
                            if not(String.IsNullOrWhiteSpace(v)) then
                                eventHandlers <- (v, xt) :: eventHandlers            
                    | _ -> ()

                    reader.Read() |> ignore
                    true

            while (Option.isSome <| moveToObject reader) do    
                currentObject <- reader.Type    

                // Check and set our root element
                match root with 
                | None -> 
                    try             
                        let nodeType = 
                            match currentObject.UnderlyingType with
                            | t when typeof<Application>.IsAssignableFrom(t) -> RootNodeType.Application
                            | t when typeof<ResourceDictionary>.IsAssignableFrom(t) -> RootNodeType.ResourceDictionary
                            | t when typeof<FrameworkElement>.IsAssignableFrom(t) -> RootNodeType.FrameworkElement
                            | _ -> failwith "Unknown"
                        root <- Some (currentObject, nodeType)
                    with 
                    | _ -> ()
                | _ -> ()

                reader.Read() |> ignore
                match root with
                | Some(_,t) ->
                    while processMember reader t do        
                        ()
                | None -> ()
    
            let root = Option.get root
            { RootType = fst root ; RootNodeType = snd root ; Members = namedMembers ; Events = eventHandlers }
        with
        | :? System.Xml.XmlException as xmlE  ->
            let message =
                if reader.HasLineInfo then
                    sprintf "Error parsing XAML contents from %s.\n  Error at line %d, column %d.\n  Element beginning at line %d, column %d." fileName xmlE.LineNumber xmlE.LinePosition reader.LineNumber reader.LinePosition
                else
                    sprintf "Error parsing XAML contents from %s.\n  Error at line %d, column %d." fileName xmlE.LineNumber xmlE.LinePosition

            raise <| XamlException(message,xmlE)