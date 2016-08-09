namespace FsXaml

open System
open System.Collections.Generic
open System.IO
open System.Threading
open System.Windows
open System.Windows.Markup
open System.Windows.Threading
open System.Xaml

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

module InjectXaml =
    let from (file : string) (root : obj) =
        let s = System.IO.Packaging.PackUriHelper.UriSchemePack
        let t = root.GetType()
        let resName = t.Assembly.GetName().Name + ".g.resources"
        use resStream = t.Assembly.GetManifestResourceStream(resName)
        match resStream with
        | null -> failwith "Unable to load XAML data. Verify that all .xaml files are compiled as \"Resource\""
        | _ ->            
            use reader = new System.Resources.ResourceReader(resStream)
            let dataType, data = file.ToLowerInvariant() |> reader.GetResourceData
            use ms = new MemoryStream(data)
            let pos = match dataType with
                        | "ResourceTypeCode.Stream" -> 4L
                        | "ResourceTypeCode.Byte" -> 4L
                        | _ -> 0L
            ms.Position <- pos
            use stream = new StreamReader(ms)
            use reader = new XamlXmlReader(stream, XamlReader.GetWpfSchemaContext())
            
            let writerSettings = XamlObjectWriterSettings()
            writerSettings.RootObjectInstance <- root
            use writer = new XamlObjectWriter(reader.SchemaContext, writerSettings)

            while reader.Read() do
                writer.WriteNode(reader)

            writer.Result
            |> ignore

/// Provides access to named children of a FrameworkElement
type NamedNodeAccessor(root : FrameworkElement) =
    let dict = new Dictionary<_,_>()
        
    /// Gets a named child element by name
    member __.GetChild name = 
        match dict.TryGetValue name with
        | true , element -> element
        | false , _ -> 
            let element = 
                match root.FindName name with            
                | null ->
                    // Fallback to searching the logical tree if our template hasn't been applied
                    LogicalTreeHelper.FindLogicalNode(root, name) :> obj
                | e -> e
            dict.[name] <- element
            element

module Wpf =
    // Gets, and potentially installs, the WPF synchronization context
    let installAndGetSynchronizationContext () =
        match SynchronizationContext.Current with
        | null ->
            // Create our UI sync context, and install it:
            DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher)
            |> SynchronizationContext.SetSynchronizationContext
        | _ -> ()

        SynchronizationContext.Current                        

    let installSynchronizationContext () =
        installAndGetSynchronizationContext() |> ignore

module Reflection = 
    let tryGetMethod (t: Type) methodName =
        let tryGetMethod (t: Type) methodName =
            let mi = t.GetMethod methodName
            if obj.ReferenceEquals(mi, null) then None
            else Some(mi)
        t.GetInterfaces()
        |> Array.tryPick (fun i -> tryGetMethod i methodName)

    let private tryGetAnyMethod source methodName =
        let t = source.GetType()
        let mi = t.GetMethod methodName
        if obj.ReferenceEquals(mi, null) then
            tryGetMethod t methodName
        else Some(mi)

    let invoke source methodName arg = 
        let mi = tryGetAnyMethod source methodName
        match mi with
        | Some mi -> mi.Invoke(source, [| arg |])
        | None -> failwithf "Could not find method: %s"  methodName
