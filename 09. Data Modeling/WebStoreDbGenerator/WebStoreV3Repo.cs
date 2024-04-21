using WebStoreDbGenerator.V2;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace WebStoreDbGenerator.V3
{
	public class WebStoreV3Repo : WebStoreV2Repo
	{
		#region "Properties"

		public override VersionIdentifier Version => VersionIdentifier.V3;
		public override string DatabaseName => "webstore-v3";

		#endregion

		#region "Generate"

		protected override async Task CreateProductDocuments(int lineIndex)
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
					var productCategoryId = row["ProductCategoryId"].ToString();
					var productCategoryName = base._productCategoryTable.AsEnumerable().First(r => r["ProductCategoryId"].ToString() == productCategoryId)["Name"].ToString();
					var productTagDocs = this.GenerateProductTagDocuments(productId);
					dynamic doc = new
					{
						id = productId,
						categoryId = productCategoryId,
						categoryName = productCategoryName,
						sku = row["Sku"].ToString(),
						name = row["Name"].ToString(),
						description = row["Description"].ToString(),
						price = (decimal)row["Price"],
						tags = productTagDocs,
					};
					docs[ctr] = doc;
					ctr++;
				}
				var errCnt = await base.BulkInsert(this.ProductContainer, docs, d => d.categoryId, "product", lineIndex + 2);
				ctr = docs.Length - errCnt;
			});
			this.WriteLine($"Generated {ctr} product documents in {elapsed}", lineIndex + 2);
		}

		private dynamic[] GenerateProductTagDocuments(string productId)
		{
			var docs = new List<dynamic>();
			foreach (DataRow row in base._productTagsTable.AsEnumerable().Where(r => r["ProductId"].ToString() == productId))
			{
				var tagName = base._productTagTable.AsEnumerable().First(r => r["ProductTagId"].ToString() == row["ProductTagId"].ToString())["Name"];
				dynamic doc = new
				{
					id = row["ProductTagId"].ToString(),
					name = tagName,
				};
				docs.Add(doc);
			}
			return docs.ToArray();
		}

		#endregion
	}
}
