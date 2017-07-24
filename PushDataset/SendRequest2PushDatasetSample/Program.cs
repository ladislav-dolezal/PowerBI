//Copyright Microsoft 2015

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using PowerBIExtensionMethods;

namespace PBIGettingStarted
{
	public class AccessToken
	{
		public string token_type;
		public string scope { get; set; }
		public string expires_in { get; set; }
		public string expires_on { get; set; }
		public string not_before { get; set; }
		public string resource { get; set; }
		public string access_token { get; set; }
		public string refresh_token { get; set; }
		public string id_token { get; set; }
	}

	//Sample to show how to use the Power BI API
	//  See also, http://docs.powerbi.apiary.io/reference

	//To run this sample:
	//Step 1 - Replace {Client ID from Azure AD app registration} with your client app ID. 
	//To learn how to get a client app ID, see Register a client app (https://msdn.microsoft.com/en-US/library/dn877542.aspx#clientID)

	class Program
	{
		//Step 1 - Replace {client id} with your client app ID. 
		//To learn how to get a client app ID, see Register a client app (https://msdn.microsoft.com/en-US/library/dn877542.aspx#clientID)
		private static string clientID = Properties.Settings.Default.ClientID;

		//RedirectUri you used when you registered your app.
		//For a client app, a redirect uri gives AAD more details on the specific application that it will authenticate.
		private static string redirectUri = "https://login.live.com/oauth20_desktop.srf";

		//Resource Uri for Power BI API
		private static string resourceUri = Properties.Settings.Default.PowerBiAPI;

		//OAuth2 authority Uri
		private static string authority = Properties.Settings.Default.AADAuthorityUri;

		private static AuthenticationContext authContext = null;
		private static string token = String.Empty;

		//Uri for Power BI datasets
		private static string datasetsUri = Properties.Settings.Default.PowerBiDataset;

		//Example dataset name and group name
		private static string datasetName = "Gira";
		private static string groupName = "Q1 Product Group";

		static void Main(string[] args)
		{
			//Example table name
			string tableName = "Message";
			//SetAccessToken();

			Console.WriteLine("--- Power BI REST API examples ---");

			//Create Dataset operation
			Console.WriteLine("Press Enter key to create a Dataset in Power BI:");
			Console.ReadLine();

			CreateDataset();

			//Get a dataset id from a Dataset name. The dataset id is used for UpdateTableSchema, AddRows, and DeleteRows
			string datasetId = GetDatasets().value.GetDataset(datasetName).Id;

			DeleteRows(datasetId, tableName);
			AddRowsInit(datasetId, tableName);
			while (true)
			{
				AddRows(datasetId, tableName);
			}
		}

