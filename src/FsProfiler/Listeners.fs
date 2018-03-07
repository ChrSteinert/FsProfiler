module FsProfiler.Listeners

open System
open System.Collections.Concurrent
open System.Diagnostics.Tracing

open FsProfiler

type private RunningTask = Guid option * string

type Task =
    {
        Id : Guid
        Name : string
        SubTasks : Task list
        DurationInMilliseconds : int64
    }

type ObservableTaskListener () as this =
    inherit EventListener ()

    do
        this.EnableEvents(FsProfilerEvents.Log, EventLevel.LogAlways)        

    let subscribers = ResizeArray<IObserver<Task>> ()
    let runningTasks = ConcurrentDictionary<Guid, RunningTask> ()
    let finishedSubtasks = ConcurrentDictionary<Guid, Task list> ()

    override __.OnEventWritten args =
        let getSubtasks key = 
            match finishedSubtasks.TryGetValue key with
            | (true, c) -> c |> List.rev
            | (false, _) -> []

        if args.EventSource.Name = "FsProfilerEvents" then
            match args.EventId with
            | 1 -> 
                let key = args.Payload.[1] :?> Guid
                runningTasks.AddOrUpdate(key, (None, args.Payload.[0] :?> string), fun _ _ -> failwith "Nope!")
                |> ignore
            | 2 -> 
                let key = args.Payload.[2] :?> Guid
                runningTasks.AddOrUpdate(key, (args.Payload.[3] :?> Guid |> Some, args.Payload.[0] :?> string), fun _ _ -> failwith "Nope!")
                |> ignore
            | 3 -> 
                let key = args.Payload.[2] :?> Guid
                let start = runningTasks.[key]
                let timing = args.Payload.[1] :?> int64
                { Id = key; Name = start |> snd; DurationInMilliseconds = timing; SubTasks = key |> getSubtasks  }
                |> (fun c -> 
                    subscribers |> Seq.iter (fun d -> d.OnNext c))
            | 4 -> 
                let key = args.Payload.[3] :?> Guid
                let parent = args.Payload.[4] :?> Guid
                let timing = args.Payload.[2] :?> int64
                let start = runningTasks.[key]
                let subtask = { Id = key; Name = start |> snd; DurationInMilliseconds = timing; SubTasks = key |> getSubtasks }
                finishedSubtasks.AddOrUpdate(parent, [ subtask ], fun _ value -> subtask :: value) |> ignore
            | _ -> ()

    override this.Dispose () =
        this.DisableEvents(FsProfilerEvents.Log)
        subscribers.Clear ()
        base.Dispose ()

    interface IObservable<Task> with
        member __.Subscribe observer : IDisposable = 
            subscribers.Add observer
            {
                new IDisposable with
                    member __.Dispose () = 
                        subscribers.Remove observer |> ignore
            }


[<Obsolete>]
type private TraceEvent =
| TaskStart of Guid * Guid option * string
| TaskStop of Guid * Guid option * int64

[<Obsolete("It has been nice - but the TaskObservable is way cooler!")>]
type MemoryStore () as this =
    inherit EventListener ()

    do
        this.EnableEvents(FsProfilerEvents.Log, EventLevel.LogAlways)        

    let queue = new ConcurrentQueue<TraceEvent> ()

    member __.GetTasks () = 
        let tasks = queue |> Seq.toList
        let rec mapTask parent c =
            match c with
            | TaskStart (id, p, name) when p = parent -> 
                let timing = tasks |> List.pick (fun c -> 
                    match c with 
                    | TaskStop (cid, p, time) when id = cid && p = parent -> time |> Some
                    | _ -> None)
                { 
                    Id = id
                    Name = name
                    SubTasks = tasks |> List.choose (mapTask (id |> Some))
                    DurationInMilliseconds = timing 
                } |> Some
            | _ -> None

        tasks
        |> List.map (mapTask None)
        |> List.choose id

    override __.OnEventWritten args =
        if args.EventSource.Name = "FsProfilerEvents" then
            match args.EventId with
            | 1 -> TaskStart(args.Payload.[1] :?> Guid, None, args.Payload.[0] :?> string)
            | 2 -> TaskStart(args.Payload.[2] :?> Guid, args.Payload.[3] :?> Guid |> Some, args.Payload.[0] :?> string)
            | 3 -> TaskStop(args.Payload.[2] :?> Guid, None, args.Payload.[1] :?> int64)
            | 4 -> TaskStop(args.Payload.[3] :?> Guid, args.Payload.[4] :?> Guid |> Some, args.Payload.[2] :?> int64)
            | _ -> failwith "Unkown event"

            |> queue.Enqueue

