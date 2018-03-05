#load "FsProfiler.fs"
open FsProfiler

open System.Diagnostics.Tracing

let wait (ms : int) = 
    System.Threading.Thread.Sleep ms

let DP () =
    let qwe (fp : DisposingProfiler) = 
        use fp2 = fp.StartSubtask "test2"
        wait 5
    use fp = new DisposingProfiler "test"
    qwe fp    
    wait 20

let WP () =
    let impl () = 
        wait 8
    
    WrappingProfiler.Profile "Test" impl


DP ()     


WP ()



type TracePrinter () =
    inherit EventListener ()

    let queue = new System.Collections.Concurrent.ConcurrentQueue<EventWrittenEventArgs> ()

    member __.Dump () =
        queue
        |> Seq.iter (fun c -> printfn "%A" c.Message)

    override __.OnEventWritten args =
        queue.Enqueue args


let tp = new TracePrinter ()
tp.EnableEvents(FsProfilerEvents.Log, EventLevel.LogAlways)        
