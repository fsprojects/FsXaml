namespace FsXaml

open System.Reflection
open System.Runtime.CompilerServices

module Wpf =
    /// Installs Blend API support
    [<MethodImplAttribute(MethodImplOptions.NoInlining)>]
    let installBlendSupport() =
        // Note that we don't actually have to do anything except guarantee that this assembly gets loaded
        // The simplest way to do that is to force a method call - preventing this from inlining will make sure it gets loaded properly
        // Once this is loaded, the XAML runtime will find the types defined here correctly.  Without this, it's possible to get runtime errors
        // because only loaded assemblies are checked for types.
        ()