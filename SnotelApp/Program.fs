open System
open Elmish
open Elmish.WPF
open WpfControls
open SnotelService.Interfaces
open SnotelService.Responses
open SnotelService.Parameters
open OxyPlot
open OxyPlot.Series

type SiteData = {
    Temperature: Decimal
    SnowDepth: Decimal
    SWE: Decimal
}

type Entity = {
    Id: int
    Value: string
}

type Model = {
    Entities: Entity list
    Selected: int option
    Service: ISnotelWebClient
    StationMetadata: StationMetadataResponse option
    SiteData: SiteData option
    Plot: PlotModel option
}

type MessageType =
  | Refresh
  | Select of int option
  | LoadFailed of exn
  | LoadSuccess of HourlyDataResponse


type PlotDataRequest = {
    Model: Model
    ItemId: int option
}

type CommandMsg =
    | LoadPlotData of PlotDataRequest

let runAsync task = 
    task
    |> Async.AwaitTask
    |> Async.RunSynchronously

let init() = 
    let m = { 
        Entities = [{Id = 0; Value = "Temperature"}; {Id = 1; Value = "Snow Depth"}; {Id = 2; Value = "SWE"}]
        Selected = None
        Service = SnotelService.Service.Client
        StationMetadata = None
        SiteData = None
        Plot = None}            
    let meta = m.Service.GetStationMetadataAsync "787:MT:SNTL" |> runAsync
    let temperature = m.Service.GetCurrentDataAsync("787:MT:SNTL", ElementType.AirTemperature) |> runAsync
    let depth = m.Service.GetCurrentDataAsync("787:MT:SNTL", ElementType.SnowDepth) |> runAsync
    let swe = m.Service.GetCurrentDataAsync("787:MT:SNTL", ElementType.SnowWaterEquivalent) |> runAsync
    let data = {Temperature = temperature.Value; SnowDepth = depth.Value; SWE = swe.Value}    
    {m with StationMetadata = Some meta; SiteData = Some data}, []

let load request =
    async {
        let! response = request.Model.Service.GetHourlyDataAsync("787:MT:SNTL", ElementType.AirTemperature, DateTime.Now) |> Async.AwaitTask
        return LoadSuccess response
    }

let createNewPlotModel seriesData = 
    let plot = PlotModel()
    do
        let series = LineSeries()
        series.Points.Add(new DataPoint(0, 0))
        series.Points.Add(new DataPoint(1, 1))
        plot.Series.Add(series)
    Some plot

let update msg model =
    match msg with 
    | Refresh -> model, []
    | Select selected -> model, [LoadPlotData {Model = model; ItemId = selected }]
    | LoadFailed e -> model, []
    | LoadSuccess data -> {model with Plot = createNewPlotModel data}, []

let toCmd = function
   | LoadPlotData request -> Cmd.OfAsync.either load request id LoadFailed

let bindings () : Binding<Model, MessageType> list = [
    "SiteName"     |> Binding.oneWay (fun m -> if m.StationMetadata.IsSome then m.StationMetadata.Value.Name else "")
    "State"        |> Binding.oneWay (fun m -> if m.StationMetadata.IsSome then m.StationMetadata.Value.State else "")
    "Coordinates"  |> Binding.oneWay (fun m -> if m.StationMetadata.IsSome then m.StationMetadata.Value.Location else "")
    "Elevation"    |> Binding.oneWay (fun m -> if m.StationMetadata.IsSome then string m.StationMetadata.Value.Elevation else "")
    "Temperature"  |> Binding.oneWay (fun m -> if m.SiteData.IsSome then string m.SiteData.Value.Temperature else "")
    "SnowDepth"    |> Binding.oneWay (fun m -> if m.SiteData.IsSome then string m.SiteData.Value.SnowDepth else "")
    "SWE"          |> Binding.oneWay (fun m -> if m.SiteData.IsSome then string m.SiteData.Value.SWE else "")
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