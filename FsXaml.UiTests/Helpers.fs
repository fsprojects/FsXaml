module FsXaml.UiTests.StartInfo

open System
open System.Diagnostics
open System.IO
open System.Reflection

let create (name : string) = 
    let testAssemblyPath = Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath
    let fileName = 
        if testAssemblyPath.Contains("Debug") then name.Replace("Release", "Debug")
        else name
    let file = FileInfo fileName
    ProcessStartInfo(file.FullName)
