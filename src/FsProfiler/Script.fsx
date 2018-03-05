#load "FsProfiler.fs"
open FsProfiler

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
