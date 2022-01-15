namespace SnotelApplication

open System
open Elmish
open Elmish.WPF
open WpfControls


module App = 
    type Message = 
    | SitePageMessage of SitePage.Message
    | Temp

    type CommandMessage =
    | NavigateToSitePage    

    type Model = {
        MainView: MainView
        SitePageModel: SitePage.Model
    }

    let init () =
        let page, _ = SitePage.init()
        {MainView = MainView(); SitePageModel = page }, []

    let update msg model =
        match msg with 
        | SitePageMessage site -> model, []
        | Temp -> model, []        
        
    let toCmd = function
    | NavigateToSitePage -> []    

    // TODO: Figure out how to specify bindings at a per page level
    let bindings () : Binding<Model, Message> list = [
        // Delegate bindings to the appropriate UI entity        
        "SiteName" |> Binding.subModel (
            (fun m -> m.SitePageModel),
            //(fun (parent, site) -> site),
            SitePageMessage,
            (fun () -> [
            "SiteName" |> Binding.oneWay(fun m -> "Test")]))
        "GetStarted"  |> Binding.cmd(Temp)
        "Frame" |> Binding.oneWay (fun _ -> SitePage())        
        ]


    [<EntryPoint; STAThread>]
    let main _ =
      Program.mkProgramWpfWithCmdMsg init update bindings toCmd
      |> Program.runWindow(MainView())