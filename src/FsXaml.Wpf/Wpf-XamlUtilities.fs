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

    let from(file) =
        let s = System.IO.Packaging.PackUriHelper.UriSchemePack
        Uri(file, UriKind.RelativeOrAbsolute) |> fromUri
        