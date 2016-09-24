module OwinStart
open System
open System.IO
open Owin
open Microsoft.AspNet.SignalR
open SignalRHubs

let hubConfig = HubConfiguration(EnableDetailedErrors = true, EnableJavaScriptProxies = true)

open System.IO
let getRootedPath (path:string) =
    if Path.IsPathRooted path then 
        path 
    else 
        let parsed = 
            path.Split([|@"\"; "/"|], StringSplitOptions.None)
            |> Path.Combine
#if INTERACTIVE
        let basePath = __SOURCE_DIRECTORY__
#else
        let basePath = 
            System.Reflection.Assembly.GetExecutingAssembly().Location
            |> Path.GetDirectoryName
#endif
        Path.Combine(basePath, parsed)

let serverPath = 
    let path = System.Configuration.ConfigurationManager.AppSettings.["WebServerFolder"].ToString() |> getRootedPath
    if not(Directory.Exists path) then Directory.CreateDirectory (path) |> ignore
    Microsoft.Owin.FileSystems.PhysicalFileSystem path

type LoggingPipelineModule() =
    inherit Microsoft.AspNet.SignalR.Hubs.HubPipelineModule() with 
        override __.OnIncomingError(exceptionContext, invokerContext) =
            let invokeMethod = invokerContext.MethodDescriptor
            let args = String.Join(", ", invokerContext.Args)
            Logary.Message.eventError(invokeMethod.Hub.Name + "." + invokeMethod.Name + "(" + args + ") exception:\r\n " + exceptionContext.Error.ToString()) |> writeLog
            base.OnIncomingError(exceptionContext, invokerContext)

        override __.OnBeforeIncoming context =
            Logary.Message.eventDebug("=> Invoking " + context.MethodDescriptor.Hub.Name + "." + context.MethodDescriptor.Name) |> writeLog
            base.OnBeforeIncoming context

        override __.OnBeforeOutgoing context =
            Logary.Message.eventDebug("<= Invoking " + context.Invocation.Hub + "." + context.Invocation.Method) |> writeLog
            base.OnBeforeOutgoing context


type MyWebStartup() =
    member x.Configuration(app:Owin.IAppBuilder) =
        //OWIN Component registrations here...

        //SignalR:
        app.MapSignalR(hubConfig) |> ignore            

        //Static files server (Note: default FileSystem is current directory!)
        let fileServerOptions = Microsoft.Owin.StaticFiles.FileServerOptions()
        fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add "index.html"
        fileServerOptions.FileSystem <- serverPath
        app.UseFileServer fileServerOptions
        |> ignore

        ()

[<assembly: Microsoft.Owin.OwinStartup(typeof<MyWebStartup>)>]
do()
