namespace FsProfiler

open System
open System.Diagnostics
open System.Diagnostics.Tracing

[<EventSource>]
type FsProfilerEvents private () =
    inherit EventSource ()

    static let log = new FsProfilerEvents ()

    static member Log = log

    [<Event(1, Message = "Task {0} started.", Level = EventLevel.LogAlways)>]
    member this.TaskStart(taskName : string, taskId : Guid) =
        #if DEBUG
        printfn "Task %s started." taskName
        #endif
        this.WriteEvent(1, taskName, taskId)

    [<Event(2, Message = "Subtask {0} of task {1} started.", Level = EventLevel.LogAlways)>]
    member this.SubtaskStart(taskName : string, parentTaskName : string, taskId : Guid, parentTaskId : Guid) =
        #if DEBUG
        printfn "Subtask %s of task %s started." taskName parentTaskName
        #endif
        this.WriteEvent(2, taskName, parentTaskName, taskId, parentTaskId)

    [<Event(3, Message = "Task {0} took {1} ms.", Level = EventLevel.Informational)>]
    member this.TaskStop(taskName : string, milliseconds : int64, taskId : Guid) =
        #if DEBUG
        printfn "Task %s took %i ms." taskName milliseconds
        #endif
        this.WriteEvent(3, taskName, milliseconds, taskId)

    [<Event(4, Message = "Subtask {0} of task {1} took {2} ms.", Level = EventLevel.Informational)>]
    member this.SubtaskStop(taskName : string, parentTaskName : string, milliseconds : int64, taskId : Guid, parentTaskId : Guid) =
        #if DEBUG
        printfn "Subtask %s of task %s took %i ms." taskName parentTaskName milliseconds
        #endif
        this.WriteEvent(4, taskName, parentTaskName, milliseconds, taskId, parentTaskId)

    interface IDisposable with
        member __.Dispose () = log.Dispose ()

type DisposingProfiler private (eventName, parent : DisposingProfiler option) =
    let watch = Stopwatch.StartNew ()
    let taskId = Guid.NewGuid ()

    do
        match parent with
        | Some c -> FsProfilerEvents.Log.SubtaskStart(eventName, c.EventName, taskId, c.TaskId)
        | None -> FsProfilerEvents.Log.TaskStart(eventName, taskId)

    new (eventName) = new DisposingProfiler (eventName, None)

    member __.EventName = eventName
    member __.TaskId = taskId

    member this.StartSubtask eventName =
        new DisposingProfiler(eventName, this |> Some)

    member __.Dispose () =
        watch.Stop ()
        match parent with
        | Some c -> FsProfilerEvents.Log.SubtaskStop(eventName, c.EventName, watch.ElapsedMilliseconds, taskId, c.TaskId)
        | None -> FsProfilerEvents.Log.TaskStop(eventName, watch.ElapsedMilliseconds, taskId)

    interface IDisposable with
        member this.Dispose () =
            this.Dispose ()

type WrappingProfiler<'a> private (eventName, f : unit -> 'a) =

    let taskId = Guid.NewGuid ()

    member private __.Run () =
        FsProfilerEvents.Log.TaskStart(eventName, taskId)
        let watch = Stopwatch.StartNew ()
        let result = f ()
        watch.Stop ()
        FsProfilerEvents.Log.TaskStop(eventName, watch.ElapsedMilliseconds, taskId)
        result

    static member Profile eventName f =
        let profiler = WrappingProfiler(eventName, f)
        profiler.Run ()
