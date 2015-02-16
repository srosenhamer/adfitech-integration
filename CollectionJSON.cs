namespace CollectionJSON
{
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    ///     The root object wrapper for JSON serialization/deserialization.
    /// </summary>
    [DataContract]
    public class CollectionWrapper : System.IDisposable
    {
        [DataMember]
        public Collection collection;

        public CollectionWrapper() { }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        ~CollectionWrapper() { Dispose(false); }
        protected virtual void Dispose(bool disposing)
        {
            collection.Dispose();
        }

    }

    /// <summary>
    ///     The "collection" object. i.e., "collection": {...}
    /// </summary>
    [DataContract]
    public class Collection : System.IDisposable
    {
        [DataMember]
        public Error error;

        [DataMember]
        public Template template;

        [DataMember]
        public List<Item> items { get; set; }

        [DataMember]
        public List<Link> links { get; set; }

        public Collection() { }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        ~Collection() { Dispose(false); }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(error != null) error.Dispose();
                if(template != null) template.Dispose();
                if (items != null)
                {
                    items.ForEach(delegate(Item _item) { _item.Dispose(); });
                    items = null;
                }
                if (links != null)
                {
                    links.ForEach(delegate(Link _link) { _link.Dispose(); });
                    links = null;
                }
            }
        }

    }

    /// <summary>
    ///     The "items" array. i.e., "items": [{"href": "url", "data": [{...}, ...]}, ...]
    /// </summary>
    [DataContract]
    public class Item : System.IDisposable
    {
        [DataMember]
        public string href;

        [DataMember(EmitDefaultValue = false)]
        public List<Datum> data { get; set; }

        [DataMember]
        public List<Link> links { get; set; }

        public Item() { }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        ~Item() { Dispose(false); }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                href = null;
                if (data != null)
                {
                    data.ForEach(delegate(Datum _data) { _data.Dispose(); });
                    data = null;
                }
                if (links != null)
                {
                    links.ForEach(delegate(Link _link) { _link.Dispose(); });
                    links = null;
                }
            }
        }

    }

    /// <summary>
    ///     The "links" array. i.e., "links": [{"rel": "name", "href": "url"}, ...]
    /// </summary>
    [DataContract]
    public class Link : System.IDisposable
    {
        [DataMember]
        public string rel;

        [DataMember]
        public string href;

        public Link() { }
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        ~Link() { Dispose(false); }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                rel = null;
                href = null;
            }
        }
    }

    /// <summary>
    ///     The "error" object. i.e., "error": {"title": "Error Title", 
    ///                                         "code": "Error Code", 
    ///                                         "message": "helpful message"}
    /// </summary>
    [DataContract]
    public class Error : System.IDisposable
    {
        [DataMember]
        public string title;

        [DataMember]
        public string code;

        [DataMember]
        public string message;

        public Error() { }

        public Error(string arg_title, string arg_code, string arg_message)
        {
            this.title = arg_title;
            this.code = arg_code;
            this.message = arg_message;
        }
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        ~Error() { Dispose(false); }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                title = null;
                code = null;
                message = null;
            }
        }
    }


    /// <summary>
    ///     The "template" wrapper object. i.e., "template": {}
    /// </summary>
    [DataContract]
    public class TemplateWrapper : System.IDisposable
    {
        [DataMember]
        public Template template;

        public TemplateWrapper() { this.template = new Template(); }
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        ~TemplateWrapper() { Dispose(false); }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (template != null)
                {
                    template.Dispose();
                    template = null;
                }
            }
        }
    }

    /// <summary>
    ///     The "data" array. i.e., "data": [{"name":"abc","value":"def"},
    ///                                      {"name":"123","value":"456"},
    ///                                      ...]
    /// </summary>
    [DataContract]
    public class Template : System.IDisposable
    {
        [DataMember(EmitDefaultValue = false)]
        public List<Datum> data { get; set; }

        // this OnSerializing hook omits any name/value pairs whose value is null
        [OnSerializing]
        void PrepareForSerialization(StreamingContext sc)
        {
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].value == null)
                    data.RemoveAt(i);
            }
        }

        public Template() { }

                public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        ~Template() { Dispose(false); }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (data != null)
                {
                    data.ForEach(delegate(Datum _data) { _data.Dispose(); });
                    data = null;
                }
            }
        }

    }

    /// <summary>
    ///     The "data" object. 
    ///     either: {"name":"abc", "value":"def"}
    ///     or:     {"name":"abc", "value": ["def", "ghi"]}
    /// </summary>
    [DataContract]
    [KnownType(typeof(List<string>))]
    public class Datum : System.IDisposable
    {
        [DataMember]
        public string name;

        [DataMember]
        public object value { get; set; }

        public Datum() { }

        public Datum(string arg_name, string arg_value)
        {
            this.name = (string)arg_name;
            this.value = arg_value;
        }
        
        public Datum(string arg_name, List<string> arg_value)
        {
            this.name = arg_name;
            this.value = arg_value;
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        ~Datum() { Dispose(false); }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                name = null;
                value = null;
            }
        }
    }
}