using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AppInstall.Framework;

namespace AppInstall.Networking
{
    public class Client<M, S>
        where M : struct, IConvertible
        where S : struct, IConvertible
    {

        /// <summary>
        /// Indicates that the request must be repeated, using a new message and specifies a maximum number of subsequent attempts to prevent infinite loops.
        /// </summary>
        public class RetryRequiredException : Exception
        {
            public NetMessage<M, S> NewRequest { get; private set; }
            public int MaxAttemts { get; private set; }
            public RetryRequiredException(NetMessage<M, S> newRequest, int maxAttempts)
            {
                NewRequest = newRequest;
                maxAttempts = MaxAttemts;
            }
        }




        private LogContext logContext;

        public Client(LogContext logContext)
        {
            this.logContext = logContext;
        }


        public string Host { get; set; }

        public int Port { get; set; }

        /// <summary>
        /// Can be used to configure a callback that checks if the response indicates an error on the server side.
        /// The function will be called for every response that is received.
        /// If the function returns a retry required exception, the new request is sent instead of the exception being raised.
        /// Arguments: request, response, Returns: the exception to be raised or null.
        /// </summary>
        public Func<NetMessage<M, S>, NetMessage<M, S>, Exception> ResponseCheck { get; set; }

        /// <summary>
        /// Connects to the server.
        /// </summary>
        private NetworkStream Connect(TcpClient client, CancellationToken cancellationToken)
        {
            var result = client.BeginConnect(Host, Port, null, null);

            if (WaitHandle.WaitAny(new WaitHandle[] { cancellationToken.WaitHandle, result.AsyncWaitHandle }) == 0) {
                client.Close();
                throw new OperationCanceledException();
            }

            client.EndConnect(result);

            return client.GetStream();
        }

        /// <summary>
        /// Sends a message to the server and receives the response.
        /// Sends additional messages if instructed by the response check.
        /// Only the response to the last message will be returned.
        /// </summary>
        public async Task<NetMessage<M, S>> SendRequest(NetMessage<M, S> request, CancellationToken cancellationToken)
        {
            int attempt = 1;

            while (true) {

                Tuple<NetMessage<M, S>, BinaryContent> response;

                logContext.Log("creating tcp client...");
                using (TcpClient client = new TcpClient()) {
                    logContext.Log("connecting to " + Host + ":" + Port + "...");
                    using (NetworkStream stream = Connect(client, cancellationToken)) {

                        // create watchdog task that closes the stream on cancellation
                        ManualResetEvent transferComplete = new ManualResetEvent(false);
                        new Task(() => {
                            if (WaitHandle.WaitAny(new WaitHandle[] { cancellationToken.WaitHandle, transferComplete }) == 0)
                                stream.Close();
                        }).Start();

                        try {
                            // send response and receive answer
                            logContext.Log("sending request " + request.Header);
                            request["Host"] = Host;
                            await request.WriteToStream(stream, cancellationToken);
                            response = await NetMessage<M, S>.ReadFromStream<BinaryContent>(stream, cancellationToken);
                            transferComplete.Set();
                            logContext.Log("received response");
                        } catch {
                            cancellationToken.ThrowIfCancellationRequested();
                            logContext.Log("request failed", LogType.Error);
                            throw;
                        }
                    }
                }

                // exception handling
                Exception ex = null;
                if (ResponseCheck != null) ex = ResponseCheck(request, response.Item1);
                var retry = ex as RetryRequiredException;
                if (retry != null)
                    if (retry.MaxAttemts > attempt++)
                        request = retry.NewRequest;
                    else
                        throw new Exception("too many attemts were made to complete a request, last attempt was \"" + request.Header + "\"");
                else if (ex != null)
                    throw new ServerSideException(request.Header, ex);
                else
                    return response.Item1;
            }
        }
    }


    public class ServerSideException : Exception
    {
        public ServerSideException(string request, Exception innerException)
            : base("the server reported an error for the request \"" + request + "\"", innerException)
        {
        }
    }
}
