namespace ViewModels

open System
open System.Windows
open System.Windows.Input

open FsXaml

open FSharp.ViewModule.Core
open FSharp.ViewModule.Core.ViewModel
open FSharp.ViewModule.Core.Validation


type MainViewModel() as self = 
    inherit ViewModelBase()

    // Using validation with default naming
    let firstName = self.Factory.Backing(<@ self.FirstName @>, "", notNullOrWhitespace >> noSpaces >> notEqual "Reed")

    // Using validation with custom name
    let validateName = 
        validate "Last Name" 
        >> notNullOrWhitespace 
        >> hasLengthAtLeast 3 
        >> noSpaces 
        >> result
    let lastName = self.Factory.Backing(<@ self.LastName @>, "", validateName)

    let hasValue str = not(System.String.IsNullOrWhiteSpace(str))
    let okCommand = 
        self.Factory.CommandSyncCheckedParam(
            (fun param -> MessageBox.Show(sprintf "Hello, %s" param) |> ignore), 
            (fun param -> not(self.HasErrors) && hasValue self.FirstName && hasValue self.LastName), 
            [ <@ self.FirstName @> ; <@ self.LastName @> ])   // Or could be: [ <@ self.FullName @> ])

    do
        // Add in property dependencies
        self.DependencyTracker.AddPropertyDependencies(<@@ self.FullName @@>, [ <@@ self.FirstName @@> ; <@@ self.LastName @@> ])
    
    member x.FirstName with get() = firstName.Value and set value = firstName.Value <- value
    member x.LastName with get() = lastName.Value and set value = lastName.Value <- value
    member x.FullName with get() = x.FirstName + " " + x.LastName 

    member x.OkCommand = okCommand

    override x.Validate propertyName =
        match propertyName with
        | "FullName" ->
            seq {
                let err = 
                    match x.FullName with
                    | "Reed Copsey" -> Some ["That is a poor choice of names"]
                    | _ -> None
                yield PropertyValidation(propertyName, "EntityError", err)
            }
        | _ -> Seq.empty