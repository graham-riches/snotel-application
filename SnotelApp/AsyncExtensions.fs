namespace Microsoft.FSharp.Control

module Async =
    let map f op = async {
        let! x    = op
        let value = f x
        return value
    }