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
    member this.StartTask(taskName : string) =
        printfn "Task %s started." taskName
        this.WriteEvent(1, taskName)

    [<Event(2, Message = "Subtask {0} of task {1} started.", Level = EventLevel.LogAlways)>]
    member this.StartSubtask(taskName : string, parentTaskName : string) =
        printfn "Subtask %s of task %s started." taskName parentTaskName
        this.WriteEvent(2, taskName, parentTaskName)

    [<Event(3, Message = "Task {0} took {1} ms.", Level = EventLevel.Informational)>]
    member this.ReportTask(taskName : string, milliseconds : int64) =
        printfn "Task %s took %i ms." taskName milliseconds
        this.WriteEvent(1, taskName, milliseconds)

    [<Event(4, Message = "Subtask {0} of task {1} took {2} ms.", Level = EventLevel.Informational)>]
    member this.ReportSubtask(taskName : string, parentTaskName : string, milliseconds : int64) =
        printfn "Subtask %s of task %s took %i ms." taskName parentTaskName milliseconds
        this.WriteEvent(2, taskName, parentTaskName, milliseconds)

    interface IDisposable with
        member __.Dispose () = log.Dispose ()

type DisposingProfiler private (eventName, parent : DisposingProfiler option) =
    let watch = Stopwatch.StartNew ()

    do
        match parent with
        | Some c -> FsProfilerEvents.Log.StartSubtask(eventName, c.EventName)
        | None -> FsProfilerEvents.Log.StartTask eventName

    new (eventName) = new DisposingProfiler (eventName, None)

    member __.EventName = eventName
    member __.Level = 
        match parent with
        | Some c -> c.Level + 1
        | None -> 0

    member this.StartSubtask eventName =
        new DisposingProfiler(eventName, this |> Some)

    member __.Dispose () =
        watch.Stop ()
        match parent with
        | Some c -> FsProfilerEvents.Log.ReportSubtask(eventName, c.EventName, watch.ElapsedMilliseconds)
        | None -> FsProfilerEvents.Log.ReportTask(eventName, watch.ElapsedMilliseconds)

    interface IDisposable with
        member this.Dispose () =
            this.Dispose ()

type WrappingProfiler<'a> private (eventName, f : unit -> 'a) =

    member private __.Run () =
        let watch = Stopwatch.StartNew ()
        let result = f ()
        watch.Stop ()
        FsProfilerEvents.Log.ReportTask(eventName, watch.ElapsedMilliseconds)
        result

    static member Profile eventName f =
        let profiler = WrappingProfiler(eventName, f)
        profiler.Run ()
