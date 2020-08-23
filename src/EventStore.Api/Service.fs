namespace EventStore.Domain

open System
open System.Data
open EventStore.Extensions
open EventStore.DataAccess
open FsToolkit.ErrorHandling

type DependencyProvider = {
    GetDbConnection : unit -> IDbConnection
    GetStream : IDbConnection -> StreamName -> Async<Result<Stream option, exn>>
    GetAllStreams : IDbConnection -> Async<Result<Stream list, exn>>
    GetSnapshots : IDbConnection -> StreamName -> Async<Result<Snapshot list, exn>>
    GetEvents : IDbConnection -> StreamName -> Version -> Async<Result<Event list, exn>>
    DeleteSnapshots : IDbConnection -> StreamName -> Async<Result<unit, exn>>
}

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
   
    let getStream (dependency : DependencyProvider) streamName = asyncResult {   
        let! streamName = validateStreamName streamName
           
        use connection = dependency.GetDbConnection ()
        
        let! result = 
            dependency.GetStream connection streamName
            |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }

    let getSnapshots (dependency : DependencyProvider) streamName = asyncResult {  
        let! streamName = validateStreamName streamName
           
        use connection = dependency.GetDbConnection ()
        
        let! result = 
            dependency.GetSnapshots connection streamName
            |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }

    let getEvents (dependency : DependencyProvider) streamName startAtVersion = asyncResult {          
        let! streamName = validateStreamName streamName
        let! startAtVersion = validateVersion startAtVersion
              
        use connection = dependency.GetDbConnection ()
        
        let! result = 
            dependency.GetEvents connection streamName startAtVersion
            |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }

    let deleteSnapshots (dependency : DependencyProvider) streamName = asyncResult {
        let! streamName = validateStreamName streamName
           
        use connection = dependency.GetDbConnection ()
        
        let! result = 
            dependency.DeleteSnapshots connection streamName
            |> AsyncResult.mapError DomainError.DatabaseError

        return result
    }