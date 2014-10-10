namespace FsXaml

open System
open System.Windows

[<AllowNullLiteral>]
type public IViewController =
    abstract member Initialized : FrameworkElement -> unit
    abstract member Loaded : FrameworkElement -> unit
    abstract member Unloaded : FrameworkElement -> unit

