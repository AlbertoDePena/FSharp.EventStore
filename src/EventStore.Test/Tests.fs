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
let ``Get All Streams with data access error should yield DomainError.DatabaseError`` () =
    
    let dbCall : GetAllStreams =
        fun () -> 
            Error (RepositoryException "DB error") 
            |> Async.singleton
    
    let computation =  async {
        let! result = Service.getAllStreams dbCall

        Assert.True(isDatabaseErrorResult result)
        Assert.False(isInvalidVersionResult result)
        Assert.False(isStreamNotFoundResult result)
        Assert.False(isValidationErrorResult result)
    }

    Async.AsTask computation
