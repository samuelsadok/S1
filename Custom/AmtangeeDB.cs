using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using AppInstall.Framework;

namespace GDM
{
    public class AmtangeeDBDataContext : AutoSubmitDataContext
    {
        public Table<AmtangeeDB.ContactCategory> ContactCategories { get { return GetTable<AmtangeeDB.ContactCategory>(); } }
        public Table<AmtangeeDB.Contact> Contacts { get { return GetTable<AmtangeeDB.Contact>(); } }
        public Table<AmtangeeDB.ItemCategory> ItemCategories { get { return GetTable<AmtangeeDB.ItemCategory>(); } }
        public Table<AmtangeeDB.Item> Items { get { return GetTable<AmtangeeDB.Item>(); } }
        public Table<AmtangeeDB.Unit> Units { get { return GetTable<AmtangeeDB.Unit>(); } }
    }


    public class AmtangeeDB : Database<AmtangeeDBDataContext>
    {

        private static readonly Guid empty = new Guid();

        #region "DB Abstraction Classes"


        [Table(Name = "ContactsCategories")]
        public class ContactCategory
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public Guid Guid { get; private set; }

            [Column(CanBeNull = false, IsDbGenerated = true)]
            public Guid Parent { get; set; }

            [Column(CanBeNull = false)]
            public Guid Owner { get; set; } // the user who owns this folder (or null if it is public)

            [Column(CanBeNull = false)]
            public string Name { get; set; }
        }


        [Table(Name = "Contacts")]
        public class Contact
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public Guid Guid { get; private set; }

            [Column(CanBeNull = false)]
            public Guid CreatedBy { get; set; }

            [Column(CanBeNull = false)]
            public Guid Category { get; set; }

            [Column(CanBeNull = false)]
            public Guid Parent { get; set; }

            [Column(CanBeNull = false, IsDbGenerated = true)]
            public short Type { get; set; }

            [Column(CanBeNull = false, IsDbGenerated = true)]
            public bool Active { get; set; }

            [Column(CanBeNull = false, IsDbGenerated = true)]
            public bool Show { get; set; }

            [Column(CanBeNull = false, IsDbGenerated = true)]
            public bool Deleted { get; set; }

            [Column(CanBeNull = true)]
            public Guid? AssignedTo { get; set; }

            [Column(CanBeNull = true)]
            public Guid? Salutation { get; set; }

            [Column(CanBeNull = true)]
            public Guid? Title { get; set; }

            [Column(CanBeNull = true)]
            public string Name { get; set; }

            [Column(CanBeNull = true)]
            public string Name2 { get; set; }

            [Column(CanBeNull = true)]
            public string Name3 { get; set; }

            public override string ToString()
            {
                return string.Join(" ", (from n in new string[] { Name2, Name, Name3 } where !string.IsNullOrWhiteSpace(n) select n.Trim()));
            }


            public static explicit operator GDM.Contact(Contact c)
            {
                return new GDM.Contact() { Guid = c.Guid, FullName = c.ToString() };
            }
        }


        [Table(Name = "ItemsCategories")]
        public class ItemCategory
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public Guid Guid { get; private set; }

            [Column(CanBeNull = false, IsDbGenerated = true)]
            public Guid Parent { get; set; }

            [Column(CanBeNull = false)]
            public string Name { get; set; }
        }


        [Table(Name = "Items")]
        public class Item
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public Guid Guid { get; private set; }

            [Column(CanBeNull = false, IsDbGenerated = true)]
            public Guid Category { get; set; }

            [Column(CanBeNull = true)]
            public string Description { get; set; }

            [Column(CanBeNull = true)]
            public Guid? Unit { get; set; }

            [Column(CanBeNull = true)]
            public Guid? PurchasingCurrency { get; set; }

            [Column(CanBeNull = true)]
            public double? PurchasingPrice { get; set; }

            [Column(CanBeNull = true)]
            public Guid? VATType { get; set; }


            public static explicit operator GDM.Item(Item i)
            {
                return new GDM.Item() { Guid = i.Guid, Name = i.Description, UnitGuid = i.Unit };
            }
        }


        [Table(Name = "Units")]
        public class Unit
        {
            [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
            public Guid Guid { get; private set; }

            [Column(CanBeNull = false)]
            public string Name { get; set; }


            public static explicit operator GDM.Unit(Unit u)
            {
                return new GDM.Unit() { Guid = u.Guid, Name = u.Name, FloatingPoint = false };
            }
        }


        #endregion


        public override string TableName { get { return "AMTANGEE"; } }




        #region "Contacts"

        private IEnumerable<ContactCategory> GetContactCategories(AmtangeeDBDataContext db, Guid parent)
        {
            return (from c in db.ContactCategories where (c.Parent == parent) && (c.Owner == empty) select c);
        }

        /// <summary>
        /// Returns all child categories
        /// </summary>
        public IEnumerable<ContactCategory> GetContactCategories(AmtangeeDBDataContext db, ContactCategory parent)
        {
            return GetContactCategories(db, parent.Guid);
        }

        /// <summary>
        /// Returns all root categories
        /// </summary>
        public IEnumerable<ContactCategory> GetContactCategories(AmtangeeDBDataContext db)
        {
            return GetContactCategories(db, new Guid());
        }

        /// <summary>
        /// Returns all contacts in the specified category.
        /// </summary>
        /// <param name="recursive">When set, all subcategories are also scanned</param>
        public IEnumerable<GDM.Contact> GetContacts(Guid category, bool recursive)
        {
            using (var db = OpenContext()) {
                var result = (from c in db.Contacts where (c.Category == category) && (c.Type == 0) && c.Active && c.Show && !c.Deleted orderby c.Name select (GDM.Contact)c);
                if (recursive)
                    foreach (var c in GetContactCategories(db, category))
                        result.Concat(GetContacts(c.Guid, true));
                return result.ToArray();
            }
        }

        #endregion



        #region "Items"

        /// <summary>
        /// Returns all items and subcategories in the specified category.
        /// </summary>
        /// <param name="recursive">When not set, only the guids and names of subcategories are returned</param>
        public void GetItems(ItemFolder folder, bool recursive)
        {
            using (var db = OpenContext())
                GetItems(db, folder, recursive);
        }

        private void GetItems(AmtangeeDBDataContext db, ItemFolder folder, bool recursive)
        {
            folder.Items = (from i in db.Items where (i.Category == folder.Guid) select (GDM.Item)i).ToArray(); // todo: order (amtangee 5)
            folder.Subfolders = (from c in db.ItemCategories where (c.Parent == folder.Guid) orderby c.Name select c).ToArray().Select((c) => new ItemFolder() { Guid = c.Guid, Name = c.Name }).ToArray();

            if (recursive)
                foreach (var f in folder.Subfolders)
                    GetItems(db, f, true);
        }

        /// <summary>
        /// Returns all units.
        /// </summary>
        public GDM.Unit[] GetUnits()
        {
            using (var db = OpenContext())
                return db.Units.Select((u) => (GDM.Unit)u).ToArray();
        }

        #endregion

    }
}
