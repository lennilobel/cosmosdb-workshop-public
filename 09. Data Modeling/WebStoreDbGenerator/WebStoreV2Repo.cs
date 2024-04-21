using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace WebStoreDbGenerator.V2
{
	public class WebStoreV2Repo : WebStoreRepoBase
	{
		#region "Private fields"

		protected Container _customerContainer;
		private Container _productCategoryContainer;
		protected Container _productContainer;
		private Container _productTagContainer;
		private Container _salesOrderContainer;

		#endregion

		#region "Properties"

		public override VersionIdentifier Version => VersionIdentifier.V2;
		public override string DatabaseName => "webstore-v2";
		public override Container CustomerContainer => this._customerContainer;
		public override Container CustomerAddressContainer => this._customerContainer;
		public override Container CustomerPasswordContainer => this._customerContainer;
		public override Container ProductCategoryContainer => this._productCategoryContainer;
		public override Container ProductContainer => this._productContainer;
		public override Container ProductTagsContainer => this._productContainer;
		public override Container ProductTagContainer => this._productTagContainer;
		public override Container SalesOrderContainer => this._salesOrderContainer;
		public override Container SalesOrderDetailContainer => this._salesOrderContainer;

		protected override (string ContainerName, string PartitionKey)[] Containers => new[]
		{
			("customer", "/id"),
			("productCategory", "/type"),
			("product", "/categoryId"),
			("productTag", "/type"),
			("salesOrder", "/customerId"),
		};

		#endregion

		#region "Generate"

		protected override void GetContainers()
		{
			if (this._customerContainer == null)
			{
				this._customerContainer = base._database.GetContainer("customer");
				this._productCategoryContainer = base._database.GetContainer("productCategory");
				this._productContainer = base._database.GetContainer("product");
				this._productTagContainer = base._database.GetContainer("productTag");
				this._salesOrderContainer = base._database.GetContainer("salesOrder");
			}
		}

		protected override int CreateDocuments()
		{
			Task.Run(async () =>
			{
				await this.CreateCustomerDocuments(0);
				await this.CreateProductCategoryDocuments(1);
				await this.CreateProductDocuments(2);
				await this.CreateProductTagDocuments(3);
				await this.CreateSalesOrderDocuments(4);
			}).Wait();

			return 5;
		}

		protected virtual async Task CreateCustomerDocuments(int lineIndex)
		{
            var ctr = 0;
            var elapsed = await base.TimeActionAsync(async () =>
            {
                var docs = new dynamic[base._customerTable.Rows.Count];
                foreach (DataRow row in base._customerTable.Rows)
                {
					if (ctr % 1000 == 0)
					{
						this.WriteLine($"({ctr + 1}) Creating customer", lineIndex + 2);
					}
					var customerId = row["CustomerId"].ToString();
                    var addressDocs = this.GenerateCustomerAddressDocuments(customerId);
                    var passwordDoc = this.GenerateCustomerPasswordDocument(customerId);
                    dynamic doc = new
                    {
                        id = customerId,
                        title = row["Title"].ToString(),
                        firstName = row["FirstName"].ToString(),
                        lastName = row["LastName"].ToString(),
                        emailAddress = row["EmailAddress"].ToString(),
                        phoneNumber = row["PhoneNumber"].ToString(),
                        creationDate = Convert.ToDateTime(row["CreationDate"]).ToString("s"),
                        addresses = addressDocs,
                        password = passwordDoc,
                    };
					docs[ctr] = doc;
					ctr++;
				}

				var errCnt = await base.BulkInsert(this.CustomerContainer, docs, d => d.id, "customer", lineIndex + 2);
				ctr = docs.Length - errCnt;
            });
			this.WriteLine($"Generated {ctr} customer documents in {elapsed}", lineIndex + 2);
		}

		protected dynamic[] GenerateCustomerAddressDocuments(string customerId)
		{
			var rows = base._customerAddressTable.AsEnumerable().Where(r => r["CustomerId"].ToString() == customerId).ToArray();
			var docs = new dynamic[rows.Length];
			var ctr = 0;
			foreach (DataRow row in rows)
			{
				dynamic doc = new
				{
					addressLine1 = row["AddressLine1"].ToString(),
					addressLine2 = row["AddressLine2"].ToString(),
					city = row["City"].ToString(),
					state = row["State"].ToString(),
					country = row["Country"].ToString(),
					zipCode = row["ZipCode"].ToString(),
				};
				docs[ctr] = doc;
				ctr++;
			}
			return docs;
		}

		protected dynamic GenerateCustomerPasswordDocument(string customerId)
		{
			var row = base._customerPasswordTable.AsEnumerable().First(r => r["CustomerId"].ToString() == customerId);
			dynamic doc = new
			{
				hash = row["Hash"].ToString(),  // Convert.ToBase64String((byte[])row["Hash"]),
				salt = row["Salt"].ToString(),
			};
			return doc;
		}

		private async Task CreateProductCategoryDocuments(int lineIndex)
		{
            var ctr = 0;
            var elapsed = await base.TimeActionAsync(async () =>
            {
				var docs = new dynamic[base._productCategoryTable.Rows.Count];
				foreach (DataRow row in base._productCategoryTable.Rows)
                {
					if (ctr % 1000 == 0)
					{
						this.WriteLine($"({ctr}) Creating product category", lineIndex + 2);
					}
					dynamic doc = new
                    {
                        id = row["ProductCategoryId"].ToString(),
                        name = row["Name"].ToString(),
                        type = "category",
                    };
					docs[ctr] = doc;
					ctr++;
				}
				var errCnt = await base.BulkInsert(this.ProductCategoryContainer, docs, d => d.type, "product category", lineIndex + 2);
				ctr = docs.Length - errCnt;
            });
			this.WriteLine($"Generated {ctr} product category documents in {elapsed}", lineIndex + 2);
		}

		protected virtual async Task CreateProductDocuments(int lineIndex)
		{
            var ctr = 0;
            var elapsed = await base.TimeActionAsync(async () =>
            {
				var docs = new dynamic[base._productTable.Rows.Count];
				foreach (DataRow row in base._productTable.Rows)
                {
					if (ctr % 1000 == 0)
					{
						this.WriteLine($"({ctr}) Creating product", lineIndex + 2);
					}
					var productId = row["ProductId"].ToString();
                    var productTagIds = this.GenerateProductTagIds(productId);
                    dynamic doc = new
                    {
                        id = productId,
                        categoryId = row["ProductCategoryId"].ToString(),
                        sku = row["Sku"].ToString(),
                        name = row["Name"].ToString(),
                        description = row["Description"].ToString(),
                        price = (decimal)row["Price"],
                        tagIds = productTagIds,
                    };
					docs[ctr] = doc;
					ctr++;
				}
				var errCnt = await base.BulkInsert(this.ProductContainer, docs, d => d.categoryId, "product", lineIndex + 2);
				ctr = docs.Length - errCnt;
            });
			this.WriteLine($"Generated {ctr} product documents in {elapsed}", lineIndex + 2);
		}

		private string[] GenerateProductTagIds(string productId)
		{
			var tagIds = new List<string>();
			foreach (DataRow row in base._productTagsTable.AsEnumerable().Where(r => r["ProductId"].ToString() == productId))
			{
				var tagId = row["ProductTagId"].ToString();
				tagIds.Add(tagId);
			}
			return tagIds.ToArray();
		}

		private async Task CreateProductTagDocuments(int lineIndex)
		{
            var ctr = 0;
            var elapsed = await base.TimeActionAsync(async () =>
            {
                var docs = new dynamic[base._productTagTable.Rows.Count];
                foreach (DataRow row in base._productTagTable.Rows)
                {
					if (ctr % 1000 == 0)
					{
						this.WriteLine($"({ctr}) Creating product tag", lineIndex + 2);
					}
					dynamic doc = new
                    {
                        id = row["ProductTagId"].ToString(),
                        name = row["Name"].ToString(),
                        type = "tag",
                    };
					docs[ctr] = doc;
					ctr++;
				}
				var errCnt = await base.BulkInsert(this.ProductTagContainer, docs, d => d.type, "product tag", lineIndex + 2);
				ctr = docs.Length - errCnt;
            });
			this.WriteLine($"Generated {ctr} product tag documents in {elapsed}", lineIndex + 2);
		}

		protected virtual async Task CreateSalesOrderDocuments(int lineIndex)
		{
            var ctr = 0;
            var elapsed = await base.TimeActionAsync(async () =>
            {
				var docs = new dynamic[base._salesOrderTable.Rows.Count];
				foreach (DataRow row in base._salesOrderTable.Rows)
                {
					if (ctr % 1000 == 0)
					{
						this.WriteLine($"({ctr}) Creating sales order", lineIndex + 2);
					}
					var salesOrderId = row["SalesOrderId"].ToString();
                    var detailDocs = this.GenerateSalesOrderDetailDocuments(salesOrderId);
                    dynamic doc = new
                    {
                        id = salesOrderId,
                        customerId = row["CustomerId"].ToString(),
                        orderDate = Convert.ToDateTime(row["OrderDate"]).ToString("s"),
                        shipDate = Convert.ToDateTime(row["ShipDate"]).ToString("s"),
                        details = detailDocs,
                    };
					docs[ctr] = doc;
					ctr++;
				}
				var errCnt = await base.BulkInsert(this.SalesOrderContainer, docs, d => d.customerId, "sales order", lineIndex + 2);
				ctr = docs.Length - errCnt;
			});
			this.WriteLine($"Generated {ctr} sales order documents in {elapsed}", lineIndex + 2);
		}

		protected dynamic[] GenerateSalesOrderDetailDocuments(string salesOrderId)
		{
			var rows = base._salesOrderDetailTable.AsEnumerable().Where(r => r["SalesOrderId"].ToString() == salesOrderId).ToArray();
			var docs = new dynamic[rows.Length];
			var ctr = 0;
			foreach (DataRow row in rows)
			{
				dynamic doc = new
				{
					sku = row["Sku"].ToString(),
					name = row["Name"].ToString(),
					price = (decimal)row["Price"],
					quantity = (short)row["Quantity"],
				};
				docs[ctr] = doc;
				ctr++;
			}
			return docs;
		}

		#endregion
	}
}
