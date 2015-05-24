using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using AppInstall.Framework;
using AppInstall.Security;

namespace GDM
{


    public class GDMDBDataContext : AutoSubmitDataContext
    {
        public Table<GDMDB.Account> Accounts { get { return GetTable<GDMDB.Account>(); } }
        public Table<GDMDB.Report> Reports { get { return GetTable<GDMDB.Report>(); } }
        public Table<GDMDB.Engagement> Engagements { get { return GetTable<GDMDB.Engagement>(); } }
        public Table<GDMDB.ItemUsage> ItemUsages { get { return GetTable<GDMDB.ItemUsage>(); } }
        public Table<GDMDB.Session> Sessions { get { return GetTable<GDMDB.Session>(); } }
    }


    public class GDMDB : Database<GDMDBDataContext>
    {

        #region "DB Abstraction Classes"



        [Table(Name = "Accounts")]
        public class Account
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public Guid Guid { get; set; }

            [Column(CanBeNull = false)]
            public byte[] Password { get; set; }

            [Column(CanBeNull = false)]
            public DateTime Added { get; set; }

            [Column(CanBeNull = false)]
            public bool Office { get; set; }
        }

        [Table(Name = "Reports")]
        public class Report
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public Guid Guid { get; set; }

            [Column(CanBeNull = false)]
            public Guid Author { get; set; }

            [Column(CanBeNull = false)]
            public DateTime Submitted { get; set; }

            [Column(CanBeNull = false)]
            public DateTime Date { get; set; }

            [Column(CanBeNull = false)]
            public Guid Project { get; set; }

            [Column(CanBeNull = false)]
            public string WorkDescription { get; set; }

            [Column(CanBeNull = false)]
            public string Notes { get; set; }

            //[Column(CanBeNull = true)]
            //public Guid? UpdatedVersion { get; set; }

