#load "FsProfiler.fs"
#load "Stores.fs"
#load "Formatters.fs"

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


let store = new MemoryStore ()


DP ()
DP ()
WP ()
    
store.GetTasks () |> ConsolePrinter.printTasks