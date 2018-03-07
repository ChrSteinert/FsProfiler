# FsProfiler (needs a fancy name (Facer (F# Tracer)))

… let's you track the execution times of definable pieces of code.

## Example

Set up something to profile

```F#
open System.Net
open System.Xml.Linq

let linkCountOnPage (url : string) =
    use dp = new DisposingProfiler "Analyzing site"
    use client = new WebClient()
    let html = 
        use __ = dp.StartSubtask "Downloading"
        client.DownloadString url
    let result =
        use __ = dp.StartSubtask "Parsing HTML"
        let doc = XDocument.Parse html
        let rec getATag (elements : XElement seq) =
            elements
            |> Seq.filter (fun c -> c.Name.LocalName = "a")
            |> Seq.append (
                elements 
                |> Seq.filter (fun c -> c.Name.LocalName <> "a") 
                |> Seq.collect (fun c -> c.Descendants () |> getATag))
        doc.Root.Descendants () |> getATag |> Seq.length
    result 
```

Prepare a sink for the profiler messages

``` F#
open FsProfiler.Stores
let store = new MemoryStore ()
```

Execute the code

```F#
linkCountOnPage "https://bing.com"
```

Use a formatter to see the results.

```F#
open FsProfiler.Formatters
    
store.GetTasks () |> ConsolePrinter.printTasks
```

This will print something like 

```
Analyzing site
    Downloading
    --- 230ms
    Parsing HTML
    --- 460ms
--- 690ms
```