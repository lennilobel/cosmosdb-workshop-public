import azure.cosmos
from azure.cosmos import PartitionKey

# Delete the acme-webstore database if it already exists
try:
    cosmos_client.delete_database('acme-webstore')
    print('Deleted existing acme-webstore database')
except azure.cosmos.errors.CosmosHttpResponseError as e:
    if e.status_code != 404:
        raise
        
# Create the database
database = cosmos_client.create_database('acme-webstore')
print('Created database')

# Create the lease container
leaseContainer = database.create_container('lease', PartitionKey(path="/id"))
print('Created lease container')

# Create the cart container partitioned on /cartId with TTL enabled
cartContainer = database.create_container('cart', PartitionKey(path="/cartId"), default_ttl=-1)
print('Created cart container')

# Create the product container partitioned on /categoryId
productContainer = database.create_container('product', PartitionKey(path="/categoryId"))
print('Created product container')

# Create the productMeta container partitioned on /type
productMetaContainer = database.create_container('productMeta', PartitionKey(path="/type"))
print('Created productMeta container')
