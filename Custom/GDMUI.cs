using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppInstall;
using AppInstall.OS;
using AppInstall.UI;
using AppInstall.Framework;
using AppInstall.Networking;

namespace GDM
{
    /// <summary>
    /// Contains all code that defines the common UI on all platforms
    /// </summary>
    public static class UI
    {

        private class TestClass
        {
            public string str;
            public int count;
        }


        public static void Launch(GDMDBClient client)
        {
            Platform.InvokeMainThread(() => {
                Window win = null;

                var errorDisplay = new DialogFeature<Exception>() {
                    Text = "Details anzeigen",
                    ViewConstructor = (ex) => new EditorViewController<string>() {
                        Title = "Fehlerdetails",
                        Data = new DataSource<string>(ex.ToString()),
                        Fields = new Field<string>[] {
                            new LargeTextFieldView<string>("Fehlerdetails", (str) => new FieldSource<string>(() => str)),
                        },
                    },
                    DismissText = "whatever",
                    ParentConstructor = () => win
                };


                var loginDisplay = new EditorViewController<GDMDBClient.LoginCredentials>() {
                    Title = "Fehlerdetails",
                    Data = client.CurrentLogin,
                    Fields = new Field<GDMDBClient.LoginCredentials>[] {
                        new PasswordFieldView<GDMDBClient.LoginCredentials>("Kennwort", (login) => new FieldSource<string>(() => login.Password, (str) => login.Password = str)),
                    },
                    SubmitText = "Login",
                    SubmissionErrorMessageFactory = (ex) => {
                        var serverEx = ex as ServerSideException;
                        var httpEx = (serverEx == null ? null : serverEx.InnerException) as HTTP.HTTPException;
                        if (httpEx != null) if (httpEx.StatusCode == HTTP.StatusCodes.Forbidden) return "falsches Kennwort";
                        return "Fehler beim Login";
                    },
                    ExceptionDisplayCommand = errorDisplay,
                };


                client.AuthenticationRequired += () => {
                    new Dialog(win, loginDisplay).Show();
                };


                var dummyProject = new Project() { Name = "unspecified project", Guid = new Guid() };
                var dummyUser = new Contact() { FullName = "unknown user", Guid = new Guid() };


                var mainView = new MultiViewController() {
                    Title = "GDM System",
                    Modality = ViewModality.Compact,
                    Subviews = new ViewController[] {
                        new ListViewController<Report>() {
                            Title = "Rapporte",
                            Data = client.Reports,
                            //CategoryNameFactory = (r) => "projekte mit " + r.Project.ToString()[0],
                            CategoryNameFactory = (r) => (r.OnServer ? "Gespeicherte Rapporte" : "Entwürfe"),
                            Fields = new Field<Report>[] {
                                new TextFieldView<Report>("Projekt", r => r.Project.Or(dummyProject).Name, true),
                                new TextFieldView<Report>("Leiter", r => r.Author.Or(dummyUser).FullName, false),
                                new TextFieldView<Report>("Datum", r => r.Date.ToShortDateString(), false),
                                /*new DateField<Report>("Datum", (r) => r.Date, (r, d) => r.Date = d)*/
                            },
                            AddText = "Neuen Rapport erstellen",
                            RefreshText = Platform.Type == PlatformType.iOS ? "Ziehen zum Aktualisieren" : "aktualisieren",
                            RefreshingText = "aktualisieren...",
                            RefreshErrorMessageFactory = (ex) => "Fehler beim Aktualisieren",
                            RefreshSuccessMessageFactory = (date) => "Letzte Aktualisierung: " + (date.HasValue ? date.Value.ToShortTimeString() : "nie"),
                            ExceptionDisplayCommand = errorDisplay,
                            PlaceholderText = "kein Rapport ausgewählt",
                            DetailViewConstructor = (parent, report, isNew) => new MultiViewController() {
                                Title = (isNew ? "Neuer Rapport" : "Rapport Bearbeiten"),
                                Modality = ViewModality.Expanded,
                                Layout = "0 1",
                                Data = client.CreateReportDataSource(report),
                                SubmitText = "abschicken",
                                SubmittingText = "abschicken...",
                                SubmissionErrorMessageFactory = (ex) => "Fehler beim Speichern",
                                SubmissionSuccessMessageFactory = (date) => "Zuletzt gespeichert: " + (date.HasValue ? date.Value.ToShortTimeString() : "nie"),
                                ExceptionDisplayCommand = errorDisplay,
                                Subviews = new ViewController[] {
                                    new EditorViewController<Report>() {
                                        Title = "Allgemein",
                                        Data = new DataSource<Report>(report),
                                        Fields = new Field<Report>[] {
                                            new TextFieldView<Report>("Projekt", r => r.Project.Or(dummyProject).Name, true),
                                            new DateFieldView<Report>("Datum", r => new FieldSource<DateTime>(() => r.Date, d => { r.Date = d; parent.DidUpdate(r); }))
                                        }
                                    },
                                    new ListViewController<Engagement>() {
                                        Title = "Einsätze",
                                        Data = report.Engagements,
                                        Fields = new Field<Engagement>[] {
                                            new TextFieldView<Engagement>("Name", engagement => client.Contacts[engagement.Contact].Or(dummyUser).FullName, true),
                                            new TimeRangeFieldView<Engagement>("Arbeitszeit", "Start", "bis", "Ende", engagement => new TimeRange(new FieldSource<Tuple<TimeSpan, TimeSpan>>(() => new Tuple<TimeSpan, TimeSpan>(engagement.Start , engagement.End), (t) => { engagement.Start = t.Item1; engagement.End = t.Item2; }))),
                                            new TimeFieldView<Engagement>("Pause", engagement => new FieldSource<TimeSpan>(() => engagement.Break, t => engagement.Break = t), engagement => false),
                                            new TimeFieldView<Engagement>("Weg", engagement => new FieldSource<TimeSpan>(() => engagement.Travel, t => engagement.Travel = t), engagement => false),
                                        },
                                        Features = new List<FeatureController>() {
                                            new DialogFeature<MappedSource<Engagement, Contact>>(() => new MappedSource<Engagement, Contact>(report.Engagements, (e) => client.Contacts[e.Contact], (c) => Engagement.FromNow(c))) {
                                                Text = "Arbeiter auswählen",
                                                ViewConstructor = (selectedContacts) => new ListViewController<Contact>() {
                                                    Title = "Arbeiter auswählen",
                                                    Data = client.Contacts,
                                                    Fields = new Field<Contact>[] {
                                                        new BoolFieldView<Contact>("Ausgewählt", (c) => new FieldSource<bool>(() => selectedContacts.Contains(c), (sel) => selectedContacts.SetIncluded(c, sel))),
                                                        new TextFieldView<Contact>("Name", (c) => c.FullName, true),
                                                    },
                                                    RefreshText = Platform.Type == PlatformType.iOS ? "Ziehen zum Aktualisieren" : "aktualisieren",
                                                    RefreshingText = "aktualisieren...",
                                                    RefreshErrorMessageFactory = (ex) => "Fehler beim Aktualisieren",
                                                    RefreshSuccessMessageFactory = (date) => "Letzte Aktualisierung: " + (date.HasValue ? date.Value.ToShortTimeString() : "nie"),
                                                    ExceptionDisplayCommand = errorDisplay,
                                                },
                                                DismissText = "OK",
                                                DismissAction = (selectedContacts) => selectedContacts.Commit(),
                                                ParentConstructor = () => win
                                            }
                                        }
                                    },
                                    new ListViewController<ItemUsage>() {
                                        Title = "Material",
                                        Data = report.Items,
                                        Fields = new Field<ItemUsage>[] {
                                            new TextFieldView<ItemUsage>("Bezeichnung", (i) => client.Items[i.Item].Name, true),
                                            new IntegerFieldView<ItemUsage>("Menge", (i) => new FieldSource<int>(() => i.Quantity, (q) => i.Quantity = q), (i) => false
                                                 // todo: proper text display
                                            )
                                        },
                                        Features = new List<FeatureController>() {
                                            new DialogFeature<MappedSource<ItemUsage, Item>>(() => new MappedSource<ItemUsage, Item>(report.Items, (i) => client.Items[i.Item], (i) => new ItemUsage() { Item = i.Guid, Quantity = 1 })) {
                                                Text = "Material auswählen",
                                                ViewConstructor = (selectedItems) => new TreeViewController<ItemFolder, Item>() {
                                                    Title = "Material auswählen",
                                                    Data = client.Items,
                                                    FolderFields = new Field<ItemFolder>[] {
                                                        new TextFieldView<ItemFolder>("Bezeichnung", (item) => item.Name, true),
                                                    },
                                                    ItemFields = new Field<Item>[] {
                                                        new BoolFieldView<Item>("Ausgewählt", (item) => new FieldSource<bool>(() => selectedItems.Contains(item), (sel) => selectedItems.SetIncluded(item, sel))),
                                                        new TextFieldView<Item>("Bezeichnung", (item) => item.Name, true),
                                                        new TextFieldView<Item>("Einheit", (item) => (item.Unit == null ? "" : item.Unit.Name), false),
                                                    },
                                                    RefreshText = Platform.Type == PlatformType.iOS ? "Ziehen zum Aktualisieren" : "aktualisieren",
                                                    RefreshingText = "aktualisieren...",
                                                    RefreshErrorMessageFactory = (ex) => "Fehler beim Aktualisieren",
                                                    RefreshSuccessMessageFactory = (date) => "Letzte Aktualisierung: " + (date.HasValue ? date.Value.ToShortTimeString() : "nie"),
                                                    ExceptionDisplayCommand = errorDisplay,
                                                },
                                                DismissText = "OK",
                                                DismissAction = (selectedItems) => selectedItems.Commit(),
                                                ParentConstructor = () => win
                                            }
                                        }
                                    },
                                    new EditorViewController<Report>() {
                                        Title = "[blabla]",
                                        Data = new DataSource<Report>(report),
                                        Fields = new Field<Report>[] {
                                            new LargeTextFieldView<Report>("Tätigkeit", r => new FieldSource<string>(() => r.WorkDescription, str => r.WorkDescription = str)),
                                            new LargeTextFieldView<Report>("Bemerkungen", r => new FieldSource<string>(() => r.Notes, str => r.Notes = str)),
                                        },
                                    },
                                    new ListViewController<Map>() {
                                        Title = "Pläne",
                                        Data = report.Maps,
                                        Fields = new Field<Map>[] {
                                            new TextFieldView<Map>("Bezeichnung", map => map.Name, true),
                                        },
                                        Features = new List<FeatureController>() {
                                            new DialogFeature<Map>(() => new Map()) {
                                                Text = "[plan xyz]",
                                                ViewConstructor = (map) => new CustomViewController<Map>() {
                                                    Title = "[plan xyz]",
                                                    Data = new DataSource<Map>(map),
                                                    ViewConstructor = () => new NewCanvas() { Source = "test.pdf" }
                                                },
                                                DismissText = "OK",
                                                DismissAction = null, // todo
                                                ParentConstructor = () => win
                                            }
                                        }
                                    },
                                }
                            }
                        }
                    }
                };


                Random random = new Random();


                var testlistData = new CollectionSource<TestClass>((cancellationToken) => {
                    Task.Delay(2000).Wait(cancellationToken);


                    try {
                        if (random.NextDouble() > 0f)
                            throw new Exception("owned");
                    } catch (Exception ex) {
                        throw new AggregateException("more pwnage", ex);
                    }



                    return new TestClass[] {
                                            new TestClass() { str = "lo" , count = 3 },
                                            new TestClass() { str = "ha", count = 5 }
                        };
                }, null, true, null) {
                    ItemConstructor = () => new TestClass() { str = "bla", count = 1 },
                    CanDeleteItems = true,
                    CanMoveItems = true
                };

                var testlist = new ListViewController<TestClass>() {
                                    Title = "interactive list",
                                    Data = testlistData,
                                    //CategoryNameFactory = (r) => "projekte mit " + r.Project.ToString()[0],
                                    CategoryNameFactory = (s) => new string((s.str.Any() ? s.str.First() : '?'), 1),
                                    Fields = new Field<TestClass>[] {
                                                new TextFieldView<TestClass>("Wert", (s) => s.str, true),
                                                new TextFieldView<TestClass>("Länge", (s) => s.count.ToString(), false)
                                            },
                                    AddText = "Neuen Rapport erstellen",
                                    RefreshText = "ziehen zum aktualisieren",
                                    RefreshingText = "aktualisieren...",
                                    RefreshErrorMessageFactory = (ex) => "Fehler beim Aktualisieren",
                                    RefreshSuccessMessageFactory = (date) => "Letzte Aktualisierung: " + (date.HasValue ? date.Value.ToShortTimeString() : "nie"),
                                    ExceptionDisplayCommand = errorDisplay,
                                    PlaceholderText = "kein str ausgewählt",
                                    DetailViewConstructor = (parent, val, isNew) => new EditorViewController<TestClass>() {
                                        Title = (isNew ? "Neuer String" : "String Bearbeiten"),
                                        SubmitText = "abschicken",
                                        SubmissionErrorMessageFactory = (ex) => "Fehler beim Speichern",
                                        ExceptionDisplayCommand = errorDisplay,
                                        Data = new DataSource<TestClass>(val),
                                        Fields = new Field<TestClass>[] {
                                                new SmallTextFieldView<TestClass>("Wert", (s) => new FieldSource<string>(() => val.str, (newStr) => { val.str = newStr; parent.DidUpdate(val); })),
                                                new IntegerFieldView<TestClass>("Repeat", (s) => new FieldSource<int>(() => val.count, (newCount) => { val.count = newCount; parent.DidUpdate(val); }), (v) => false) {
                                                    TextConstructor = (v) => {
                                                        if (v.count == 1)
                                                            return "repeat once";
                                                        else if (v.count == 2)
                                                            return "repeat twice";
                                                        else
                                                            return "repeat " + v.count + " times";
                                                        }
                                                }
                                        }
                                    }
                                };

                var test = new MultiViewController() {
                    Title = "Main Test View",
                    Modality = ViewModality.Compact,
                    Subviews = new ViewController[] {
                        new MultiViewController() {
                            Title = "Test View 1",
                            Modality = ViewModality.Expanded,
                            Layout = "0 1",
                            Subviews = new ViewController[] {
                                testlist,
                                new ListViewController<string>() {
                                    Title = "Test List 2",
                                    Data = new CollectionSource<string>(new string[] { "hello 2" , "world 2!", "bla", "ble", "bli", "blo", "blu" }),
                                    //CategoryNameFactory = (r) => "projekte mit " + r.Project.ToString()[0],
                                    CategoryNameFactory = (s) => new string(s.First(), 1),
                                    Fields = new Field<string>[] {
                                                new TextFieldView<string>("Wert", (s) => s, true),
                                                new TextFieldView<string>("Länge", (s) => s.Length.ToString(), false)
                                            },
                                    AddText = "Neuen Rapport erstellen",
                                    RefreshText = "aktualisieren",
                                    RefreshErrorMessageFactory = (ex) => "Fehler beim Aktualisieren",
                                    ExceptionDisplayCommand = errorDisplay,
                                    PlaceholderText = "kein str ausgewählt",
                                    DetailViewConstructor = null
                                }
                            }
                        },
                        //testlist,
                        new ListViewController<string>() {
                            Title = "Test List 4",
                            Data = new CollectionSource<string>(new string[] { "hello 2" , "world 2!", "bla", "ble", "bli", "blo", "blu" }) { CanDeleteItems = true, CanMoveItems = true },
                            //CategoryNameFactory = (r) => "projekte mit " + r.Project.ToString()[0],
                            CategoryNameFactory = (s) => new string(s.First(), 1),
                            Fields = new Field<string>[] {
                                        new TextFieldView<string>("Wert", (s) => s, true),
                                        new TextFieldView<string>("Länge", (s) => s.Length.ToString(), false)
                                    },
                            AddText = "Neuen Rapport erstellen",
                            RefreshText = "aktualisieren",
                            RefreshErrorMessageFactory = (ex) => "Fehler beim Aktualisieren",
                            ExceptionDisplayCommand = errorDisplay,
                            PlaceholderText = "kein str ausgewählt",
                            DetailViewConstructor = null
                        }
                    }
                };




                Platform.DefaultLog.Log("creating window...");
                win = new Window(mainView);
                win.Show();
                win.Closed += () => ApplicationControl.Shutdown();

                new Task(() => {
                    while (!ApplicationControl.ShutdownToken.WaitHandle.WaitOne(10000)) {
                        Platform.DefaultLog.Log("UI dump:");
                        win.DumpLayout(Platform.DefaultLog);
                    }
                }).Start();
            });
        }

    }
}
