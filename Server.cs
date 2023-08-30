using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace HTTPServer
{
    class Server
    {
        Socket serverSocket;

        public Server(int portNumber, string redirectionMatrixPath)
        {
            //TODO: call this.LoadRedirectionRules passing redirectionMatrixPath to it
            LoadRedirectionRules(redirectionMatrixPath);
            //TODO: initialize this.serverSocket
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 1000);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(ipep);
        }

        public void StartServer()
        {
            // TODO: Listen to connections, with large backlog.
            Console.WriteLine("Start Listening....");
            serverSocket.Listen(500);
            // TODO: Accept connections in while loop and start a thread for each connection on function "Handle Connection"
            while (true)
            {
                //TODO: accept connections and start thread for each accepted connection.
                Socket clientsocket = serverSocket.Accept();
                Thread thread = new Thread(new ParameterizedThreadStart(HandleConnection));
                thread.Start(clientsocket);
            }
        }

        public void HandleConnection(object obj)
        {
            Console.WriteLine("Conection Accepted");
            // TODO: Create client socket 
            Socket newclient = (Socket)obj;
            // set client socket ReceiveTimeout = 0 to indicate an infinite time-out period
            newclient.ReceiveTimeout = 0;
            // TODO: receive requests in while true until remote client closes the socket.
            while (true)
            {
                try
                {
                    // TODO: Receive request
                    byte[] recievedData = new byte[65536];
                    int receivedLen = newclient.Receive(recievedData);
                    String data = Encoding.ASCII.GetString(recievedData);
                    Console.WriteLine(data);
                    // TODO: break the while loop if receivedLen==0
                    if (receivedLen == 0)
                    {
                        Console.WriteLine("client ended connection..");
                        break;
                    }
                    // TODO: Create a Request object using received request string
                    Request req = new Request(data);
                    // TODO: Call HandleRequest Method that returns the response
                    Response serverrsponse = HandleRequest(req);
                    string res = serverrsponse.ResponseString;
                    byte[] response = Encoding.ASCII.GetBytes(res);
                    // TODO: Send Response back to client
                    newclient.Send(response);
                }
                catch (Exception ex)
                {
                    // TODO: log exception using Logger class
                    Logger.LogException(ex);
                }
            }

            // TODO: close client socket
        }

        Response HandleRequest(Request request)
        {
            Response res;
            string content = "";
            try
            {
                //TODO: check for bad request 
                if (!request.ParseRequest())
                {
                    //code = "400";
                    //content = "<!DOCTYPE html>< html >< body >< h1 > 400 Bad Request</ h1 >< p > 400 Bad Request</ p ></ body ></ html >";
                    content = LoadDefaultPage(Configuration.BadRequestDefaultPageName);
                    res = new Response(StatusCode.BadRequest, "html", content, "");
                    return res;
                }
                //TODO: map the relativeURI in request to get the physical path of the resource.
                string phyPath = Configuration.RootPath + request.relativeURI;
                //TODO: check for redirect
                string redirectionPath = GetRedirectionPagePathIFExist(request.relativeURI);
                if (!String.IsNullOrEmpty(redirectionPath))
                {
                    phyPath = Configuration.RootPath + "/" + redirectionPath;
                    content = File.ReadAllText(phyPath);
                    res = new Response(StatusCode.Redirect, "html", content, redirectionPath);
                    return res;
                }
                //TODO: check file exists
                if (!File.Exists(phyPath))
                {
                    content = LoadDefaultPage(Configuration.NotFoundDefaultPageName);
                    res = new Response(StatusCode.NotFound, "html", content, "");
                    return res;
                }
                //TODO: read the physical file
                content = File.ReadAllText(phyPath);
                // Create OK response
                res = new Response(StatusCode.OK, "html", content, "");
                return res;
            }
            catch (Exception ex)
            {
                // TODO: log exception using Logger class
                Logger.LogException(ex);
                // TODO: in case of exception, return Internal Server Error. 
                content = LoadDefaultPage(Configuration.InternalErrorDefaultPageName);
                res = new Response(StatusCode.InternalServerError, "html", content, "");
                return res;
            }
        }

        private string GetRedirectionPagePathIFExist(string relativePath)
        {
            // using Configuration.RedirectionRules return the redirected page path if exists else returns empty
            string RedirectionPath;
            if (relativePath[0] == '/')
            {
                relativePath = relativePath.Substring(1);
            }
            if (Configuration.RedirectionRules.TryGetValue(relativePath, out RedirectionPath))
            {
                return RedirectionPath;
            }
            else
                return string.Empty;
        }

        private string LoadDefaultPage(string defaultPageName)
        {
            string filePath = Path.Combine(Configuration.RootPath, defaultPageName);
            // TODO: check if filepath not exist log exception using Logger class and return empty string
            if (!File.Exists(filePath))
            {
                Logger.LogException(new Exception(defaultPageName + " Page not Exist"));
                return "";
            }
            // else read file and return its content
            string content = File.ReadAllText(filePath);
            return content;
        }

        private void LoadRedirectionRules(string filePath)
        {
            try
            {
                // TODO: using the filepath paramter read the redirection rules from file
                FileStream sr1 = new FileStream(filePath, FileMode.Open);
                StreamReader sr2 = new StreamReader(sr1);
                // then fill Configuration.RedirectionRules dictionary
                while (sr2.Peek() != -1)
                {
                    string line = sr2.ReadLine();
                    string[] msg = line.Split(',');
                    if (msg[0] == "")
                    {
                        break;
                    }
                    Configuration.RedirectionRules.Add(msg[0], msg[1]);
                }
                sr1.Close();
            }
            catch (Exception ex)
            {
                // TODO: log exception using Logger class
                Logger.LogException(ex);
                Environment.Exit(1);
            }
        }
    }
}