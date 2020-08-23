namespace EventStore.Domain

open System
open System.Data
open EventStore.Extensions
open EventStore.DataAccess
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module Service =

    let private validateStreamName =
        Validation.validateStreamName
        >> Result.mapError DomainError.ValidationError
        >> Async.singleton

    let private validateVersion =
        Validation.validateVersion
        >> Result.mapError DomainError.ValidationError
        >> Async.singleton        
   
    let getAllStreams (getAllStreams : GetAllStreams) = 
        getAllStreams ()
        |> AsyncResult.mapError DomainError.DatabaseError

    let getStream (getStream : GetStream) streamName = 
        validateStreamName streamName
        |> AsyncResult.bind (getStream >> AsyncResult.mapError DomainError.DatabaseError)

    let getSnapshots (getSnapshots : GetSnapshots) streamName = 
        validateStreamName streamName
        |> AsyncResult.bind (getSnapshots >> AsyncResult.mapError DomainError.DatabaseError)

    let getEvents (getEvents : GetEvents) streamName startAtVersion = asyncResult {          
        let! streamName = validateStreamName streamName
        let! startAtVersion = validateVersion startAtVersion
              
        let! result = 
            getEvents streamName startAtVersion
            |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }

    let deleteSnapshots (deleteSnapshots : DeleteSnapshots) streamName = 
        validateStreamName streamName
        |> AsyncResult.bind (deleteSnapshots >> AsyncResult.mapError DomainError.DatabaseError)