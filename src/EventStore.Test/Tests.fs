module Tests

open System
open Xunit
open EventStore.Extensions
open EventStore.DomainTypes
open EventStore.DataAccessTypes
open EventStore.Domain
open FsToolkit.ErrorHandling

let isValidationErrorResult result =
    match result with
    | Error (DomainError.ValidationError _) -> true
    | _ -> false

let isInvalidVersionResult result =
    match result with
    | Error (DomainError.InvalidVersion) -> true
    | _ -> false

let isStreamNotFoundResult result =
    match result with
    | Error (DomainError.StreamNotFound) -> true
    | _ -> false

let isDatabaseErrorResult result =
    match result with
    | Error (DomainError.DatabaseError _) -> true
    | _ -> false

[<Fact>]
let getAllStreams_with_data_access_error_should_yield_DatabaseError () = // fsharplint:disable-line
    
    let dbCall : GetAllStreams =
        fun () -> async { return (failwith "DB error") }
    
    let computation =  async {
        let! result = Service.getAllStreams dbCall

        Assert.True(isDatabaseErrorResult result)
        Assert.False(isInvalidVersionResult result)
        Assert.False(isStreamNotFoundResult result)
        Assert.False(isValidationErrorResult result)
    }

    Async.AsTask computation

[<Fact>]
let getStream_with_data_access_error_should_yield_DatabaseError () = // fsharplint:disable-line
    
    let dbCall : GetStream =
        fun _ -> async { return (failwith "DB error") }
    
    let computation =  async {
        let query : UnvalidatedStreamQuery = { StreamName =  "Some stream" }
        let! result = Service.getStream dbCall query

        Assert.True(isDatabaseErrorResult result)
        Assert.False(isInvalidVersionResult result)
        Assert.False(isStreamNotFoundResult result)
        Assert.False(isValidationErrorResult result)
    }

    Async.AsTask computation

[<Fact>]
let getStream_without_stream_name_should_yield_ValidationError () = // fsharplint:disable-line
    
    let dbCall : GetStream =
        fun _ -> async { return (failwith "DB error") }
    
    let computation =  async {
        let query : UnvalidatedStreamQuery = { StreamName =  "" }
        let! result = Service.getStream dbCall query

        Assert.False(isDatabaseErrorResult result)
        Assert.False(isInvalidVersionResult result)
        Assert.False(isStreamNotFoundResult result)
        Assert.True(isValidationErrorResult result)
    }

    Async.AsTask computation
