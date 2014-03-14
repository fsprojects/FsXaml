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
        use reader = new System.Resources.ResourceReader(stream)
        let dataType, data = file.ToLowerInvariant() |> reader.GetResourceData
        let ms = new MemoryStream(data)
        let pos = match dataType with
                    | "ResourceTypeCode.Stream" -> 4L
                    | "ResourceTypeCode.Byte" -> 4L
                    | _ -> 0L
        ms.Position <- pos
        System.Windows.Markup.XamlReader.Load(ms) |> unbox        