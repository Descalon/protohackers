open System.Net.Sockets
open System.Net
open System.Text
open System.Threading

let fromBytes (index: int) (count:int) (input : byte[]) = Encoding.UTF8.GetString(input, index, count)
let toBytes (input : string) = Encoding.UTF8.GetBytes(input)

let generateEndpoint = async {
    return IPEndPoint(IPAddress.Parse("0.0.0.0"), 80)
} 

let startServer = async {
    let! endpoint = generateEndpoint
    let socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
    socket.Bind(endpoint)
    socket.Listen(10)
    return socket
}

let server = startServer |> Async.RunSynchronously

let receive (socket: Socket) = async {
    printfn "Waiting to receive"
    let buffer = Array.create 8192 0uy
    let! recieved = socket.ReceiveAsync(buffer, SocketFlags.None) |> Async.AwaitTask
    let trunc = buffer |> Array.truncate recieved
    printfn "Received message: %s" (fromBytes 0 recieved trunc)
    return buffer |> Array.truncate recieved
}

let send (socket: Socket) (buffer: byte[]) = async {
    do! socket.SendAsync(buffer) |> Async.AwaitTask |> Async.Ignore
}

let echo (socket: Socket) = async {
    let! r = receive socket
    do! send socket r
}

let run (fn: Socket -> Async<'a>) (token: CancellationToken) (socket: Socket)= async {
    printfn "Server started listening on port %i" 80
    let childTask (s:Socket) = async {
        do! fn s |> Async.Ignore
    }
    while not token.IsCancellationRequested do 
        printfn "Waiting for client"
        let! handler = socket.AcceptAsync() |> Async.AwaitTask
        if handler.Connected then 
            printfn "Connected to client"
            Async.Start ((childTask handler),token)
}

let csource = new CancellationTokenSource()
System.Console.CancelKeyPress.Add( fun _ -> csource.Cancel(); csource.Dispose(); printfn "Server is disposed")

Async.Start (server |> run echo csource.Token)

while true do ()