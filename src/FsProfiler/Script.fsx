#load "FsProfiler.fs"
#load "Stores.fs"
#load "Formatters.fs"
#I @"C:\Users\christiansteinert\Downloads"
#r "Microsoft.Diagnostics.Tracing.TraceEvent.dll"


open FsProfiler
open Stores
open Formatters

let wait (ms : int) = 
    System.Threading.Thread.Sleep ms

let DP () =
    let qwe (fp : DisposingProfiler) = 
        use fp2 = fp.StartSubtask "DP Level 1"
        wait 5
        use fp3 = fp2.StartSubtask "DP Level 2"
        wait 4
    use fp = new DisposingProfiler "DP Level 0"
    qwe fp    
    wait 20

let WP () =
    let impl () = 
        wait 8
    
    WrappingProfiler.Profile "Test" impl

open System.Net

let download (url : string) =
    use __ = new DisposingProfiler "Download site"
    use client = new WebClient()
    client.DownloadString url

open FsProfiler.Stores
let store = new MemoryStore ()


DP ()
DP ()
WP ()

open FsProfiler.Formatters
    
store.GetTasks () |> ConsolePrinter.printTasks


open Microsoft.Diagnostics.Tracing.Session

TraceEventSession.GetActiveSessionNames ()
let ses = new TraceEventSession("asd1", TraceEventSessionOptions.Create)
ses.EnableProvider(System.Guid.Parse "25cb5edc-07fc-57ae-8325-0f742b7c59f8")
ses.EnableProvider("FsProfilerEvents")
ses.Source.Dynamic.add_All (fun c -> printfn "%A" c)
let tsk = System.Threading.Tasks.Task.Run (fun () -> ses.Source.Process () )
tsk.Dispose ()
ses.Source.StopProcessing ()
ses.Stop ()
ses.Dispose ()

ses.Source.Dynamic