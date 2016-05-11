namespace FsXaml

open System.Collections.Generic
open System.Windows

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