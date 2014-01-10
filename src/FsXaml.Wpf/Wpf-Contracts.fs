namespace FsXaml

open System
open System.Windows

[<AllowNullLiteral>]
type public IViewController =
    abstract member Attach : FrameworkElement -> unit

