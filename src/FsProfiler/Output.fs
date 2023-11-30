module FsProfiler.Output

open Listeners

module TaskPrinter =

  open System.Text

  /// Formats a task to a string.
  let sprint task =
    let builder = StringBuilder ()
    let rec printTask level task =
      Printf.bprintf builder "%*s%s\n" (level * 4) "" task.Name
      task.SubTasks |> List.iter (printTask (level + 1))
      Printf.bprintf builder "%*s--- %ims\n" (level * 4) "" task.DurationInMilliseconds
    task |> printTask 0
    builder.ToString ()

  /// Formats a task to a string and prints that to the console.
  let print task =
    task |> sprint |> printf "%s"

  /// Formats a task to a string and prints it to a string builder.
  let bprint builder task =
    task |> sprint |> Printf.bprintf builder "%s"
  
  /// Formats a task to a string and prints it to a stderr.
  let eprint task =
    task |> sprint |> Printf.eprintf "%s"

  /// Formats a task to a string and prints it to a `System.IO.TextWriter`.
  let fprint writer task =
    task |> sprint |> Printf.fprintf writer "%s"
