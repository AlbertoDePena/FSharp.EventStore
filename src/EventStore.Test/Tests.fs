module Tests

open System
open Xunit
open EventStore.DomainTypes
open EventStore.DataAccessTypes
open EventStore.Domain
open FsToolkit.ErrorHandling

let isDomainValidationError result =
    match result with
    | Error (DomainError.ValidationError _) -> true
    | _ -> false

let isDomainInvalidVersion result =
    match result with
    | Error (DomainError.InvalidVersion) -> true
    | _ -> false

let isDomainStreamNotFound result =
    match result with
    | Error (DomainError.StreamNotFound) -> true
    | _ -> false

let isDomainDatabaseError result =
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

        Assert.True(isDomainDatabaseError result)
        Assert.False(isDomainInvalidVersion result)
        Assert.False(isDomainStreamNotFound result)
        Assert.False(isDomainValidationError result)
    }

    Async.RunSynchronously computation
