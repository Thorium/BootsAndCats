module Server


open System
open SignalRHubs
open OwinStart
open Logary
open Logary.Configuration
open Logary.Targets
open Logary.Metrics
open Hopac

let mutable server = Unchecked.defaultof<IDisposable>
let mutable log = Unchecked.defaultof<IDisposable>

let startServer() =
    let fetchLogLevel =
        match System.Configuration.ConfigurationManager.AppSettings.["LogLevel"].ToString().ToLower() with
        | "error" -> LogLevel.Error
        | "warn" -> LogLevel.Warn
        | "debug" -> LogLevel.Debug
        | _ -> LogLevel.Verbose

    log <-
        withLogaryManager "BootsAndCats" (
            withTargets [
                // See Logary examples for advanced logging.
                Console.create (Console.empty) "console"
            ] >> withRules [
                Rule.createForTarget "console" |> Rule.setLevel fetchLogLevel
            ]
        ) |> run
    //Scheduler.doStuff()

    let options = Microsoft.Owin.Hosting.StartOptions()

    let addPorts protocol addr (ports:string) =
        ports.Split(',')
        |> Array.filter(fun p -> p<>"")
        |> Array.map(fun port -> protocol + "://" + addr + ":" + port)
        |> Array.iter(fun url ->
            Message.eventInfo url |> writeLog
            options.Urls.Add url
        )

    addPorts "http" System.Configuration.ConfigurationManager.AppSettings.["WebServerIp"] System.Configuration.ConfigurationManager.AppSettings.["WebServerPorts"]

    // You probably need adnmin rights to start a web server:
    server <- Microsoft.Owin.Hosting.WebApp.Start<MyWebStartup> options

    let logger = Logging.getCurrentLogger ()
    Message.eventInfo ("Server started.") |> writeLog

let stopServer() =
    if server <> Unchecked.defaultof<IDisposable> then
        server.Dispose()
    if log <> Unchecked.defaultof<IDisposable> then
        log.Dispose()

#if INTERACTIVE
#else
[<EntryPoint>]
#endif
let main args = 
    startServer()
    Message.eventInfo "Press Enter to stop & quit." |> writeLog
    Console.ReadLine() |> ignore
    stopServer()
    0