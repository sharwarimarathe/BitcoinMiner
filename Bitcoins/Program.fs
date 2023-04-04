#if INTERACTIVE
#time "on"
#r "nuget: Akka, 1.4.25"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
open Akka.TestKit
#endif

open System
open Akka
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Linq
open System.Security.Cryptography
open System.Text
open Akka.Cluster
open Configs
open Akka
let timeWatch = System.Diagnostics.Stopwatch()
let processor = System.Diagnostics.Process.GetCurrentProcess()
let mutable cpuTimer = processor.TotalProcessorTime
type messageLog =
    | RStringLog
    | FindMineLog

type MessageType =
    | FindMine
    | StringRandom



type Message = {Type: MessageType; mutable code: int; Value: String }

let printStatistics() =
    let cpuCurrent = (processor.TotalProcessorTime-cpuTimer).TotalMilliseconds
    printfn "\n\n***********************************************************************************\n\
    The current statistics\n\
    The absolute time is= %dms\n\
    The CPU time is= %dms\n\
    ***************************************************************************************\n" (int64 cpuCurrent) timeWatch.ElapsedMilliseconds
let rec readInput() =
    let cmd = System.Console.ReadLine()

    match cmd with
        |   "printStatistics" ->
             printStatistics()
             readInput()
        | _ -> readInput()

let coinFound string1 digest zeros nodeAddress=
    printfn "\n\n***************************************************************************\n\
    BitCoin found!\n\
    The coin is = %s\n\
    The hash is= %s\n\
    The leading zeros are= %d\n\
    Actor = %s\n\
    *****************************************************************************************\n" string1 digest zeros nodeAddress

let parametersStr() =
    cpuTimer <- processor.TotalProcessorTime
    timeWatch.Start()
let mutable actorSystem = Unchecked.defaultof<Actor.ActorSystem>

   
   

[<EntryPoint>]
let main temp =
    let nodeTemp = temp.[0]
    let zeroTemp = (temp.[1] |> int)
    let hostSeed = if nodeTemp<>"loneNode" then temp.[2] else ""
    let port = if nodeTemp<>"loneNode" then temp.[3] else ""
    let host = if nodeTemp="client" then temp.[4] else ""
    let system1 = if nodeTemp = "loneNode" then "coin-miner" else "clusters-miners"
    let node1 = sprintf "cluster-node-%s" Environment.MachineName
    let mineGen =
        if nodeTemp = "loneNode" then "akka://" + system1 + "/user/mineGenerator" else "akka.tcp://" + system1 +  "@" + hostSeed + ":" + port + "/user/mineGenerator"

    let stringGen (mailBox: Actor<MessageType>) =
        let rec loop() = actor {
            let! message = mailBox.Receive();
            match message with
            | StringRandom ->
                let rStr k=
                    let r = Random()
                    let chars = Array.concat([[|'a' .. 'z'|];[|'A' .. 'Z'|];[|'0' .. '9'|]])
                    let sz = Array.length chars in
                    "smathapati" + String(Array.init k (fun _ -> chars.[r.Next sz]))
                let y=rStr 10
                select mineGen mailBox.Context.System <! {Type=FindMine; code=0; Value=y}
                mailBox.Self <! StringRandom
            | _ ->
                printfn "%s : Invalid string entered" (mailBox.Self.Path.ToStringWithAddress())
            return! loop()
        }
        loop()
   
    let hashGen (mailbox: Actor<Message>) =
        let rec loop() = actor {
            let! message = mailbox.Receive ()
            match message.Type with
            | FindMine ->
                let input = if message.code > 0 then (message.Value + message.code.ToString()) else message.Value
                let lowercase (x : string) = x.ToLower()
                let replace (x : string) = x.Replace("-", "")
                let encode(x:string)=Encoding.ASCII.GetBytes(x)
                let digest =
                    input
                        |> encode
                        |> (new SHA256Managed()).ComputeHash
                        |> System.BitConverter.ToString
                        |> lowercase
                        |>replace
                let numZeros =
                    let mutable counter =0
                    let mutable flag = 0
                    for i in digest do
                        if i = '0' && flag <> 1 then
                            counter <- counter+1
                        elif i <> '0'  then
                             flag <- 1
                    counter
                if numZeros = zeroTemp then
                    coinFound input digest zeroTemp (mailbox.Self.Path.ToStringWithAddress())
                elif message.code < Int32.MaxValue then
                    message.code <- (message.code+1)
                    select mineGen mailbox.Context.System <! message
            | _ ->
                printfn " Invalid message received"
            return! loop ()
        }
        loop()

    if nodeTemp = "loneNode" then
        printfn "Starting the single node"
        let system = System.create system1 <| loneNodeConfig
        spawnOpt system "mineGenerator" hashGen [ Router(Akka.Routing.FromConfig.Instance) ] |> ignore
        let routerCreator = spawnOpt system "routerCreatorClust" stringGen [ Router(Akka.Routing.FromConfig.Instance) ]
        routerCreator <! StringRandom
        parametersStr()
    elif nodeTemp = "client" then
         printfn "Beginning the client node"
         let system3 = clientAkkaConfig host port hostSeed |> System.create system1
         parametersStr()
         
         let clientRef =  
             spawn system3 "listenClient"
             <| fun mailbox ->
                 let cluster = Cluster.Get (mailbox.Context.System)
                 cluster.Subscribe (mailbox.Self, [| typeof<ClusterEvent.IMemberEvent>|])
                 mailbox.Defer <| fun () -> cluster.Unsubscribe (mailbox.Self)
                 printfn "Created an actor on node [%A] with roles [%s]" cluster.SelfAddress (String.Join(",", cluster.SelfRoles))
                 let rec loop () = actor {
                     let! (msg: obj) = mailbox.Receive ()
                     match msg with
                     | :? ClusterEvent.MemberRemoved as actor ->
                             printfn "Actor Member has been removed %A" msg
                     | :? ClusterEvent.IMemberEvent           ->
                             printfn "The event was received from the cluster %A" msg
                     | _ ->
                             printfn "Received the message: %A" msg
                     return! loop ()
                 }
                 loop ()
         0 |> ignore
    else
        printfn "Beginning the seed node"
        let system2 = seedAkkaConfig hostSeed port |> System.create system1

        let seedAct =
            spawn system2 node1 (fun (mailBox: Actor<ClusterEvent.IClusterDomainEvent>) ->
                let cluster = Cluster.Get system2
                cluster.Subscribe(mailBox.Self, [| typeof<ClusterEvent.IClusterDomainEvent> |])
                mailBox.Defer(fun () -> cluster.Unsubscribe(mailBox.Self))
                let rec loop () =
                    actor {
                        let! message = mailBox.Receive()                        
                        match message with
                        | :? ClusterEvent.MemberJoined as event ->
                            printfn "Node %s Joined at %O" event.Member.Address.Host DateTime.Now
                        | :? ClusterEvent.MemberLeft as event ->
                            printfn "Node %s Left at %O" event.Member.Address.Host DateTime.Now
                        | other ->
                            printfn "Cluster event was received at %O and %O" other DateTime.Now
       
                        return! loop()
                    }
                loop())

        let cluster = Cluster.Get system2
        cluster.RegisterOnMemberUp (fun () ->
            spawnOpt system2 "mineGenerator" hashGen [ Router(Akka.Routing.FromConfig.Instance) ] |> ignore
            let routerCreator = spawnOpt system2 "routerCreatorClust" stringGen [ Router(Akka.Routing.FromConfig.Instance) ]//created actor for string generation
            routerCreator <! StringRandom
            parametersStr()
        )

        0 |> ignore
    readInput()

    0


