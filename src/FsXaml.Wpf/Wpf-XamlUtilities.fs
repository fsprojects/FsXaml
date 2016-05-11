namespace FsXaml

open System
open System.IO
open System.Threading
open System.Windows
open System.Windows.Markup
open System.Windows.Threading
open System.Xaml

module LoadXaml =

    type internal IMarker = interface end
    
    let moduleType = typeof<IMarker>.DeclaringType

    let fromUri(uri) =
        let xaml = Application.LoadComponent <| uri
        xaml |> unbox

    let fromDirect (file : string) (root : obj) =
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
                         
    let from (file : string) (t : Type) =
        let s = System.IO.Packaging.PackUriHelper.UriSchemePack
        let resName = t.Assembly.GetName().Name + ".g.resources"
        use stream = t.Assembly.GetManifestResourceStream(resName)
        match stream with
        | null -> failwith "Unable to load XAML data. Verify that all .xaml files are compiled as \"Resource\""
        | _ ->
            use reader = new System.Resources.ResourceReader(stream)
            let dataType, data = file.ToLowerInvariant() |> reader.GetResourceData
            let ms = new MemoryStream(data)
            let pos = match dataType with
                        | "ResourceTypeCode.Stream" -> 4L
                        | "ResourceTypeCode.Byte" -> 4L
                        | _ -> 0L
            ms.Position <- pos
            try
                System.Windows.Markup.XamlReader.Load(ms) |> unbox
            with
                | ioe when ioe.Message.Contains("The invocation of the constructor on type") ->
                    failwith "Unable to load XAML data. Verify that all .xaml files are compiled as \"Resource\""
                | _ -> reraise()

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