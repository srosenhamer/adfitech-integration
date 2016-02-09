## Adfitech API .Net Samples

A repository of .NET classes written to interact with the adfitech api. Currently, these classes are only availabe in C#.

#### Prerequisites


  - In order to access the API, you must have a valid access key and access key ID issued by Adfitech, if  you do not have this, contact your CSR.
  - Access to api functionality is controlled by the profile assigned to your access key. Only functionality agreed upon in your integration setup will be enabled.

## Implementation

There are several source files included in this repository which you are free to download and include in your project in order to speed the implementation process.

  - `ExtensionMethods.cs`  -  a few simple extension to existing .net classes
  - `CollectionJSON.cs`  -  a class for serializing/deserializing json strings
  - `AdfitechIntegration.cs`  -  a class which provides methods used to interact with the API, this class depends on `CollectionJSON.cs`, `AdfitechIntegrationException.cs` and `ExtensionMethods.cs`
  - `AdfitechIntegrationException.cs`  -  some standard exceptions which could be thrown from the `AdfitechIntegration` class
  - `Sample.cs`  -  just some implementation examples using the above source files

Below are a few samples of how one might use the aforementioned classes.

### Environments

Adfitech maintains a separate production and staging environment. It is intended that all client testing be performed using the staging environment. 

*Access keys are tied to specific environments, your staging key will not work in production and the opposite is also true.*

The `Integration` class defaults to using the staging environment but this is easily changed when calling the contructor, as seen below.

---

## Some example code utilizing the `Adfitech.Integration` class

### Initialization

The `Adfitech.Integration` class can be initialized with your access key and id, from there the `HMAC` generation is handled for you for all subsequent api requests.

```
    using System;
    using System.Collections.Generic;
    using Adfitech;

    string api_key = "d2b2e5a58d136ca0678a8b16cb8709153bd4130c";
    int api_key_id = 345;

    Integration adf;

    // this will use the 'staging' environment (default)
    adf = new Integration(api_key, api_key_id); 
    // to use the 'production' environment
    adf = new Integration(api_key, api_key_id, "production"); 

```

All following sample code assumes you instantiated an `Integration` object using the variable name `adf` as in the example above.

---

### Query Reviews by Loan Number

In this example, we find all reviews that have been ordered for loan `0102030407`, then we find the specific review for each loan and simply write the field/value pairs to the console.

```
  try
  {
      string loan_number = "0102030407";
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
              Integration review = new Integration(api_key, api_key_id);
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
```

---

### Find a Review by ID

As noted in the previous example individual reviews are accessible directly by ID. In this example, we find an review for which we already know the id and then we examine the field/value pairs.

```
  string review_id = "1234";
  adf.get_review(review_id);
  Console.WriteLine("    Review:");
  review.item.data.ForEach(delegate(CollectionJSON.Datum field)
  {
      Console.WriteLine("      " + field.name + ": " + field.value);
  });
```

---

### Upload a PDF to to a specific Review

Simply posts a PDF that will be added to loanvault attached to the specified review. `file_path` should be fully qualified file path.

```
  adf.post_pdf(review_id, file_path);
  Console.WriteLine("PDF posted - href: " + adf.item.href + ":");
  adf.item.data.ForEach(delegate(CollectionJSON.Datum field)
  {
      Console.WriteLine("  " + field.name + ": " + field.value);
  });
```

---

### Query Available Documents by Review ID

The following example is a simple query for loan documents available given a review id, the details of those documents are simply output to the console.

```
  string review_id = "1234";
  adf.get_document_list(review_id);
  if (adf.empty)
      Console.WriteLine("  no documents found.");
  else{
      adf.items.ForEach(delegate(CollectionJSON.Item item)
      {
          Console.WriteLine("  Item " + item.href + ":");
          item.data.ForEach(delegate(CollectionJSON.Datum field)
          {
              Console.WriteLine("    " + field.name + ": " + field.value);
          });
      });

  }
```

---

### Create a PDF of Loan Images Synchronously

The Adfitech API handles the creation of loan image PDF files in an asynchronous fashion. The `get_review_images` method defined in the `Integration` class can be used to handle this process in a synchronous fashion. The request to generate the images is made and then we simply attempt to download the result file a predermined number of times, catching the expected `NotReady` exception and trying again until we've tried our maximum defined number of times.

Once a file is succesfully retrieved, the path to any files is stored int the `downloaded_files` array.

```
  int review_id = 1234;
  List<string> doc_codes = new List<string>{"103-1", "103-2"};
  adf.get_review_images(review_id, doc_codes);
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
```

*Note: As currently written, once the instantiated `Integration` object is finalized, any files downloaded will be deleted.*

---

### Asynchronous PDF creation

If it is desired to handle downloaded of generated files in an asynchronous fashion, there are a couple of methods available which will allow a developer to process query the status of a previously requested pdf generation.

```
  // obtain a list of previously requested pdf images
  string review_id = "1234";
  adf.get_review_images_list(review_id)

  // retrieve the loan_images resource of requested pdf
  string images_id = "987";
  adf.get_review_images_resource(images_id);
  // there is a 'status' field included 
  adf.item.data.ForEach(delegate(CollectionJSON.Datum field)
  {
      if(field.name == "status")
        Console.WriteLine("    Request status: " + field.value);
  });
  // the status will be 'completed' for any finished request.

```