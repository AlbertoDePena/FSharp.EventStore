namespace EventStore.Extensions

open System
open System.Text.RegularExpressions
open System.Threading.Tasks

[<RequireQualifiedAccess>]
module Strings =
    
    let isNullOrEmpty = String.IsNullOrEmpty

    let isNullOrWhiteSpace = String.IsNullOrWhiteSpace

    let isNotNullOrEmpty = isNullOrEmpty >> not

    let isNotNullOrWhiteSpace = isNullOrWhiteSpace >> not

    let empty = String.Empty
    
    let trim (value : string) = value.Trim()

    let toUpper (value : string) = value.ToUpper()

    let toLower (value : string) = value.ToLower()

    let isMatch pattern value = Regex.IsMatch(value, pattern)

    let createOptional value =
        if isNullOrWhiteSpace value then
           None
        else Some (trim value)

    let createRequired name value =
        if isNullOrWhiteSpace value then
            Error (sprintf "'%s' is required." name)
        else Ok (trim value)

    let validateMaxLength name length value =
        if String.length value > length then
            Error (sprintf "'%s' cannot be longer than %i characters." name length)
        else Ok value

    let validateFormat (predicate : string -> bool) name value =
        if predicate value then
            Ok value
        else Error (sprintf "'%s' is not properly formatted." name)

[<RequireQualifiedAccess>]
module Async =
    
    /// <summary>
    /// Async.StartAsTask and up-cast from Task<unit> to plain Task.
    /// </summary>
    /// <param name="task">The asynchronous computation.</param>
    let AsTask (task : Async<unit>) = Async.StartAsTask task :> Task
   