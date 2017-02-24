# SimpleWAWS

## Running:

#### Minimum requirements for settings:
1. `TryUserName`
2. `TryPassword`
3. `Subscriptions` *comma separated*
4. `EnableAuth`

`TryUserName` and `TryPassword` are the credentials for a user that has `Owner` or `Co-Admin` access on the `Subscriptions`.

`Subscriptions` can either be a comma separated list of `SubscriptionId` or a comma separated list of the subscriptions `displayName`

`EnableAuth` set to `False` if you don't care about testing the Authentication module.

### Settings for testing authentication module
1. `EnableAuth` set to `True` to enable Auth. Not defined means `True`
2. `BaseLoginUrl` AAD base login url. Use: `https://login.microsoftonline.com/{tenantId}/oauth2/authorize`
3. `AADAppId` Your AAD AppId. You can create a web application from Azure Portal in your AAD tenant
4. `LoginErrorPage` The page to return in case of a login error. Set to `/Sorry`
5. `FacebookAppId` Facebook AppId. Create a Facebook App from `https://developers.facebook.com`
6. `GoogleAppId` Google AppId. Create a Google App from `https://developers.google.com`
7. `AADIssuerKeys` Url for AAD keys. Use `https://login.microsoftonline.com/common/discovery/keys`
8. `GoogleIssuerCerts` Url for Google Cert. Use: `https://www.googleapis.com/oauth2/v1/certs`
9. `VkClientSecret` Vk.com ClientSecret. Create a Vk.com app from `https://vk.com/dev`
10. `VkClientId` Vk.com ClientId. Create a Vk.com app from `https://vk.com/dev`

You don't have to create all the different Apps. You can only create the ones you're interested in testing.

### Settings for testing RBAC
*RBAC doesn't work if ran locally*
## Code and branches

#### Branches
**master**: (https://tryappservice.azure.com)
> This is connected to production. After all scenarios are validated on next deploy to production and merge into master

**dev**: (https://tryappservice-next.azure.com)
> This is the next environment. This is never swapped, instead changes from dev get merged into master.

#### Development workflow

``` bash
# make sure you're working on the dev branch
> git checkout dev

# create your own personal branch based on dev
> git checkout -b ahmels-work

# make all your changes in your branch
# commit and push these changes to github
> git push origin ahmels-work -u

# open a pull request.
# once everything is good, merge, rebase and push
> git checkout dev
> git merge ahmels-work
> git pull --rebase

# fix any conflicts
> git push origin dev

This will get merged into master at a later date. 
```
### Other settings for other features
1. `TryTenantId` used for RBAC scenarios
2. `TryTenantName` used for RBAC scenarios
3. `SiteExpiryMinutes` number of minutes before the resource expires. Default: `59`
4. `GeoRegions` The geoRegions to use to create resources. Default: `East US,West US,North Europe,West Europe,South Central US,North Central US,East Asia,Southeast Asia,Japan West,Japan East,Brazil South`
5. `FreeSitesIISLogsBlob` Blob container name for saving logs
6. `FreeSitesIISLogsQueue` Azure storage queue to inform aggregation webJob of a new item in the blob storage above
7. `StorageConnectionString` connection string for storage used for logs above
8. `DocumentDbUrl` Used for DocumentDb logging
9. `DocumentDbKey` Used for DocumentDb logging
10. `FromEmail` Used for alert emails
11. `EmailServer` Used for alert emails
12. `EmailUserName` Used for alert emails
13. `EmailPassword` Used for alert emails
14. `ToEmail` Used for alert emails
15. `SearchServiceName` Required for the Azure Search template to work
16. `SearchServiceApiKeys` Required for the Azure Search template to work
17. `ExtendedResourceExpireHours` number of hours that that `SiteExpiryMinutes` can be extended to. Default: `24`
