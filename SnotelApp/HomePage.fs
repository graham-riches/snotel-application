namespace SnotelApplication

open System
open Elmish
open Elmish.WPF
open WpfControls

module HomePage = 
    type Model = {
        temp: bool
    }

    type Message = 
    | GetStarted

    type CommandMessage = 
    | GetStartedTriggered

