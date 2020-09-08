namespace EventStore.Domain

open System

type StringMax = private StringMax of string

[<RequireQualifiedAccess>]
module StringMax =

    let value (StringMax x) = x

    let tryCreate value =
        if String.IsNullOrWhiteSpace(value)
        then None
        else Some (StringMax value) 

type String50 = private String50 of string

[<RequireQualifiedAccess>]
module String50 =

    let value (String50 x) = x

    let tryCreate value =
        if String.IsNullOrWhiteSpace(value)
        then None
        else if value.Length > 50
        then None
        else Some (String50 value)

type String256 = private String256 of string

[<RequireQualifiedAccess>]
module String256 =

    let value (String256 x) = x

    let tryCreate value =
        if String.IsNullOrWhiteSpace(value)
        then None
        else if value.Length > 256
        then None
        else Some (String256 value)            

type NonNegativeInt = private NonNegativeInt of Int32

[<RequireQualifiedAccess>]
module NonNegativeInt =

    let value (NonNegativeInt x) = x

    let tryCreate (value : int32) =
        if value < 0
        then None
        else Some (NonNegativeInt value)

[<RequireQualifiedAccess>]
type DomainError =
    | ValidationError of errorMessage : string           
    | StreamNotFound
    | InvalidVersion
    | DatabaseError of ex : Exception

type NewEvent = {
    Data : StringMax
    Type : String256 }

type AppendEvents = {
    ExpectedVersion : NonNegativeInt
    StreamName : String256
    Events : NewEvent list }

type CreateSnapshot = {
    StreamName : String256
    Description : String256
    Data : StringMax }

type StreamQuery = { StreamName : String256 }    

type SnapshotsQuery = { StreamName : String256 }

type EventsQuery = { 
    StreamName : String256
    StartAtVersion : NonNegativeInt }