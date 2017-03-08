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
    
module InjectXaml =
    [<Literal>]
    let private errorText = "Unable to load XAML data. Verify that all .xaml files are compiled as \"Resource\""

    let private getResourceStream (file : string) (ass : System.Reflection.Assembly) =
        let resName = ass.GetName().Name + ".g.resources"

        use resStream = ass.GetManifestResourceStream(resName)
        match resStream with
        | null -> failwith errorText
        | _ ->            
            use reader = new System.Resources.ResourceReader(resStream)
            let dataType, data = file.ToLowerInvariant() |> reader.GetResourceData
            let pos = match dataType with
                        | "ResourceTypeCode.Stream" -> 4L
                        | "ResourceTypeCode.Byte" -> 4L
                        | _ -> 0L
            
            let ms = new MemoryStream(data)
            ms.Position <- pos
            ms

    let private getEmbeddedResourceStream (file : string) (ass : System.Reflection.Assembly) =
        let resourceName = 
            ass.GetManifestResourceNames()
            |> Array.tryFind (fun n -> n.ToLowerInvariant().Trim() = file.ToLowerInvariant().Trim())

        match resourceName with
        | None -> failwith errorText
        | Some name ->
            use resStream = ass.GetManifestResourceStream(name)
            match resStream with
            | null -> failwith errorText
            | _ ->                                    
                let ms = new MemoryStream()
                resStream.CopyTo(ms)
                ms.Position <- 0L
                ms

    let from (file : string) (root : obj) =
        let s = System.IO.Packaging.PackUriHelper.UriSchemePack
        let t = root.GetType()
        
        use ms = 
            try
                getResourceStream file t.Assembly :> Stream
            with
            | _ ->
                try
                    getEmbeddedResourceStream file t.Assembly :> _
                with
                | _ ->
                    System.IO.File.OpenRead file :> _

        use stream = new StreamReader(ms)
        use reader = new XamlXmlReader(stream, XamlReader.GetWpfSchemaContext())
            
        let writerSettings = XamlObjectWriterSettings()
        writerSettings.RootObjectInstance <- root
        use writer = new XamlObjectWriter(reader.SchemaContext, writerSettings)

        try
            while reader.Read() do                
                writer.WriteNode(reader)
        with 
        | :? XamlException as xamlException -> // From writer
            let message =
                if reader.HasLineInfo then
                    sprintf "Error parsing XAML contents from %s.\n  Error at line %d, column %d.\n  Element beginning at line %d, column %d." file xamlException.LineNumber xamlException.LinePosition reader.LineNumber reader.LinePosition
                else
                    sprintf "Error parsing XAML contents from %s.\n  Error at line %d, column %d." file xamlException.LineNumber xamlException.LinePosition
            raise <| XamlException(message, xamlException)
        | :? System.Xml.XmlException as xmlE  -> // From reader
            let message =
                if reader.HasLineInfo then
                    sprintf "Error parsing XAML contents from %s.\n  Error at line %d, column %d.\n  Element beginning at line %d, column %d." file xmlE.LineNumber xmlE.LinePosition reader.LineNumber reader.LinePosition
                else
                    sprintf "Error parsing XAML contents from %s.\n  Error at line %d, column %d." file xmlE.LineNumber xmlE.LinePosition                        
            raise <| XamlException(message, xmlE)
        writer.Result
        |> ignore

/// Provides access to named children of a FrameworkElement
[<Sealed;AbstractClass>] 
type MemberAccessor () =
    static let mutable weakTable = System.Runtime.CompilerServices.ConditionalWeakTable<FrameworkElement,Dictionary<string,obj>>()
        
    /// Gets a named child element by name
    static member GetNamedMember<'a> (root : FrameworkElement) name : 'a = 
        let dict = weakTable.GetOrCreateValue root

        lock dict (fun _ ->
            match dict.TryGetValue name with
            | true , element -> element
            | false , _ -> 
                let element = 
                    match root.FindName (name) with            
                    | null ->
                        // Fallback to searching the logical tree if our template hasn't been applied
                        LogicalTreeHelper.FindLogicalNode(root, name) :> obj
                    | e -> e
                dict.[name] <- element
                element)
        |> unbox

    /// Provides access to keyed children of a ResourceDictionary
    static member GetResourceByKey<'a> (root : ResourceDictionary) (name : string) : 'a = root.[name] |> unbox

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

module DesignMode =
    let dependencyObject = DependencyObject()

    let InDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode dependencyObject

    let failIfDesignModef format message =
        if InDesignMode then
            failwithf format message

    let failIfDesignMode message = failIfDesignModef "%s" message

    let failIfNotDesignMode message =
        if not InDesignMode then
            failwith message
