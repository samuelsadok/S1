using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AppInstall.Framework;
using AppInstall.Networking;

namespace GDM
{
    public class GDMServer : Server<HTTP.Methods, HTTP.StatusCodes>
    {

        private GDMDB db;
        private AmtangeeDB amtangee;
        private TimeSpan sessionLifespan;

        public GDMServer(GDMDB db, AmtangeeDB amtangee, TimeSpan sessionLifespan, int clientSlots, int clientTimeQuota, LogContext logContext)
            : base(CustomConstants.DB_SERVER_PORT, clientSlots, clientTimeQuota, logContext)
        {
            this.db = db;
            this.amtangee = amtangee;
            this.sessionLifespan = sessionLifespan;
        }


        /// <summary>
        /// Throws an exception if the user is not logged in.
        /// </summary>
        private void EnforceLogin(GDMDB.Session session)
        {
            if (!db.HasStandardPrivileges(session))
                throw new HTTP.HTTPException(HTTP.StatusCodes.Unauthorized);
        }


        public override NetMessage<HTTP.Methods, HTTP.StatusCodes> HandleRequest(NetMessage<HTTP.Methods, HTTP.StatusCodes> request, IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            LogContext.Log("handle request...", LogType.Debug);
            var response = new NetMessage<HTTP.Methods, HTTP.StatusCodes>(GDMDBProtocol.PROTOCOL_IDENTIFIER, HTTP.StatusCodes.OK);

            var session = db.RecallSession(Utilities.TryParseGuid(request.GetFieldOrDefault("Session", null)), sessionLifespan);
            response["Session"] = session.Guid.ToString();
            LogContext.Log("session is " + session.Guid, LogType.Debug);

            string urlRemainder = null;
            
            switch (request.Method) {
                case HTTP.Methods.GET:

                    if (request.Resource.URLStartsWith(GDMDBProtocol.CONTACTS_RESOURCE, out urlRemainder)) {
                        var list = amtangee.GetContacts(Guid.Parse(Config.CommonConfig["reports/contacts_category"]), true).ToArray();
                        LogContext.Log("returning " + list.Count() + " contacts");
                        response.Content = new BinaryContent(Utilities.XMLSerialize(list));
                        break;
                    }
                    
                    EnforceLogin(session);

                    if (request.Resource.URLStartsWith(GDMDBProtocol.ITEMS_RESOURCE, out urlRemainder)) {

                        var recursive = bool.Parse(request.Query["recursive"]);
                        var list = new ItemFolder() { Guid = Guid.Parse(urlRemainder) };
                        amtangee.GetItems(list, recursive);
                        LogContext.Log("returning " + list.Items.Count() + " items, recursive: " + recursive);
                        response.Content = new BinaryContent(Utilities.XMLSerialize(list));

                    } else if (request.Resource.URLStartsWith(GDMDBProtocol.UNITS_RESOURCE, out urlRemainder)) {
                        var list = amtangee.GetUnits();
                        LogContext.Log("returning " + list.Count() + " units");
                        response.Content = new BinaryContent(Utilities.XMLSerialize(list));

                    } else if (request.Resource.URLStartsWith(GDMDBProtocol.REPORTS_RESOURCE, out urlRemainder)) {
                        DateTime earliest = new DateTime(long.Parse(request.Query.GetValueOrDefault("from", "0")));
                        DateTime latest = new DateTime(long.Parse(request.Query.GetValueOrDefault("to", DateTime.UtcNow.Ticks.ToString())));
                        earliest = earliest.Bound(System.Data.SqlTypes.SqlDateTime.MinValue.Value, System.Data.SqlTypes.SqlDateTime.MaxValue.Value);
                        latest = latest.Bound(System.Data.SqlTypes.SqlDateTime.MinValue.Value, System.Data.SqlTypes.SqlDateTime.MaxValue.Value);
                        var list = db.GetReports(session, earliest, latest);
                        LogContext.Log("returning " + list.Count() + " reports");
                        response.Content = new BinaryContent(Utilities.XMLSerialize(list.ToArray()));

                    } else {
                        throw new ResourceNotFoundException(request.Resource);
                    }
                    break;

                case HTTP.Methods.PUT:

                    if (request.Resource.URLStartsWith(GDMDBProtocol.LOGIN_RESOURCE, out urlRemainder)) {
                        if (db.Login(session, Guid.Parse(request.Query["user"]), request.Query["password"]) != true)
                            throw new HTTP.HTTPException(HTTP.StatusCodes.Forbidden);
                        break;
                    }

                    EnforceLogin(session);

                    if (request.Resource.URLStartsWith(GDMDBProtocol.REPORTS_RESOURCE, out urlRemainder)) {
                        db.SubmitReport(session, Utilities.XMLDeserialize<Report>(((BinaryContent)request.Content).Content));
                    } else if (request.Resource.URLStartsWith(GDMDBProtocol.SET_ACCOUNT_PASSWORD, out urlRemainder)) {
                        db.DefineAccount(session, Guid.Parse(request.Query.GetValueOrDefault("user", session.Guid.ToString())), request.Query["pass"]);
                    } else if (request.Resource.URLStartsWith(GDMDBProtocol.SET_ACCOUNT_TYPE, out urlRemainder)) {
                        db.EditAccount(session, Guid.Parse(request.Query.GetValueOrDefault("user", session.Guid.ToString())), bool.Parse(request.Query["office"]));
                    } else {
                        throw new ResourceNotFoundException(request.Resource);
                    }
                    break;

                default: throw new HTTP.HTTPException(HTTP.StatusCodes.MethodNotAllowed, "method \"" + request.Method + "\" not supported");
            }

            return response;
        }

        public override NetMessage<HTTP.Methods, HTTP.StatusCodes> HandleException(NetMessage<HTTP.Methods, HTTP.StatusCodes> request, Exception exception, CancellationToken cancellationToken)
        {
            LogContext.Log("client request failed: " + exception.ToString(), LogType.Error);

            HTTP.HTTPException httpEx = exception as HTTP.HTTPException;
            return new NetMessage<HTTP.Methods, HTTP.StatusCodes>(GDMDBProtocol.PROTOCOL_IDENTIFIER, httpEx == null ? HTTP.StatusCodes.InternalServerError : httpEx.StatusCode) {
                Content = new BinaryContent(exception.ToString())
            };
        }
    }
}
