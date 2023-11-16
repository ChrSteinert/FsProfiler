# FsProfiler

[![NuGet Status](https://img.shields.io/nuget/v/FsProfiler.svg?style=flat)](https://www.nuget.org/packages/FsProfiler/)

 let's you track the execution times of definable pieces of code.

## Examples

### Disposing Profiler

The `DisposingProfiler` tracks the time taken between its instanciation and disposition.
It is super easy to integrate and requires no manual "stopping".

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

Set up a listener for events. 

``` F#
open FsProfiler.Listeners

use tObs = new ObservableTaskListener ()
```

This listener implements `IObservable<Task>`. 
You can easily observe completed tasks by subscribing to that listener. 
The easyest way is to just use one of the task formatters.

```F#
open FsProfiler.Output

let asd = tObs.Subscribe (TaskPrinter.print)
```

Execute the code

```F#
linkCountOnPage "https://github.com/ChrSteinert/FsProfier"
```

As we subscribed to completed events with an printer that just outputs to the console we get the following output:


```
Analyzing site
    Downloading
    --- 230ms
    Parsing HTML
    --- 460ms
--- 690ms
```
