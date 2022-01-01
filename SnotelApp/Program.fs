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


module Core = 
    type Entity = {
        Id: int
        Value: string
    }

    type Model = {
        Site: SnotelSite.Snotel option
        Entities: Entity list
        Selected: int option    
        Plot: PlotModel option
    }

    type MessageType =
      | Select of int option
      | LoadSiteDataSuccess of SnotelSite.Snotel
      | LoadSiteDataFailed of exn
      | LoadMetricsFailed of exn
      | LoadMetricsSuccess of HourlyDataResponse
      | Empty

    type CommandMsg =
        | LoadStationData of string
        | LoadPlotData of SnotelSite.DataRequest

    let init () = 
        let m = {
            Site = None
            Entities = [{Id = 0; Value = "Temperature"}; {Id = 1; Value = "Snow Depth"}; {Id = 2; Value = "SWE"}]
            Selected = None
            Plot = None}            
        m, [LoadStationData "787:MT:SNTL"]

    let loadSiteFromString site =
        async {
            let! site = SnotelSite.loadSiteFromStationTriplet site
            return LoadSiteDataSuccess site
        }

    let loadDataMetrics request = 
        async {
            let! response = SnotelSite.handleDataRequest request
            match response with
            | Some r -> return LoadMetricsSuccess r
            | None -> return Empty      
        }

    let createNewPlotModel (seriesData: HourlyDataResponse) = 
        let plot = PlotModel()
        do
            let series = LineSeries(Color=OxyColors.Black)
            plot.Axes.Add(new DateTimeAxis(StringFormat="MM-dd HH:mm", Position=AxisPosition.Bottom))
            for dataPoint in seriesData.DataPoints do
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Parse dataPoint.Label), float dataPoint.Value))
            plot.Series.Add(series)
        Some plot

    let update msg model =
        match msg with 
        | Select selected -> model, [LoadPlotData {Site = model.Site; ItemId = selected }]
        | LoadSiteDataSuccess site -> { model with Site = Some site }, []        
        | LoadMetricsSuccess data -> {model with Plot = createNewPlotModel data}, []        
        | _ -> model, []

    let toCmd = function
        | LoadStationData site -> Cmd.OfAsync.either loadSiteFromString site id LoadSiteDataFailed
        | LoadPlotData request -> Cmd.OfAsync.either loadDataMetrics request id LoadMetricsFailed

    let getTextFieldFromOption optValue = 
        match optValue with
        | Some v -> v
        | None -> ""

    let bindings () : Binding<Model, MessageType> list = [
        "SiteName"     |> Binding.oneWay (fun m -> if m.Site.IsSome then m.Site.Value.MetaData.Name else "")
        "State"        |> Binding.oneWay (fun m -> if m.Site.IsSome then m.Site.Value.MetaData.State else "")
        "Coordinates"  |> Binding.oneWay (fun m -> if m.Site.IsSome then m.Site.Value.MetaData.Location else "")
        "Elevation"    |> Binding.oneWay (fun m -> if m.Site.IsSome then string m.Site.Value.MetaData.Elevation else "")
        "Temperature"  |> Binding.oneWay (fun m -> if m.Site.IsSome then string m.Site.Value.SiteData.Temperature else "")
        "SnowDepth"    |> Binding.oneWay (fun m -> if m.Site.IsSome then string m.Site.Value.SiteData.SnowDepth else "")
        "SWE"          |> Binding.oneWay (fun m -> if m.Site.IsSome then string m.Site.Value.SiteData.SWE else "")
        "Plot"         |> Binding.oneWay (fun m -> if m.Plot.IsSome then m.Plot.Value else PlotModel())
        "PlotItems"    |> Binding.subModelSeq
            (fun m -> m.Entities
            , fun e -> e.Id
            , fun () -> [
            "Name" |> Binding.oneWay (fun (_, e) -> e.Value)
            ])
        "ItemSelected" |> Binding.subModelSelectedItem("PlotItems", (fun m -> m.Selected), Select)
    ]

    [<EntryPoint; STAThread>]
    let main _ =
      Program.mkProgramWpfWithCmdMsg init update bindings toCmd
      |> Program.runWindow (MainView())