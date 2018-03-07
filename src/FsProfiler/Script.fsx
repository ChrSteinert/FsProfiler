#load "FsProfiler.fs"
#load "Listeners.fs"
#load "Output.fs"
#I @"C:\Users\christiansteinert\Downloads"
#r "Microsoft.Diagnostics.Tracing.TraceEvent.dll"
#r "System.Xml.Linq"


open FsProfiler
open FsProfiler.Listeners
open FsProfiler.Output

let wait (ms : int) = 
    System.Threading.Thread.Sleep ms

let DP () =
    let qwe (fp : DisposingProfiler) = 
        use fp2 = fp.StartSubtask "DP Level 1"
        wait 5
        use __ = fp2.StartSubtask "DP Level 2"
        wait 4
    use fp = new DisposingProfiler "DP Level 0"
    qwe fp    
    wait 20

let WP () =
    let impl () = 
        wait 8
    
    WrappingProfiler.Profile "Test" impl


let tObs = new ObservableTaskListener ()
let asd = tObs.Subscribe (TaskPrinter.print)

DP ()
DP ()
WP ()

asd.Dispose ()
    


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

let store = new MemoryStore ()
linkCountOnPage "https://bing.com"
store.GetTasks () |> Seq.iter TaskPrinter.print