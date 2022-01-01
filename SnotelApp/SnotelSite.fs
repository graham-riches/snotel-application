namespace SnotelApplication

open System
open SnotelService.Responses
open SnotelService.Interfaces
open SnotelService.Parameters

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

    type DataRequest = {
        Site: Snotel option
        ItemId: int option
    }    

    let handleDataRequest request =
        async {
            match request.Site with
            | Some s -> 
                let! data = s.Service.GetHourlyDataAsync(s.StationTriplet, ElementType.AirTemperature, DateTime.Now.AddDays(-1)) |> Async.AwaitTask
                return Some data
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