		//The Create Dataset operation creates a new Dataset from a JSON schema definition and returns the Dataset ID 
		//and the properties of the dataset created.
		//POST https://api.powerbi.com/v1.0/myorg/datasets
		//Create Dataset operation: https://msdn.microsoft.com/en-US/library/mt203562(Azure.100).aspx
		static void CreateDataset()
		{
			//In a production application, use more specific exception handling.           
			try
			{
				//Create a POST web request to list all datasets
				HttpWebRequest request = DatasetRequest(String.Format("{0}/datasets", datasetsUri), "POST", AccessToken());

				//Get a list of datasets
				dataset ds = GetDatasets().value.GetDataset(datasetName);

				if (ds == null)
				{
					//POST request using the json schema from Product
					Console.WriteLine(PostRequest(request, new Message().ToDatasetJson(datasetName)));
				}
				else
				{
					Console.WriteLine("Dataset exists");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		//Groups: The Create Dataset operation can also create a dataset in a group
		//POST https://api.PowerBI.com/v1.0/myorg/groups/{group_id}/datasets
		//Create Dataset operation: https://msdn.microsoft.com/en-US/library/mt203562(Azure.100).aspx
		static void CreateDataset(string groupId)
		{
			//In a production application, use more specific exception handling.           
			try
			{
				//Create a POST web request to list all datasets
				HttpWebRequest request = DatasetRequest(String.Format("{0}/groups/{1}/datasets", datasetsUri, groupId), "POST", AccessToken());

				//Get a list of datasets in groupId
				dataset[] groupDatasets = GetDatasets(groupId).value;

				if (groupDatasets.Count() == 0)
				{
					//POST request using the json schema from Product into groupId
					Console.WriteLine(PostRequest(request, new Message().ToDatasetJson(datasetName)));
				}
				else
				{
					Console.WriteLine("Dataset exists in this group.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		//The Get Datasets operation returns a JSON list of all Dataset objects that includes a name and id.
		//GET https://api.powerbi.com/v1.0/myorg/datasets
		//Get Dataset operation: https://msdn.microsoft.com/en-US/library/mt203567.aspx
		static Datasets GetDatasets()
		{
			Datasets response = null;

			//In a production application, use more specific exception handling.
			try
			{
				//Create a GET web request to list all datasets
				HttpWebRequest request = DatasetRequest(String.Format("{0}/datasets", datasetsUri), "GET", AccessToken());

				//Get HttpWebResponse from GET request
				string responseContent = GetResponse(request);

				JavaScriptSerializer json = new JavaScriptSerializer();
				response = (Datasets)json.Deserialize(responseContent, typeof(Datasets));
			}
			catch (Exception ex)
			{
				//In a production application, handle exception
			}

			return response;
		}

		//Groups: The Get Datasets operation can also get datasets in a group
		//GET https://api.powerbi.com/v1.0/myorg/groups/{group_id}/datasets
		//Get Dataset operation: https://msdn.microsoft.com/en-US/library/mt203567.aspx
		static Datasets GetDatasets(string groupId)
		{
			Datasets response = null;

			//In a production application, use more specific exception handling.
			try
			{
				//Create a GET web request to list all datasets in a group
				HttpWebRequest request = DatasetRequest(String.Format("{0}/groups/{1}/datasets", datasetsUri, groupId), "GET", AccessToken());

				//Get HttpWebResponse from GET request
				string responseContent = GetResponse(request);

				JavaScriptSerializer json = new JavaScriptSerializer();
				response = (Datasets)json.Deserialize(responseContent, typeof(Datasets));
			}
			catch (Exception ex)
			{
				//In a production application, handle exception
			}

			return response;
		}

		//The Get Tables operation returns a JSON list of Tables for the specified Dataset.
		//GET https://api.powerbi.com/v1.0/myorg/datasets/{dataset_id}/tables
		//Get Tables operation: https://msdn.microsoft.com/en-US/library/mt203556.aspx
		static Tables GetTables(string datasetId)
		{
			Tables response = null;

			//In a production application, use more specific exception handling.
			try
			{
				//Create a GET web request to list all datasets
				HttpWebRequest request = DatasetRequest(String.Format("{0}/datasets/{1}/tables", datasetsUri, datasetId), "GET", AccessToken());

				//Get HttpWebResponse from GET request
				string responseContent = GetResponse(request);

				JavaScriptSerializer json = new JavaScriptSerializer();
				response = (Tables)json.Deserialize(responseContent, typeof(Tables));
			}
			catch (Exception ex)
			{
				//In a production application, handle exception
			}

			return response;
		}

		//Groups: The Get Tables operation returns a JSON list of Tables for the specified Dataset in a Group.
		//GET https://api.powerbi.com/v1.0/myorg/groups/{group_id}/datasets/{dataset_id}/tables
		//Get Tables operation: https://msdn.microsoft.com/en-US/library/mt203556.aspx
		static Tables GetTables(string groupId, string datasetId)
		{
			Tables response = null;

			//In a production application, use more specific exception handling.
			try
			{
				//Create a GET web request to list all datasets
				HttpWebRequest request = DatasetRequest(String.Format("{0}/groups/{1}/datasets/{2}/tables", datasetsUri, groupId, datasetId), "GET", AccessToken());

				//Get HttpWebResponse from GET request
				string responseContent = GetResponse(request);

				JavaScriptSerializer json = new JavaScriptSerializer();
				response = (Tables)json.Deserialize(responseContent, typeof(Tables));
			}
			catch (Exception ex)
			{
				//In a production application, handle exception
			}

			return response;
		}

		//The Add Rows operation adds Rows to a Table in a Dataset.
		//POST https://api.powerbi.com/v1.0/myorg/datasets/{dataset_id}/tables/{table_name}/rows
		//Add Rows operation: https://msdn.microsoft.com/en-US/library/mt203561.aspx
		static void AddRows(string datasetId, string tableName)
		{
			//In a production application, use more specific exception handling. 
			try
			{
				HttpWebRequest request = DatasetRequest(String.Format("{0}/datasets/{1}/tables/{2}/rows", datasetsUri, datasetId, tableName), "POST", AccessToken());

				//Create a list of Product
				List<Message> messages = new List<Message>();

				int value = 0;
				int destination = 0;
				for (int i = 6; i < 15; i++)
				{
					destination += 5;
					Random r = new Random();
					value = r.Next(-2, 3);
					System.Threading.Thread.Sleep(200);
					messages.Add(
							new Message() { Hour = string.Format("{0}:00", ("0" + i.ToString()).Substring(("0" + i.ToString()).Length - 2, 2)), Value = value, Destination = 0 }
						);
				}

				//POST request using the json from a list of Product
				//NOTE: Posting rows to a model that is not created through the Power BI API is not currently supported. 
				//      Please create a dataset by posting it through the API following the instructions on http://dev.powerbi.com.
				PostRequest(request, messages.ToJson(JavaScriptConverter<Message>.GetSerializer()));
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("{0} : {1}", System.DateTime.Now.ToString(), ex.Message));
				token = String.Empty;
			}
		}

		static void AddRowsInit(string datasetId, string tableName)
		{
			//In a production application, use more specific exception handling. 
			try
			{
				HttpWebRequest request = DatasetRequest(String.Format("{0}/datasets/{1}/tables/{2}/rows", datasetsUri, datasetId, tableName), "POST", AccessToken());

				//Create a list of Product
				List<Message> messages = new List<Message>();

				int destination = 0;
				for (int i = 6; i < 15; i++)
				{
					destination += 5;

					messages.Add(
							new Message() { Hour = string.Format("{0}:00", ("0" + i.ToString()).Substring(("0" + i.ToString()).Length - 2, 2)), Value = destination, Destination = destination }
						);
				}

				//POST request using the json from a list of Product
				//NOTE: Posting rows to a model that is not created through the Power BI API is not currently supported. 
				//      Please create a dataset by posting it through the API following the instructions on http://dev.powerbi.com.
				Console.WriteLine(PostRequest(request, messages.ToJson(JavaScriptConverter<Message>.GetSerializer())));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		//Groups: The Add Rows operation adds Rows to a Table in a Dataset in a Group.
		//POST https://api.powerbi.com/v1.0/myorg/groups/{group_id}/datasets/{dataset_id}/tables/{table_name}/rows
		//Add Rows operation: https://msdn.microsoft.com/en-US/library/mt203561.aspx
		static void AddRows(string groupId, string datasetId, string tableName)
		{
			////In a production application, use more specific exception handling. 
			//try
			//{
			//	HttpWebRequest request = DatasetRequest(String.Format("{0}/groups/{1}/datasets/{2}/tables/{3}/rows", datasetsUri, groupId, datasetId, tableName), "POST", AccessToken());

			//	//Create a list of Product
			//	List<Product> products = new List<Product>
			//	{
			//		new Product{ProductID = 1, Name="Adjustable Race", Category="Components", IsCompete = true, ManufacturedOn = new DateTime(2014, 7, 30)},
			//		new Product{ProductID = 2, Name="LL Crankarm", Category="Components", IsCompete = true, ManufacturedOn = new DateTime(2014, 7, 30)},
			//		new Product{ProductID = 3, Name="HL Mountain Frame - Silver", Category="Bikes", IsCompete = true, ManufacturedOn = new DateTime(2014, 7, 30)},
			//	};

			//	//POST request using the json from a list of Product
			//	//NOTE: Posting rows to a model that is not created through the Power BI API is not currently supported. 
			//	//      Please create a dataset by posting it through the API following the instructions on http://dev.powerbi.com.
			//	Console.WriteLine(PostRequest(request, products.ToJson(JavaScriptConverter<Product>.GetSerializer())));

			//}
			//catch (Exception ex)
			//{
			//	Console.WriteLine(ex.Message);
			//}
		}

		//The Delete Rows operation deletes Rows from a Table in a Dataset.
		//DELETE https://api.powerbi.com/v1.0/myorg/datasets/{dataset_id}/tables/{table_name}/rows
		//Delete Rows operation: https://msdn.microsoft.com/en-US/library/mt238041.aspx
		static void DeleteRows(string datasetId, string tableName)
		{
			//In a production application, use more specific exception handling. 
			try
			{
				//Create a DELETE web request
				HttpWebRequest request = DatasetRequest(String.Format("{0}/datasets/{1}/tables/{2}/rows", datasetsUri, datasetId, tableName), "DELETE", AccessToken());
				request.ContentLength = 0;

				Console.WriteLine(GetResponse(request));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		//Groups: The Delete Rows operation deletes Rows from a Table in a Dataset in a Group.
		//DELETE https://api.powerbi.com/v1.0/myorg/groups/{group_id}/datasets/{dataset_id}/tables/{table_name}/rows
		//Delete Rows operation: https://msdn.microsoft.com/en-US/library/mt238041.aspx
		static void DeleteRows(string groupId, string datasetId, string tableName)
		{
			//In a production application, use more specific exception handling. 
			try
			{
				//Create a DELETE web request
				HttpWebRequest request = DatasetRequest(String.Format("{0}/groups/{1}/datasets/{2}/tables/{3}/rows", datasetsUri, groupId, datasetId, tableName), "DELETE", AccessToken());
				request.ContentLength = 0;

				Console.WriteLine(GetResponse(request));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		//The Update Table Schema operation updates a Table schema in a Dataset.
		//PUT https://api.powerbi.com/v1.0/myorg/datasets/{dataset_id}/tables/{table_name}
		//Update Table Schema operation: https://msdn.microsoft.com/en-US/library/mt203560.aspx
		static void UpdateTableSchema(string datasetId, string tableName)
		{
			//In a production application, use more specific exception handling.           
			try
			{
				////Create a POST web request to list all datasets
				//HttpWebRequest request = DatasetRequest(String.Format("{0}/datasets/{1}/tables/{2}", datasetsUri, datasetId, tableName), "PUT", AccessToken());

				//PostRequest(request, new Product2().ToTableSchema(tableName));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		//Groups: The Update Table Schema operation updates a Table schema in a Dataset in a Group.
		//PUT https://api.powerbi.com/v1.0/myorg/groups/{group_id}/datasets/{dataset_id}/tables/{table_name}
		//Update Table Schema operation: https://msdn.microsoft.com/en-US/library/mt203560.aspx
		static void UpdateTableSchema(string groupId, string datasetId, string tableName)
		{
			//In a production application, use more specific exception handling.           
			try
			{
				////Create a POST web request to list all datasets
				//HttpWebRequest request = DatasetRequest(String.Format("{0}/groups/{1}/datasets/{2}/tables/{3}", datasetsUri, groupId, datasetId, tableName), "PUT", AccessToken());

				//PostRequest(request, new Product2().ToTableSchema(tableName));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		//The Get Groups operation returns a JSON list of all the Groups that the signed in user is a member of. 
		//GET https://api.powerbi.com/v1.0/myorg/groups
		//Get Groups operation: https://msdn.microsoft.com/en-US/library/mt243842.aspx
		static Groups GetGroups()
		{
			Groups response = null;

			//In a production application, use more specific exception handling.
			try
			{
				//Create a GET web request to list all datasets
				HttpWebRequest request = DatasetRequest(String.Format("{0}/groups", datasetsUri), "GET", AccessToken());

				//Get HttpWebResponse from GET request
				string responseContent = GetResponse(request);

				JavaScriptSerializer json = new JavaScriptSerializer();
				response = (Groups)json.Deserialize(responseContent, typeof(Groups));
			}
			catch (Exception ex)
			{
				//In a production application, handle exception
			}

			return response;
		}

		/// <summary>
		/// Use AuthenticationContext to get an access token
		/// </summary>
		/// <returns></returns>
		static string AccessToken()
		{
			if (token == String.Empty)
			{
				token = GetAccessToken();
				/* with login dialog
				//Get Azure access token
				// Create an instance of TokenCache to cache the access token
				TokenCache TC = new TokenCache();
				// Create an instance of AuthenticationContext to acquire an Azure access token
				authContext = new AuthenticationContext(authority, TC);
				// Call AcquireToken to get an Azure token from Azure Active Directory token issuance endpoint
				token = authContext.AcquireToken(resourceUri, clientID, new Uri(redirectUri), PromptBehavior.RefreshSession).AccessToken;*/
			}

			return token;
		}

		public static string GetAccessToken()
		{
			StringBuilder body = new StringBuilder();
			body.Append("resource=" + HttpUtility.UrlEncode(resourceUri));
			body.Append("&client_id=" + HttpUtility.UrlEncode(clientID));
			body.Append("&grant_type=" + HttpUtility.UrlEncode("password"));
			body.Append("&username=" + HttpUtility.UrlEncode("powerbi@inqu.de"));
			body.Append("&password=" + HttpUtility.UrlEncode("dresden.20172"));
			body.Append("&scope=" + HttpUtility.UrlEncode("openid"));
			//body.Append("&client_secret=" + HttpUtility.UrlEncode(""));

			using (WebClient web = new WebClient())
			{
				web.Headers.Add("client-request-id", Guid.NewGuid().ToString());
				web.Headers.Add("return-client-request-id", "true");

				string data = web.UploadString("https://login.windows.net/common/oauth2/token", body.ToString());

				dynamic result = JsonConvert.DeserializeObject(data);

				try
				{
					return result.access_token;
				}
				catch
				{ // Log as you want }
				}

				return null;
			}
		}

		//private static async void SetAccessToken()
		//{
		//	List<KeyValuePair<string, string>> vals = new List<KeyValuePair<string, string>>();
		//	vals.Add(new KeyValuePair<string, string>("grant_type", "password"));
		//	vals.Add(new KeyValuePair<string, string>("scope", "openid"));
		//	vals.Add(new KeyValuePair<string, string>("resource", resourceUri));
		//	vals.Add(new KeyValuePair<string, string>("client_id", clientID));
		//	vals.Add(new KeyValuePair<string, string>("client_secret", ""));
		//	vals.Add(new KeyValuePair<string, string>("username", "ladislav.dolezal@inqu.de"));
		//	vals.Add(new KeyValuePair<string, string>("password", "Corwin.471"));
		//	string TenantId = "inqu.de";
		//	string url = string.Format("https://login.windows.net/{0}/oauth2/token", TenantId);
		//	HttpClient hc = new HttpClient();
		//	HttpContent content = new FormUrlEncodedContent(vals);
		//	HttpResponseMessage hrm = hc.PostAsync(url, content).Result;
		//	string responseData = "";
		//	if (hrm.IsSuccessStatusCode)
		//	{
		//		Stream data = await hrm.Content.ReadAsStreamAsync();
		//		using (StreamReader reader = new StreamReader(data, Encoding.UTF8))
		//		{
		//			responseData = reader.ReadToEnd();
		//		}
		//	}
		//	AccessToken t = JsonConvert.DeserializeObject<AccessToken>(responseData);
		//	token = t.access_token;
		//}

		private static string PostRequest(HttpWebRequest request, string json)
		{
			byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
			request.ContentLength = byteArray.Length;

			//Write JSON byte[] into a Stream
			using (Stream writer = request.GetRequestStream())
			{
				writer.Write(byteArray, 0, byteArray.Length);
			}

			return GetResponse(request);
		}

		private static string GetResponse(HttpWebRequest request)
		{
			string response = string.Empty;

			using (HttpWebResponse httpResponse = request.GetResponse() as System.Net.HttpWebResponse)
			{
				//Get StreamReader that holds the response stream
				using (StreamReader reader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
				{
					response = reader.ReadToEnd();
				}
			}

			return response;
		}

		private static HttpWebRequest DatasetRequest(string datasetsUri, string method, string accessToken)
		{
			HttpWebRequest request = System.Net.WebRequest.Create(datasetsUri) as System.Net.HttpWebRequest;
			request.KeepAlive = true;
			request.Method = method;
			request.ContentLength = 0;
			request.ContentType = "application/json";
			request.Headers.Add("Authorization", String.Format("Bearer {0}", accessToken));

			return request;
		}
	}
}