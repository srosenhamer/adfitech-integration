namespace Adfitech
{
    using System.Collections.Generic;
    using System.Runtime.Serialization.Json;
    using CollectionJSON;

    class Integration
    {
        private const string STAGING_ENDPOINT = "https://api.staging.adfitech.com";
        private const string PRODUCTION_ENDPOINT = "https://api.adfitech.com";
        private const string LOANS_PATH = "loans";
        private const string REVIEWS_PATH = "reviews";
        private const string AI_PATH = "ai";
        private const string IMAGES_PATH = "loan_images";
        private const string PDF = "application/pdf";
        private const string FILE_NOT_READY = "created";

        private string endpoint;
        private string env;
        public string environment
        {
            get { return env; }
            set
            {
                env = value;
                this.endpoint = (env == "production" ? PRODUCTION_ENDPOINT :
                                                       STAGING_ENDPOINT);
            }
        }
        private string request_path;

        private string key;
        private int key_id;

        private System.Net.HttpWebRequest request;

        public bool throw_exceptions = true;
        public bool success;
        public bool empty;
        public List<string> downloaded_files;
        public bool file_received() { return (downloaded_files.Count > 0); }

        public CollectionJSON.Error error;
        public List<CollectionJSON.Item> items;
        public CollectionJSON.Item item;
        private CollectionJSON.TemplateWrapper template_wrapper;

        /// <summary>
        ///    Adfitech.Integration default constructor.
        /// </summary>
        /// <param name="key">The api key provided by adfitech.</param>
        /// <param name="key_id">The api key id provided by adfitech.</param>
        /// <param name="env">
        ///     A string indicating the target environment, 
        ///     'production' or 'staging'. Default is 'staging'.
        /// </param>
        public Integration(string _key, int _key_id, string _env = "staging") 
        {
            this.key = _key;
            this.key_id = _key_id;
            this.environment = _env;
            this.error = null;
            this.item = null;
            this.items = null;
            this.downloaded_files = new List<string>();

            if (this.environment == "staging")
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }
        }

        /// <summary>
        ///    Adfitech.Integration destructor.
        /// </summary>
        ~Integration()
        {
            if (this.downloaded_files != null && this.downloaded_files.Count > 0)
            {
                this.downloaded_files.ForEach(delegate(string file_path)
                {
                    if(System.IO.File.Exists(file_path))
                        System.IO.File.Delete(file_path);
                });
            }
        }

        /// <summary>
        ///    Initializes the instance request object with necessary values.
        /// </summary>
        /// <param name="method">verb indicating the http method GET/POST/PUT/DELETE</param>
        private void initialize_request(string method = "GET")
        {
            string url = this.request_path.IndexOf("http") < 0 ? 
                this.endpoint + "/" + this.request_path :
                this.request_path;

            this.request = System.Net.WebRequest.Create(url) as System.Net.HttpWebRequest;
            this.request.KeepAlive = true;
            this.request.Method = method;
            this.request.ContentType = "application/vnd.collection+json";
            this.request.Accept = "application/vnd.collection+json";
            this.request.AddAuthorizationHeader(key, key_id); // add HMAC signature
            this.request.Timeout = 3600000;

        }

        /// <summary>
        ///    Processes the response from the integration service.
        /// </summary>
        /// <param name="debug">When set to `true` will simply spill the response body to the console.</param>
        /// <exception cref="Adfitech.ResourceNotFoundException">
        ///     Thrown when the requested resource is not found by the server.
        /// </exception>
        /// <exception cref="Adfitech.IntegrationException">
        ///     Thrown when a JSON formatted error is returned from the server.
        /// </exception>
        /// <exception cref="System.Net.WebException">
        ///     Thrown when an error of that type is detected and unable to be 
        ///     handled by one of the above mentioned exception classes.
        /// </exception>
        private void process_response(bool debug=false)
        {
            try
            {
                using (var response = request.GetResponse() as System.Net.HttpWebResponse)
                {
                    if (debug == true)
                    {
                        if (response.ContentType == PDF)
                            System.Console.WriteLine("PDF received. " + response.ContentLength + " bytes.");
                        else{
                            var str = response.GetResponseStream();
                            var reader = new System.IO.StreamReader(str);
                            System.Console.WriteLine(reader.ReadToEnd());
                        }
                        this.empty = true;
                        return;
                    }
                    if (response.ContentType == PDF)
                    {
                        string file_path = System.IO.Path.GetTempFileName();
                        using (var fstream = System.IO.File.Open(file_path, System.IO.FileMode.Open))
                        {
                            response.GetResponseStream().CopyTo(fstream);
                            this.downloaded_files.Add(file_path);
                            this.empty = false;
                        }
                    }
                    else
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(CollectionWrapper));
                        CollectionWrapper response_wrapper = (CollectionWrapper)ser.ReadObject(response.GetResponseStream());
                        this.items = response_wrapper.collection.items;
                        this.empty = true;
                        if (this.items != null)
                        {
                            if (this.items.Count > 0)
                                this.empty = false;
                            if (this.items.Count == 1)
                                this.item = response_wrapper.collection.items[0];
                        }
                    }
                    this.success = true;
                }
            }
            catch (System.Net.WebException web_e)
            {
                this.success = false;
                using (var resp = web_e.Response as System.Net.WebResponse)
                {
                    if (resp != null)
                    {
                        try
                        {
                            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(CollectionWrapper));
                            CollectionWrapper error_wrapper = (CollectionWrapper)ser.ReadObject(resp.GetResponseStream());
                            this.error = error_wrapper.collection.error;
                            if (this.throw_exceptions)
                            {
                                if (error_wrapper.collection.error.code == "404")
                                    throw new ResourceNotFoundException(request_path + " not found", 
                                        error_wrapper.collection.error);
                                else
                                    throw new IntegrationException("error", error_wrapper.collection.error);
                            }
                        }
                        catch (System.Runtime.Serialization.SerializationException) 
                        {
                            System.Console.WriteLine(web_e.Message + "\n" + web_e.ToString());
                            throw new System.Net.WebException("Non-JSON error received from server", web_e); 
                        }
                    }
                    else { throw; }
                }
            }
            this.request = null;
        }

        /// <summary>
        ///     POSTs a JSON payload to the configured request_path.
        /// </summary>
        private void post_json()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(CollectionJSON.TemplateWrapper));
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            ser.WriteObject(ms, this.template_wrapper);
            var byte_array = ms.ToArray();
            initialize_request("POST");
            using (var writer = this.request.GetRequestStream()) { writer.Write(byte_array, 0, byte_array.Length); }
            process_response();
        }

        /// <summary>
        ///     GETs a list of reviews for a given loan number.
        /// </summary>
        /// <param name="loan_number">The loan number for which to search.</param>
        /// <exception cref="Adfitech.ResourceNotFoundExcepion">
        ///     Thrown when the requested loan is not found.
        /// </exception>
        public void find_reviews(string loan_number)
        {
            this.request_path = REVIEWS_PATH + "?loan_number=" + loan_number;
            initialize_request();
            process_response();
        }

        /// <summary>
        ///     GETs the review resource for the given id
        /// </summary>
        /// <param name="summary_id">The review id to be retrieved.</param>
        /// <exception cref="Adfitech.ResourceNotFoundExcepion">
        ///     Thrown when the requested review is not found.
        /// </exception>
        public void get_review(string summary_id)
        {
            this.request_path = REVIEWS_PATH + "/" + summary_id;
            initialize_request();
            process_response();
        }

        /// <summary>
        ///     GETs the list of documents for a given review id
        /// </summary>
        /// <param name="summary_id">The review id to be looked up.</param>
        /// <exception cref="Adfitech.ResourceNotFoundExcepion">
        ///     Thrown when the requested review is not found.
        /// </exception>
        public void get_document_list(string review_id)
        {
            this.request_path = REVIEWS_PATH + "/" + review_id + "/documents";
            initialize_request();
            process_response();
        }

        /// <summary>
        ///     GETs the list of images that have been generated and are available for
        ///     download.
        ///       when `review_id == null`
        ///         the list will include all availabe generated images for this integration
        ///         account.
        ///       whien `review_id != null`
        ///         the list will include only those generated images that belong to the
        ///         provided review_id
        /// </summary>
        /// <param name="review_id">The id of the review for which a list is desired.</param>
        /// <exception cref="Adfitech.ResourceNotFoundExcepion">
        ///     Thrown when the requested review is not found.
        /// </exception>
        public void get_review_images_list(string review_id=null)
        {
            this.request_path = IMAGES_PATH;
            if (review_id != null)
                this.request_path += "?loan_number=" + review_id;
            initialize_request();
            process_response();
        }

        /// <summary>
        ///     GETs the images resource for the supplied id. This is useful for determining
        ///     the status of a previous request to generate loan images.
        /// </summary>
        /// <param name="images_id">The id of the generated images resource.</param>
        /// <exception cref="Adfitech.ResourceNotFoundExcepion">
        ///     Thrown when the requested review is not found.
        /// </exception>
        public void get_review_images_resource(string images_id=null)
        {
            this.request_path = IMAGES_PATH + "/" + images_id;
            initialize_request();
            process_response();
        }

        /// <summary>
        ///     when `synchronous == true` (default)
        ///       Retrieves an image file for the specified list of documents
        ///     when `synchronous == false`
        ///       Requests the image file to be created for the specified list of documents
        ///       but does not retrieve the generated file.
        /// </summary>
        /// <param name="review_id">The review id to which the documents belong.</param>
        /// <param name="doc_code">This list of documents to be retrieved</param>
        /// <param name="synchronous">
        ///   When true the call will be made in a synchronous fashion. If the request to generate the
        ///   PDF results in a file link being returned, we'll request that file up to `max_sync_tries`
        ///   times assuming the response from the server is `FILE_NOT_READY`. If another status code
        ///   is returned or we exhaust our maximum attemtps, an exception will be raised.
        /// </param>
        /// <exception cref="Adfitech.IntegrationException">
        ///     Thrown when an error occurs creating the loan resource.
        /// </exception>
        /// <exception cref="Adfitech.ResourceNotReadyException">
        ///     Thrown when the requested generated documents are not ready for retrieval.
        /// </exception>
        public void get_review_images(long review_id, List<string> doc_code = null, string stack=null, bool synchronous = true)
        {
            long max_sync_tries = 30;
            long attempt = 0;

            this.request_path = IMAGES_PATH;
            this.template_wrapper = new CollectionJSON.TemplateWrapper();
            this.template_wrapper.template.data = new List<CollectionJSON.Datum> {
                new CollectionJSON.Datum("review_id", review_id.ToString()),
                new CollectionJSON.Datum("documents", doc_code),
                new CollectionJSON.Datum("stack", stack)
            };
            post_json();

            if (!this.empty)
            {
                this.items.ForEach(delegate(CollectionJSON.Item item)
                {
                    item.links.ForEach(delegate(CollectionJSON.Link link){
                        if (link.rel == "file")
                        {
                            if (synchronous)
                            {
                                this.request_path = link.href;
                                string code = FILE_NOT_READY;
                                while (code == FILE_NOT_READY && attempt != max_sync_tries)
                                {
                                    try {
                                        System.Threading.Thread.Sleep(1000);
                                        initialize_request();
                                        process_response();
                                        code = "done";
                                    }
                                    catch (IntegrationException e)
                                    {
                                        if (e.error.code == FILE_NOT_READY)
                                            attempt++;
                                    }
                                }
                                if (code == FILE_NOT_READY)
                                {
                                    throw new ResourceNotReadyException("The requested file could not be retrieved after " + max_sync_tries + " tries");
                                }
                            }
                        }
                    });
                });
            }
        }

        /// <summary>
        ///     Retrieves an image file at the specified location
        /// </summary>
        /// <param name="url">The url of resource</param>
        /// <exception cref="Adfitech.IntegrationException">
        ///     Thrown when an error occurs creating the loan resource.
        /// </exception>
        public void get_review_images(string url)
        {
            this.request_path = url;
            initialize_request();
            process_response();
        }

        /// <summary>
        ///     POSTs a loan to the integration service using the supplied Fannie 3.2 formatted data file.
        /// </summary>
        /// <param name="loan_number">The loan number being POSTed.</param>
        /// <param name="product_id">The product id to which the loan is to be POSTed.</param>
        /// <param name="file_path">A valid local file path for the Fannie 3.2 formatted file.</param>
        /// <exception cref="Adfitech.IntegrationException">
        ///     Thrown when an error occurs creating the loan resource.
        /// </exception>
        public void post_loan(string loan_number, string product_id, string file_path)
        {
            this.request_path = REVIEWS_PATH;
            this.template_wrapper = new CollectionJSON.TemplateWrapper();
            this.template_wrapper.template.data = new List<CollectionJSON.Datum> {
                new CollectionJSON.Datum("loan_number", loan_number),
                new CollectionJSON.Datum("product_id", product_id),
                new CollectionJSON.Datum("source_type", "Fannie32"),
                new CollectionJSON.Datum("fannie_3_2_file", System.Convert.ToBase64String(System.IO.File.
                                                                                          ReadAllBytes(file_path)))
            };
            post_json();
        }

        /// <summary>
        ///     POSTs a loan to the integration service using the supplied List<CollectionJSON.Datum>. 
        ///     Field names specified in the List must be valid recognized fields accepted by the 
        ///     service.
        /// </summary>
        /// <param name="loan_number">The loan number to be registered.</param>
        /// <param name="data">
        ///     A collection of type List<CollectionJSON.Datum> containing the
        ///     data values which will be used to create the loan.
        /// </param>
        public void post_loan(string loan_number, List<CollectionJSON.Datum> data)
        {
            this.request_path = REVIEWS_PATH;
            this.template_wrapper = new CollectionJSON.TemplateWrapper();
            this.template_wrapper.template.data = data;
            post_json();
        }

        /// <summary>
        ///     POSTs a PDF to the integration service which will be processed and added to 
        ///     the loan imaging repository for the specified loan.
        /// </summary>
        /// <param name="review_id">The loan review to which the PDF will be added.</param>
        /// <param name="file_path">A valid local file path for the PDF file.</param>
        /// <param name="doc_type">
        ///     An optional document type which will be used to name the document in 
        ///     the loan repository. A list of valid doc_type values will be supplied
        ///     by Adfitech.
        /// </param>
        public void post_pdf(string review_id, string file_path, string doc_type=null)
        {
            this.request_path = AI_PATH;
            this.template_wrapper = new CollectionJSON.TemplateWrapper();
            this.template_wrapper.template.data = new List<CollectionJSON.Datum> 
            {
                new CollectionJSON.Datum("review_id", review_id),
                new CollectionJSON.Datum("profile", "adf_import"),
                new CollectionJSON.Datum("doc_type", doc_type),
                new CollectionJSON.Datum("file", System.Convert.ToBase64String(System.IO.File.
                                                                               ReadAllBytes(file_path)))
            };
            post_json();
        }

    }

}
