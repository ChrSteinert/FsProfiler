module FsProfiler.Formatters

open Stores

module ConsolePrinter =

    let printTasks tasks = 
        let rec printTask level task =
            printfn "%*s%s" (level * 4) "" task.Name
            task.SubTasks |> List.iter (printTask (level + 1))
            printfn "%*s--- %ims" (level * 4) "" task.DurationInMilliseconds
        tasks |> List.iter (printTask 0)