            [Association(ThisKey = "updatedVersionGuid", Storage = "updatedVersion", IsForeignKey = true)]
            public Report UpdatedVersion { get { return updatedVersion.Entity; } set { updatedVersion.Entity = value; } }
            private EntityRef<Report> updatedVersion = new EntityRef<Report>();
            [Column(Name = "updatedVersion", CanBeNull = true)]
            protected Guid? updatedVersionGuid;
        }

        [Table(Name = "Engagements")]
        public class Engagement
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public long ID { get; private set; }

            //[Column(CanBeNull = false)]
            //public Guid Report { get; set; }

            [Association(ThisKey = "reportGuid", Storage = "report", IsForeignKey = true)]
            public Report Report { get { return report.Entity; } set { report.Entity = value; } }
            private EntityRef<Report> report = new EntityRef<Report>();
            [Column(Name = "report", CanBeNull = false)]
            protected Guid reportGuid;

            [Column(CanBeNull = false)]
            public Guid Contact { get; set; }

            [Column(CanBeNull = false)]
            public long Start { get; set; }

            [Column(CanBeNull = false)]
            public long End { get; set; }

            public static explicit operator GDM.Engagement(Engagement e) {
                return new GDM.Engagement() {
                    Contact = e.Contact,
                    Start = TimeSpan.FromTicks(e.Start),
                    End = TimeSpan.FromTicks(e.End)
                };
            }
        }

        [Table(Name = "ItemUsages")]
        public class ItemUsage
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public long ID { get; private set; }

            //[Column(CanBeNull = false)]
            //public Guid Report { get; set; }

            [Association(ThisKey = "reportGuid", Storage = "report", IsForeignKey = true)]
            public Report Report { get { return report.Entity; } set { report.Entity = value; } }
            private EntityRef<Report> report = new EntityRef<Report>();
            [Column(Name = "report", CanBeNull = false)]
            protected Guid reportGuid;

            [Column(CanBeNull = false)]
            public Guid Item { get; set; }

            [Column(CanBeNull = false)]
            public int Quantity { get; set; }

            public static explicit operator GDM.ItemUsage(ItemUsage i)
            {
                return new GDM.ItemUsage() {
                    Item = i.Item,
                    Quantity = i.Quantity
                };
            }
        }


        [Table(Name = "Sessions")]
        public class Session
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public Guid Guid { get; private set; }

            [Column(CanBeNull = true)]
            public Guid? Predecessor { get; set; }

            [Column(CanBeNull = false, IsDbGenerated = true)]
            public Guid User { get; set; }

            [Column(CanBeNull = false)]
            public DateTime Created { get; set; }

            [Column(CanBeNull = false)]
            public DateTime Expiration { get; set; }
        }


        #endregion


        public override string TableName { get { return "GDM"; } }



        #region "Accounts"

        /// <summary>
        /// Sets the password for the specified user.
        /// This creates a new account if the user had no account previously.
        /// Only office users can change passwords of other accounts or create new accounts.
        /// </summary>
        public void DefineAccount(Session session, Guid user, string password)
        {
            using (var db = OpenContext()) {
                var oldAccount = db.Accounts.Where((a) => a.Guid == user).SingleOrDefault();

                if (oldAccount != null) {
                    if (!HasAdvancedPrivileges(db, session) && (session.User != user)) throw new AccessViolationException("only office users are allowed to change passwords of other users");
                    oldAccount.Password = Password.ToHash(password);
                } else {
                    if (!HasAdvancedPrivileges(db, session)) throw new AccessViolationException("only office users are allowed to create accounts");
                    db.Accounts.InsertOnSubmit(new Account() {
                        Guid = user,
                        Added = DateTime.UtcNow,
                        Password = Password.ToHash(password),
                        Office = false
                    });
                }
            }
        }

        /// <summary>
        /// Edits properties of a user account. Only office users can do this.
        /// </summary>
        public void EditAccount(Session session, Guid user, bool office)
        {
            using (var db = OpenContext()) {
                if (!HasAdvancedPrivileges(db, session)) throw new AccessViolationException("only office users are allowed to edit accounts");
                db.Accounts.Where((a) => a.Guid == user).SingleOrDefault().Office = office;
            }
        }

        /// <summary>
        /// Tries to authenticate the user for the specified session using the provided password.
        /// Returns true if the authentication succeeded, false if the password was invalid and null if the user does not exist.
        /// </summary>
        public bool? Login(Session session, Guid user, string password)
        {
            // todo: prevent brute force attacks
            using (var db = OpenContext()) {
                var account = db.Accounts.Where((a) => a.Guid == user).SingleOrDefault();
                if (account == null)
                    return null;
                if (!account.Password.SequenceEqual(Password.ToHash(password)))
                    return false;
                session.User = user;
            }
            return true;
        }


        /// <summary>
        /// Determines if the specified session is logged in.
        /// </summary>
        public bool HasStandardPrivileges(Session session)
        {
            using (var db = OpenContext())
                return db.Accounts.Where((a) => a.Guid == session.User).SingleOrDefault() != null;
        }

        /// <summary>
        /// Determines if the specified session is associated with office privileges.
        /// </summary>
        public bool HasAdvancedPrivileges(GDMDBDataContext db, Session session)
        {
            var user = db.Accounts.Where((a) => a.Guid == session.User).SingleOrDefault();
            if (user == null) return false;
            return user.Office;
        }

        /// <summary>
        /// Recalls a session by it's guid.
        /// If the session does not exist or is expired, a new one is created, else the expiration time of the session is postponed.
        /// </summary>
        public Session RecallSession(Guid? guid, TimeSpan lifespan)
        {
            using (var db = OpenContext()) {
                Session session = (guid == null ? null : db.Sessions.SingleOrDefault((s) => s.Guid == guid));

                if (session != null) {
                    db.Sessions.Context.Refresh(RefreshMode.OverwriteCurrentValues, session);
                    if (session.Expiration < DateTime.UtcNow) // if the session is expired, ignore it, else renew its lifespan.
                        session = null;
                    else
                        session.Expiration = DateTime.UtcNow + lifespan;
                }

                if (session == null) {
                    session = new Session() {
                        Predecessor = guid,
                        Created = DateTime.UtcNow,
                        Expiration = DateTime.UtcNow + lifespan
                    };
                    db.Sessions.InsertOnSubmit(session);
                }

                return session;
            }
        }


        #endregion


        #region "Reports"

        /// <summary>
        /// Submits a new report for the user or updates the report if it already exists.
        /// </summary>
        public void SubmitReport(Session session, GDM.Report report)
        {


            using (var db = OpenContext()) {
                var oldReport = (report.Guid == null ? null : db.Reports.Where((r) => r.Guid == report.Guid).SingleOrDefault());

                // todo: require office role for updates
                var newReport = new Report() {
                    Guid = Guid.NewGuid(),
                    Submitted = DateTime.UtcNow,
                    Project = report.ProjectGuid.Value,
                    Date = report.Date,
                    Author = (oldReport == null ? session.User : oldReport.Author),
                    Notes = report.Notes,
                    UpdatedVersion = null
                };

                if (oldReport != null) oldReport.UpdatedVersion = newReport;

                db.Reports.InsertOnSubmit(newReport);

                db.Engagements.InsertAllOnSubmit(from e in report.Engagements
                                              select new Engagement() {
                                                  Report = newReport,
                                                  Contact = e.Contact,
                                                  Start = e.Start.Ticks,
                                                  End = e.End.Ticks
                                              });

                db.ItemUsages.InsertAllOnSubmit(from i in report.Items
                                             select new ItemUsage() {
                                                 Report = newReport,
                                                 Item = i.Item,
                                                 Quantity = i.Quantity
                                             });
            }
        }

        /// <summary>
        /// Returns the reports submitted by the specified user within the specified UTC time window.
        /// If the session has office privileges, "user" can be null and will yield reports from all users.
        /// </summary>
        public IEnumerable<GDM.Report> GetReports(Session session, DateTime earliest, DateTime latest)
        {
            using (var db = OpenContext()) {
                var query = db.Reports.Where((r) => (r.Submitted >= earliest) && (r.Submitted <= latest) && (r.UpdatedVersion == null));
                if (!HasAdvancedPrivileges(db, session)) query = query.Where((r) => (r.Author == session.User));
                return query.ToArray().Select((r) => new GDM.Report() {
                    OnServer = true,
                    Guid = r.Guid,
                    ProjectGuid = r.Project,
                    Date = r.Date,
                    AuthorGuid = r.Author,
                    Engagements = new CollectionSource<GDM.Engagement>(db.Engagements.Where((e) => e.Report.Guid == r.Guid).ToArray().Select((e) => (GDM.Engagement)(e))),
                    Items = new CollectionSource<GDM.ItemUsage>(db.ItemUsages.Where((i) => i.Report.Guid == r.Guid).ToArray().Select((i) => (GDM.ItemUsage)(i))),
                    WorkDescription = r.WorkDescription,
                    Notes = r.Notes
                }).ToArray();
            }
        }

        #endregion

    }
}
