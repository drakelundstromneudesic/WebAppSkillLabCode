param location string = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: 'EmailForwardingKeyVault'
  location: location
  properties: {

    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: false
    accessPolicies: [
      {
        objectId: azureFunction.outputs.functionIdentity
        permissions: {
          secrets: [
            'all'
          ]
        }
        tenantId: subscription().tenantId
      }
    ]
  }
}

module azureFunction 'azure-function-module.bicep' = {
  name: 'azureFunction'
  params: {
    location: location
  }
}
