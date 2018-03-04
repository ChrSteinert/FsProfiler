#load "FsProfiler.fs"
open FsProfiler

let asd () =
    use fp = new FsProfiler.FsProfiler "test"
    use fp2 = fp.StartSubtask "test2"
    System.Threading.Thread.Sleep 20

asd ()     
