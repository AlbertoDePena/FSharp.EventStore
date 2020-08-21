namespace EventStore.Domain

open System
open System.Data
open EventStore.Extensions
open EventStore.DataAccess
open Microsoft.Extensions.Logging

type DependencyProvider = {
    Logger : ILogger
    GetDbConnection : unit -> IDbConnection
    GetStream : IDbConnection -> StreamName -> Async<Stream list>
    GetSnapshots : IDbConnection -> StreamName -> Async<Snapshot list>
    GetEvents : IDbConnection -> StreamName -> Version -> Async<Event list>
    DeleteSnapshots : IDbConnection -> StreamName -> Async<unit>
}

[<RequireQualifiedAccess>]
module Service =

    let private toAsyncResult (logger : ILogger) computation = async {
        let! choice = computation |> Async.Catch

        let result =
            match choice with
            | Choice1Of2 data -> Ok data
            | Choice2Of2 error ->
                logger.LogError(error, error.Message) 
                Error DomainError.DatabaseError

        return result
    }
   
    let getStream (dependency : DependencyProvider) streamName = async {   
        match Validation.validateStreamName streamName with
        | Error error -> return Error (DomainError.ValidationError error)
        | Ok streamName ->             
            use connection = dependency.GetDbConnection ()
            
            let! result = 
                dependency.GetStream connection streamName
                |> (toAsyncResult dependency.Logger)

            return result
    }

    let getSnapshots (dependency : DependencyProvider) streamName = async {  
        match Validation.validateStreamName streamName with
        | Error error -> return Error (DomainError.ValidationError error)
        | Ok streamName ->                 
            use connection = dependency.GetDbConnection ()
            
            let! result = 
                dependency.GetSnapshots connection streamName
                |> (toAsyncResult dependency.Logger)

            return result
    }

    let getEvents (dependency : DependencyProvider) streamName startAtVersion = async {  
        
        let eventsQuery = result {
            let! streamName = Validation.validateStreamName streamName
            let! startAtVersion = Validation.validateVersion startAtVersion

            return (streamName, startAtVersion)
        }

        match eventsQuery with
        | Error error -> return Error (DomainError.ValidationError error)
        | Ok (streamName, startAtVersion) ->                 
            use connection = dependency.GetDbConnection ()
            
            let! result = 
                dependency.GetEvents connection streamName startAtVersion
                |> (toAsyncResult dependency.Logger)

            return result
    }

    let deleteSnapshots (dependency : DependencyProvider) streamName = async {
        match Validation.validateStreamName streamName with
        | Error error -> return Error (DomainError.ValidationError error)
        | Ok streamName ->   
            use connection = dependency.GetDbConnection ()
            
            let! result = 
                dependency.DeleteSnapshots connection streamName
                |> (toAsyncResult dependency.Logger)

            return result
    }