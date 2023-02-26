open System.Net.Sockets
open System.Net
open System.Text
open System.Threading
open Protohackers

let echo (socket: Socket) =
    async {
        let! r = Server.receive socket
        do! Server.send socket r
    }
let endpoint = IPEndPoint(IPAddress.Parse("0.0.0.0"), 80)

let startServer = Server.start endpoint 10 

let server = Server.start endpoint 10 |> Async.RunSynchronously

let csource = new CancellationTokenSource()
System.Console.CancelKeyPress.Add( fun _ -> csource.Cancel(); csource.Dispose(); printfn "Server is disposed")

Async.Start (server |> Server.run echo csource.Token)

while true do ()