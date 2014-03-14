namespace FsXaml

open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Markup


/// Provides access to named children of a FrameworkElement
type XamlFileAccessor(root : FrameworkElement) =
    let dict = new Dictionary<_,_>()
        
    /// Creates an accessor directly from a resource location, loading the resource    
    new(resourceLocation: string) as self =
        let root : FrameworkElement = LoadXaml.from resourceLocation (self.GetType())
        XamlFileAccessor(root)    

    /// Gets a named child element by name
    member this.GetChild name = 
        match dict.TryGetValue name with
        | true,element -> element
        | false,element -> 
            let element = root.FindName name
            dict.[name] <- element
            element

    /// The root element of the XAML document
    member this.Root = root

/// Provides access to keyed children of a ResourceDictionary
type XamlResourceAccessor(root : ResourceDictionary) =
    let dict = new Dictionary<_,_>()
    
    /// Creates an accessor directly from a resource location, loading the resource    
    new(resourceLocation: string) as self =
        let root : ResourceDictionary = LoadXaml.from resourceLocation (self.GetType())
        XamlResourceAccessor(root)    

    /// Gets a resource by name
    member this.GetResource name = 
        match dict.TryGetValue name with
        | true,element -> element
        | false,element -> 
            let element = root.[name]
            dict.[name] <- element
            element

    /// The root element of the XAML document
    member this.Root = root

/// Creates the root element of a XAML type defined within a resource file location
type XamlTypeFactory<'T>(resourceFile: string) =
    /// Creates the root element
    member self.CreateRoot() : 'T = LoadXaml.from resourceFile (self.GetType())

/// Creates a container for a given XAML element (UserControl or similar) defined within a resource file location
type XamlContainer(resourceFile: string) as self =
    inherit System.Windows.Controls.ContentControl()
    do self.Content <- LoadXaml.from resourceFile (self.GetType())
