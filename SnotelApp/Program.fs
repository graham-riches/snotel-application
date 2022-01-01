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
        TimeSpan: SnotelSite.TimeRequest
    }

    type MessageType =
      | Select of int option
      | LoadSiteDataSuccess of SnotelSite.Snotel
      | LoadSiteDataFailed of exn
      | LoadMetricsFailed of exn
      | LoadMetricsSuccess of SnotelService.Responses.DataPoint[]
      | SetTimeSpan of SnotelSite.TimeRequest
      | Empty

    type CommandMsg =
        | LoadStationData of string
        | LoadPlotData of SnotelSite.DataRequest

    let zipMap f a b = 
        Seq.zip a b |> Seq.map (fun (x, y) -> f x y)

    let init () = 
        let plotOptions = ["Temperature"; "Temperature Minimum"; "Temperature Maximum"; "Snow Depth"; "SWE"]
        let m = {
            Site = None
            Entities = zipMap (fun id label -> {Id = id; Value = label}) {0..100} plotOptions |> Seq.toList
            Selected = None
            Plot = None
            TimeSpan = SnotelSite.Last24Hours}            
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

    let createNewPlotModel (seriesData: SnotelService.Responses.DataPoint[]) = 
        let plot = PlotModel()
        do
            let series = LineSeries(Color=OxyColors.Black)
            plot.Axes.Add(new DateTimeAxis(StringFormat="MM-dd HH:mm", Position=AxisPosition.Bottom))
            for dataPoint in seriesData do
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Parse dataPoint.Label), float dataPoint.Value))
            plot.Series.Add(series)
        Some plot

    let update msg model =
        match msg with 
        | Select selected -> {model with Selected = selected}, [LoadPlotData {Site = model.Site; ItemId = selected; TimeSpan = model.TimeSpan }]
        | LoadSiteDataSuccess site -> { model with Site = Some site }, []        
        | LoadMetricsSuccess data -> {model with Plot = createNewPlotModel data}, []        
        | SetTimeSpan time -> {model with TimeSpan = time}, [LoadPlotData {Site = model.Site; ItemId = model.Selected; TimeSpan = time}]
        | _ -> model, []

    let toCmd = function
        | LoadStationData site -> Cmd.OfAsync.either loadSiteFromString site id LoadSiteDataFailed
        | LoadPlotData request -> Cmd.OfAsync.either loadDataMetrics request id LoadMetricsFailed

    let bindings () : Binding<Model, MessageType> list = [
        "SiteName"    |> Binding.oneWay (fun m -> if m.Site.IsSome then m.Site.Value.MetaData.Name else "")
        "State"       |> Binding.oneWay (fun m -> if m.Site.IsSome then m.Site.Value.MetaData.State else "")
        "Coordinates" |> Binding.oneWay (fun m -> if m.Site.IsSome then m.Site.Value.MetaData.Location else "")
        "Elevation"   |> Binding.oneWay (fun m -> if m.Site.IsSome then string m.Site.Value.MetaData.Elevation else "")
        "Temperature" |> Binding.oneWay (fun m -> if m.Site.IsSome then string m.Site.Value.SiteData.Temperature else "")
        "SnowDepth"   |> Binding.oneWay (fun m -> if m.Site.IsSome then string m.Site.Value.SiteData.SnowDepth else "")
        "SWE"         |> Binding.oneWay (fun m -> if m.Site.IsSome then string m.Site.Value.SiteData.SWE else "")
        "Plot"        |> Binding.oneWay (fun m -> if m.Plot.IsSome then m.Plot.Value else PlotModel())
        "TimeSpanDay" |> Binding.twoWay (
            (fun m -> match m.TimeSpan with | SnotelSite.Last24Hours -> true | _ -> false), 
            (fun v _ -> SetTimeSpan SnotelSite.Last24Hours))
        "TimeSpanWeek" |> Binding.twoWay (
            (fun m -> match m.TimeSpan with | SnotelSite.LastWeek -> true | _ -> false), 
            (fun v _ -> SetTimeSpan SnotelSite.LastWeek))
        "TimeSpanSeason" |> Binding.twoWay (
            (fun m -> match m.TimeSpan with | SnotelSite.SeasonTotal -> true | _ -> false), 
            (fun v _ -> SetTimeSpan SnotelSite.SeasonTotal))
        "PlotItems" |> Binding.subModelSeq
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