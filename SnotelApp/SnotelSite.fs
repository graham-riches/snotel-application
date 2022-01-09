namespace SnotelApplication

open System
open SnotelService.Responses
open SnotelService.Interfaces
open SnotelService.Parameters
open Microsoft.FSharp.Control

module SnotelSite = 
    type SiteData = {
        Temperature: Decimal
        SnowDepth: Decimal
        SWE: Decimal
    }

    type Snotel = {
        Service: ISnotelWebClient
        StationTriplet: string
        MetaData: StationMetadataResponse
        SiteData: SiteData
    }

    type TimeRequest = 
    | Last24Hours
    | LastWeek
    | SeasonTotal

    type DataRequest = {
        Site: Snotel option
        ItemId: int option
        TimeSpan: TimeRequest
    }

    let elementTypeFromId itemId = 
        match itemId with
        | Some 0 -> Some ElementType.AirTemperature
        | Some 1 -> Some ElementType.AirTemperatureMinimum
        | Some 2 -> Some ElementType.AirTemperatureMaximum
        | Some 3 -> Some ElementType.SnowDepth
        | Some 4 -> Some ElementType.SnowWaterEquivalent
        | _ -> None

    let getSeasonStartDate () = 
        let now = DateTime.Now
        match now.Month with
        | m when m < 7 -> DateTime(now.AddYears(-1).Year, 11, 1)
        | _ -> DateTime(now.Year, 11, 1)

    let handleDataRequest request =
        async {
            match request.Site with
            | Some s -> 
                match elementTypeFromId request.ItemId with
                | Some e -> 
                    let! data = 
                        match request.TimeSpan with
                        | Last24Hours -> s.Service.GetHourlyDataAsync(s.StationTriplet, e, DateTime.Now.AddDays(-1)) |> Async.AwaitTask |> Async.map (fun r -> r.GetDataPoints)
                        | LastWeek    -> s.Service.GetHourlyDataAsync(s.StationTriplet, e, DateTime.Now.AddDays(-7)) |> Async.AwaitTask |> Async.map (fun r -> r.GetDataPoints)
                        | SeasonTotal -> s.Service.GetDailyDataAsync(s.StationTriplet, e, getSeasonStartDate ())     |> Async.AwaitTask |> Async.map (fun r -> r.GetDataPoints)                              
                    return Some (data ())
                | None -> return None                  
            | None -> return None
        }                    

    let loadSiteFromStationTriplet stationTriplet = 
        async {
            let service = SnotelService.Service.Client
            let! meta = service.GetStationMetadataAsync stationTriplet |> Async.AwaitTask
            let! temperature = service.GetCurrentDataAsync(stationTriplet, ElementType.AirTemperature) |> Async.AwaitTask
            let! depth = service.GetCurrentDataAsync(stationTriplet, ElementType.SnowDepth) |> Async.AwaitTask
            let! swe = service.GetCurrentDataAsync(stationTriplet, ElementType.SnowWaterEquivalent) |> Async.AwaitTask            
            let data = {Temperature = temperature.Value; SnowDepth = depth.Value; SWE = swe.Value}
            return {
                Service = service
                StationTriplet = stationTriplet
                MetaData = meta
                SiteData = data }
        }
