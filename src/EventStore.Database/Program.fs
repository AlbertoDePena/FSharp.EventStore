namespace EventStore.Database

open System
open System.Reflection
open DbUp
open DbUp.Helpers

module Program =

    type UpgradeTarget =
        | SqlScripts
        | StoredProcedures

    let update dbConnectionString upgradeTarget =
        let directory =
            match upgradeTarget with
            | SqlScripts -> "SqlScripts"
            | StoredProcedures -> "StoredProcedures"

        let builder =
            DeployChanges.To.SqlDatabase(dbConnectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), fun assemblyName -> assemblyName.Contains(directory))
                .LogScriptOutput()
                .LogToConsole()

        let upgrader = 
            match upgradeTarget with
            | SqlScripts -> builder.Build() 
            | StoredProcedures -> builder.JournalTo(NullJournal()).Build()

        let result = upgrader.PerformUpgrade()

        if not result.Successful 
        then raise result.Error

    [<EntryPoint>]
    let main _ =
        let dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")

        if String.IsNullOrWhiteSpace(dbConnectionString)
        then failwith "Database connection string is required"
        else
            update dbConnectionString SqlScripts
            update dbConnectionString StoredProcedures

        0 // return an integer exit code
