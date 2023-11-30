module FsProfiler.Output

open Listeners

module TaskPrinter =

  open System.Text

  let sprint task =
    let builder = StringBuilder ()
    let rec printTask level task =
      Printf.bprintf builder "%*s%s\n" (level * 4) "" task.Name
      task.SubTasks |> List.iter (printTask (level + 1))
      Printf.bprintf builder "%*s--- %ims\n" (level * 4) "" task.DurationInMilliseconds
    task |> printTask 0
    builder.ToString ()

  let print task =
    task |> sprint |> printf "%s"

  let bprint builder task =
    task |> sprint |> Printf.bprintf builder "%s"

  let eprint task =
    task |> sprint |> Printf.eprintf "%s"

  let fprint writer task =
    task |> sprint |> Printf.fprintf writer "%s"