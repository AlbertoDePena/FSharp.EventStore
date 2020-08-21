namespace EventStore.Api

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http

module Functions =

    [<FunctionName("HelloWorld")>]
    let HelloWorld ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>] request: HttpRequest) (logger: ILogger) =
        logger.LogInformation("Executing HelloWorld function...")

        OkObjectResult("Hello World.") :> IActionResult 
