module SignalRHubs

#if INTERACTIVE
#r "../packages/FSharp.Data.2.0.7/lib/net40/FSharp.Data.dll"
#endif    
open System
open FSharp.Data
open Hopac
open System.Threading.Tasks

let logger = Logary.Logging.getCurrentLogger ()
let writeLog x = Logary.Logger.log logger x |> start

let bank = WorldBankData.GetDataContext()
let countries = bank.Regions.World.Countries
                |> Seq.filter(fun i -> i.CapitalCity <> "") 
                |> Seq.map(fun i -> i.Name, i.CapitalCity)

let corrects = countries |> Seq.toArray
let rnd = Random()
let questionItem() = corrects.[rnd.Next(corrects.Length)]

let generateQuiz() =
    let correct, capitalCity = questionItem()
    capitalCity, [correct; fst(questionItem()); fst(questionItem())] |> List.sort

let checkAnswer country capitalCity =
    corrects |> Array.exists(fun (cou, cap) -> cou = country && cap = capitalCity)

let preGenerated = [0..500] |> List.map(fun _ -> generateQuiz())
let takeNth n = preGenerated |> Seq.skip n |> Seq.head

type QuizItem(quizId)=
    let item = takeNth quizId
    member x.QuizId = quizId
    member x.Capital = item |> fst
    member x.Countries = item |> snd

open Microsoft.AspNet.SignalR

type IMessageToClient =  // Server can push data to single or all clients
    abstract informResult : string -> Task
    abstract newQuiz : QuizItem -> Task

type CatsHub() =
    inherit Hub<IMessageToClient>()

    override this.OnConnected() =
        base.OnConnected() |> ignore
        QuizItem(rnd.Next(500)) |> this.Clients.Caller.newQuiz

    member this.GuessCountry(quizzId : int, country : string):unit =
        let capital = takeNth quizzId |> fst
        match checkAnswer country capital with
        | true ->
            this.Clients.Caller.informResult("Correct!") |> ignore
            QuizItem(rnd.Next(500)) |> this.Clients.Caller.newQuiz |> ignore
            //this.Clients.All?informResult("Correct found")
        | false ->
            this.Clients.Caller.informResult("Wrong! Try again!") |> ignore

