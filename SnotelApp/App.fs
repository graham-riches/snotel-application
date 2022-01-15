namespace SnotelApplication

open System
open Elmish
open Elmish.WPF
open WpfControls
open SnotelService.Responses
open SnotelService.Parameters
open OxyPlot
open OxyPlot.Series
open OxyPlot.Axes


module App = 
    type Message = 
    | SitePageMessage of SitePage.Message

    type CommandMessage =
    | NavigateToSitePage

    type Model = {
        SitePageModel: SitePage.Model
    }

    let init () =
        let page, _ = SitePage.init()
        {SitePageModel = page }, []

    let update msg model =
        match msg with 
        | SitePageMessage site -> model, []
        
    let toCmd = function
    | NavigateToSitePage -> []

    let bindings () : Binding<Model, Message> list = [
        "SiteName"    |> Binding.oneWay (fun m -> "")
        "State"       |> Binding.oneWay (fun m -> "")
        "Coordinates" |> Binding.oneWay (fun m -> "")
        "Elevation"   |> Binding.oneWay (fun m -> "")
        "Temperature" |> Binding.oneWay (fun m -> "")
        "SnowDepth"   |> Binding.oneWay (fun m -> "")
        "SWE"         |> Binding.oneWay (fun m -> "")
        "Plot"        |> Binding.oneWay (fun m -> PlotModel())
        ]


    [<EntryPoint; STAThread>]
    let main _ =
      Program.mkProgramWpfWithCmdMsg init update bindings toCmd
      |> Program.runWindow (MainView())