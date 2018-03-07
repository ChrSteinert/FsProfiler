# FsProfiler (needs a fancy name)

… let's you track the execution times of definable pieces of code.

## Create sink for trace events
```F#
open FsProfiler.Stores

let store = new MemoryStore ()
```

## Trace execution times
```F#
open System.Net

let download (url : string) =
    use __ = new DisposingProfiler "Download site"
    use client = new WebClient()
    client.DownloadString url
```

## Show results 
```F#
open FsProfiler.Formatters
    
store.GetTasks () |> ConsolePrinter.printTasks
```