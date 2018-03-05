open System
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


type TraceEvent =
| TaskStart of Guid * string
| SubtaskStart of Guid * parent : Guid * string
| TaskStop of Guid * int64
| SubtaskStop of Guid * parent : Guid * int64

type TracePrinter () as this =
    inherit EventListener ()

    do
        this.EnableEvents(FsProfilerEvents.Log, EventLevel.LogAlways)        

    let queue = new System.Collections.Concurrent.ConcurrentQueue<TraceEvent> ()

    member __.Dump () =
        queue
        |> Seq.iter (fun c -> printfn "%A" c)

    override __.OnEventWritten args =
        if args.EventSource.Name = "FsProfilerEvents" then
            match args.EventId with
            | 1 -> TaskStart(args.Payload.[1] :?> Guid, args.Payload.[0] :?> string)
            | 2 -> SubtaskStart(args.Payload.[2] :?> Guid, args.Payload.[3] :?> Guid, args.Payload.[0] :?> string)
            | 3 -> TaskStop(args.Payload.[2] :?> Guid, args.Payload.[1] :?> int64)
            | 4 -> SubtaskStop(args.Payload.[3] :?> Guid, args.Payload.[4] :?> Guid, args.Payload.[2] :?> int64)
            | _ -> failwith "Unkown event"

            |> queue.Enqueue


let tp = new TracePrinter ()
tp.Dump ()