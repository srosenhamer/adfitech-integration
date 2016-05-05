namespace api_example
{
    using System;
    using System.Collections.Generic;
    using Adfitech;

    class Example
    {
        private Integration adf;
        private Integration review;
        private string api_key = "THE ACCESS KEY PROVIDED BY ADFITECH";
        private int api_key_id = 1234; // change to the id provided by ADFITECH

        public void show_data(Integration obj)
        {
            obj.items.ForEach(delegate(CollectionJSON.Item item)
            {
                Console.WriteLine("  Item " + item.href + ":");
                item.data.ForEach(delegate(CollectionJSON.Datum field)
                {
                    Console.WriteLine("    " + field.name + ": " + field.value);
                });
            });
        }

        public void list_documents(string loan_number)
        {
            try
            {
                int summary_id = 0;
                adf.find_reviews(loan_number);
                if (!adf.empty)
                {
                    Console.WriteLine("Found review for loan " + loan_number + ", getting document list:");
                    adf.item.data.ForEach(delegate(CollectionJSON.Datum field)
                    {
                        Console.WriteLine("    " + field.name + ": " + field.value);
                        if (field.name == "id")
                        {
                            summary_id = (int)System.Convert.ToInt32(field.value);
                        }
                    });
                    if (summary_id != 0)
                    {
                        list_documents(summary_id);
                    }
                    else
                    {
                        Console.WriteLine("  no summary found.");
                    }
                }
                else
                    Console.WriteLine("No reviews found for loan " + loan_number);
            }
            catch (Adfitech.IntegrationException e)
            {
                Console.WriteLine("caught exception " + e.GetType() + ":");
                Console.WriteLine("  " + e.error.code + ": " + e.error.message);
            }

        }
        public void list_documents(int review_id)
        {
            adf.get_document_list(review_id.ToString());
            if (adf.empty)
                Console.WriteLine("  no documents found.");
            else
                show_data(adf);

        }
        public void get_review_images(string review_id, List<string> doc_code, string stack=null)
        {
            adf.get_review_images(Convert.ToInt64(review_id), doc_code, stack);
            if (adf.empty)
            {
                Console.WriteLine("  Document(s) not retrieved.");
            }
            else
            {
                adf.downloaded_files.ForEach(delegate(string file_path)
                {
                    Console.WriteLine("  Got something back: " + file_path);
                });
            }
        }
        public void get_review_images(string url)
        {
            adf.get_review_images(url);
            if (!adf.file_received())
            {
                Console.WriteLine("  Document(s) not retrieved.");
            }
            else
            {
                adf.downloaded_files.ForEach(delegate(string file_path)
                {
                    Console.WriteLine("  Got something back: " + file_path);
                });
            }
        }
        public void find_reviews(string loan_number)
        {
            try
            {
                string summary_id = null;
                adf.find_reviews(loan_number);
                if(adf.empty)
                    Console.WriteLine("No reviews found for loan " + loan_number);
                else
                    Console.WriteLine(adf.items.Count + " reviews found for loan " + loan_number);
                adf.items.ForEach(delegate(CollectionJSON.Item item)
                {
                    Console.WriteLine("  Loan " + item.href + ":");
                    item.data.ForEach(delegate(CollectionJSON.Datum field)
                    {
                        Console.WriteLine("    " + field.name + ": " + field.value);
                        if (field.name == "summary_id")
                        {
                            summary_id = (string)field.value;
                        }
                    });
                    if (summary_id != null)
                    {
                        review = new Integration(api_key, api_key_id);
                        review.get_review(summary_id);
                        Console.WriteLine("    Review:");
                        review.item.data.ForEach(delegate(CollectionJSON.Datum field)
                        {
                            Console.WriteLine("      " + field.name + ": " + field.value);
                        });
                    }
                });
            }
            catch (Adfitech.IntegrationException e)
            {
                Console.WriteLine("caught exception "+e.GetType()+":");
                Console.WriteLine("  " + e.error.code + ": " + e.error.message);
            }
        }
        public void post_loan_32(string loan_number, string product_id, string file_path)
        {
            try
            {
                adf.post_loan(loan_number, product_id, file_path);
                Console.WriteLine("Loan posted - href: " + adf.item.href + ":");
                adf.item.data.ForEach(delegate(CollectionJSON.Datum field)
                {
                    Console.WriteLine("  " + field.name + ": " + field.value);
                });
            }
            catch (Adfitech.IntegrationException e)
            {
                Console.WriteLine("caught exception " + e.GetType() + ":");
                Console.WriteLine("  " + e.error.code + ": " + e.error.message);
            }
        }
        public void post_loan_json(string loan_number, string product_id)
        {
            List<CollectionJSON.Datum> data = new List<CollectionJSON.Datum> 
            {
                new CollectionJSON.Datum("loan_number", loan_number),
                new CollectionJSON.Datum("last_name", "Smithy"),
                new CollectionJSON.Datum("product_id", product_id),
                new CollectionJSON.Datum("source_type", "JSON")
            };
            try
            {
                adf.post_loan(loan_number, data);
                Console.WriteLine("Loan posted - href: " + adf.item.href + ":");
                adf.item.data.ForEach(delegate(CollectionJSON.Datum field)
                {
                    Console.WriteLine("  " + field.name + ": " + field.value);
                });
            }
            catch (Adfitech.IntegrationException e)
            {
                Console.WriteLine("caught exception " + e.GetType() + ":");
                Console.WriteLine("  " + e.error.code + ": " + e.error.message);
            }
        }
        public void post_loan_pdf(string review_id, string file_path, string doc_type)
        {
            try
            {
                adf.post_pdf(review_id, file_path, doc_type);
                Console.WriteLine("PDF posted - href: " + adf.item.href + ":");
                adf.item.data.ForEach(delegate(CollectionJSON.Datum field)
                {
                    Console.WriteLine("  " + field.name + ": " + field.value);
                });
            }
            catch (Adfitech.IntegrationException e)
            {
                Console.WriteLine("caught exception " + e.GetType() + ":");
                Console.WriteLine("  " + e.error.code + ": " + e.error.message);
            }
        }

        public Example()
        {
            adf = new Integration(api_key, api_key_id);
        }
    }
    class Sample
    {
        // the command line program entry point
        static void Main(string[] args)
        {
            var ex = new Example();

            // ovbviously replace `A LOAN NUMBER`, `A REVIEW ID` with actual data
            ex.find_reviews("A LOAN NUMBER"); 
            ex.list_documents("A REVIEW ID");
            ex.get_review_images("A REVIEW ID", new List<string>{"103-1", "103-2"});
            ex.post_loan_32("A LOAN NUMBER", "A PRODUCT ID", "..\\..\\support\\data\\0102030407.fnm");
            ex.post_loan_pdf("A REVIEW ID", "..\\..\\support\\images\\HDS.pdf", "LNFILE");
        }
    }
}
