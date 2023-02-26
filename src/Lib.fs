namespace Protohackers

open System
open System.Net.Sockets
open System.Net
open System.Threading


// The inteface loggers need to implement.
type ILogger = abstract Log : Printf.StringFormat<'a,unit> -> 'a

module Logging =
    let DefaultLogger = { 
        new ILogger with
            member __.Log format =
                Printf.kprintf (printfn "LOG: %s") format
    }

[<RequireQualifiedAccess>]
module Server =
    open Logging

    let private decode (bytes: byte[])= System.Text.Encoding.ASCII.GetString bytes


    let start (endpoint: IPEndPoint) port =
        async {
            let socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            endpoint.ToString() |> DefaultLogger.Log "Binding to endpoint %s"
            port.ToString() |> DefaultLogger.Log "Listening on port %s"
            socket.Bind(endpoint)
            socket.Listen(port)
            return socket
        }

    let receive (socket: Socket) =
        async {
            let buffer = Array.create 8192 0uy

            let! recieved =
                socket.ReceiveAsync(buffer, SocketFlags.None)
                |> Async.AwaitTask
            
            let retval = buffer |> Array.truncate recieved

            retval |> decode |> DefaultLogger.Log "recieved text: %s"

            return retval
        }

    let send (socket: Socket) (buffer: byte []) =
        async {
            do!
                socket.SendAsync(buffer)
                |> Async.AwaitTask
                |> Async.Ignore
        }


    let run (fn: Socket -> Async<'a>) (token: CancellationToken) (socket: Socket) =
        async {
            DefaultLogger.Log "Server started listening on port %i" 80
            let childTask (s: Socket) = async { do! fn s |> Async.Ignore }

            while not token.IsCancellationRequested do
                DefaultLogger.Log "Waiting for client"
                let! handler = socket.AcceptAsync() |> Async.AwaitTask

                if handler.Connected then
                    DefaultLogger.Log "Connected to client"
                    Async.Start((childTask handler), token)
        }
