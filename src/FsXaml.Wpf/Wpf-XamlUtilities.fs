namespace FsXaml

open System
open System.IO
open System.Windows
open System.Windows.Controls
open System.Windows.Markup

module LoadXaml =
    let fromUri(uri) =
        let xaml = Application.LoadComponent <| uri
        xaml |> unbox
         
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
                | :? System.Windows.Markup.XamlParseException as ioe -> 
                    // TODO: Replace Contains with line number & position function to display
                    //       XAML property and property value causing the issue.
                    match ioe.Message.Contains("The invocation of the constructor on type") with
                        | true -> failwith "Unable to load XAML data. Verify that all .xaml files are compiled as \"Resources\""
                        | false -> reraise()    
                | _ -> reraise()
                        