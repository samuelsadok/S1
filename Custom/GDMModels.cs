using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AppInstall.Framework;

namespace GDM
{

    /// <summary>
    /// Represents an entity of some type that was retrieved from the server or is intended to be stored on the server.
    /// </summary>
    public abstract class GDMEntity
    {
        /// <summary>
        /// Must be set to the GDM DB client that retrieved this object.
        /// </summary>
        [XmlIgnore()]
        public GDMDBClient Client { get; set; }

        /// <summary>
        /// The unique identifier of this entity as used in the databases.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Compares two entities by their Guid.
        /// </summary>
        public sealed override bool Equals(object obj)
        {
            var cast = obj as GDMEntity;
            if (cast == null) return false;
            return this.Guid.Equals(cast.Guid);
        }

        public sealed override int GetHashCode()
        {
            return this.Guid.GetHashCode();
        }

        /// <summary>
        /// Returns a GDM entity by it's GUID.
        /// </summary>
        /// <param name="guid">The GUID of the entity (if null, the function returns null).</param>
        /// <param name="cached">A reference to a field that caches the entry. If this equals the requested entity, the collection lookup is omitted.</param>
        /// <param name="source">The collection that holds the requested entity.</param>
        protected T GetEntity<T>(Guid? guid, ref T cached, CollectionSource<T> source)
            where T : GDMEntity
        {
            if (guid != (cached == null ? (Guid?)null : cached.Guid))
                cached = guid == null ? (T)null : source.Single(entity => entity.Guid == guid);
            return cached;
        }

        /// <summary>
        /// Returns an entity's GUID in a field while respecting potential null values.
        /// </summary>
        protected Guid? SetEntity<T>(T entity)
            where T : GDMEntity
        {
            return entity == null ? (Guid?)null : entity.Guid;
        }
    }


    public class Contact : GDMEntity
    {
        public string FullName { get; set; }
    }

    public class Engagement
    {
        public Guid Contact { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public TimeSpan Travel { get; set; }
        public TimeSpan Break { get; set; }

        public static Engagement FromNow(Contact contact)
        {
            var now = DateTime.Now;
            return new Engagement() {
                Contact = contact.Guid,
                Start = new TimeSpan(Math.Min(now.Hour, 9), 0, 0),
                End = new TimeSpan(now.Hour, 0, 0)
            };
        }
    }

    public class ItemFolder : GDMEntity
    {
        public string Name { get; set; }
        public ItemFolder[] Subfolders { get; set; }
        public Item[] Items { get; set; }


        /// <summary>
        /// Recursively updates the Client reference in every item.
        /// </summary>
        public void UpdateClient(GDMDBClient client)
        {
            Client = client;

            if (Items != null)
                foreach (var i in Items)
                    i.Client = client;

            if (Subfolders != null)
                foreach (var f in Subfolders)
                    f.UpdateClient(client);
        }
    }

    public class Item : GDMEntity
    {
        public string Name { get; set; }

        [XmlIgnore()]
        public Unit Unit { get { return GetEntity(UnitGuid, ref cachedUnit, Client.Units); } set { UnitGuid = SetEntity(value); } }
        [XmlIgnore()]
        private Unit cachedUnit = null;
        public Guid? UnitGuid { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ItemUsage 
    {
        public Guid Item { get; set; }
        public int Quantity { get; set; }
    }

    public class Unit : GDMEntity
    {
        public string Name { get; set; }
        public bool FloatingPoint { get; set; }
    }

    public class Report : GDMEntity
    {

        /// <summary>
        /// True: this report is stored on the server.
        /// False: this report is only available locally.
        /// </summary>
        public bool OnServer { get; set; }

        [XmlIgnore()]
        public Project Project { get { return GetEntity(ProjectGuid, ref cachedProject, Client.Projects); } set { ProjectGuid = SetEntity(value); } }
        [XmlIgnore()]
        private Project cachedProject = null;
        public Guid? ProjectGuid { get; set; }

        [XmlIgnore()]
        public Contact Author { get { return GetEntity(AuthorGuid, ref cachedAuthor, Client.Contacts); } set { AuthorGuid = SetEntity(value); } }
        [XmlIgnore()]
        private Contact cachedAuthor = null;
        public Guid? AuthorGuid { get; set; }

        public DateTime Date { get; set; }
        public CollectionSource<Engagement> Engagements { get; set; }
        public CollectionSource<ItemUsage> Items { get; set; }
        public string WorkDescription { get; set; }
        public string Notes { get; set; }
        public CollectionSource<Map> Maps { get; set; }
        public CollectionSource<Photo> Photos { get; set; }
    }

    public class Project : GDMEntity
    {
        public string Name { get; set; }
    }

    public class Map : GDMEntity
    {
        public string Name { get; set; }
        public DataSource<byte[]> Data { get; set; }
    }

    public class Photo : GDMEntity
    {

    }
}
