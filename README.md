# Plumber - workflow for Umbraco

[![Build status](https://ci.appveyor.com/api/projects/status/ap94da7169wk0g0v?svg=true)](https://ci.appveyor.com/project/nathanwoulfe/umbracoworkflow)
[![NuGet release](https://img.shields.io/nuget/dt/Workflow.Umbraco.svg)](https://www.nuget.org/packages/Workflow.Umbraco)
[![Our Umbraco project page](https://img.shields.io/badge/our-umbraco-brightgreen.svg)](https://our.umbraco.org/projects/backoffice-extensions/plumber-workflow-for-umbraco)


Plumber adds a heap of useful bits and pieces to Umbraco, to allow multi-staged workflow approval for publish/unpublish actions. 

To get started, clone the repo, build the Workflow project (build action should do some copying), then start the Workflow.Site project (credentials below). Running localbuild.bat in /BuildPackage should generate a package in /BuildPackage/artifacts, while the default Grunt task in Workflow looks after the usual concat/minify/copy type tasks. localbuild.bat also runs the default Grunt task to ensure the built package is reasonably tidy.

In the backoffice, the new Workflow section has a documentation tab, which offers more explanation of features and processes, or you can [read the documentation here](Workflow/DOCS.md).

The workflow model is derived from the workflow solution developed by myself and the web team at [USC](http://www.usc.edu.au), but re-visions that basic three-step workflow into something much more flexible.

## Get started

### Grab the latest release from AppVeyor:

https://ci.appveyor.com/project/nathanwoulfe/umbracoworkflow/build/artifacts

### Or install via Nuget:

```Install-Package Workflow.Umbraco```

## Workflow.Site

Logins for the test site:

**Username**: EditorUser@mail.com<br />
**Password**: JOP{H#kG

**Username**: AdminUser@mail.com<br />
**Password**: tzX)TSiA

Users have different permissions - admin has the full set, editor is much more limited.

Other user accounts exist in the site, as do a range of workflow configurations.

The database is my development environment, so I'll likely introduce breaking changes (password, users deleted etc), but will try to remember not to remove the two listed above.

## Like it? Love it? 

Maintaining an open source product takes time and effort, which could be otherwise spent on paid work.

Since we live in the future, feel free to buy me a beer (or moon-lambo, depending on current prices) via a donation to one of the wallets below:

- NANO<br />**xrb_1dig7t3qjnwdtq53zrs9wc5mi9ry8doctdjwrgacs1oumqeykod9kdhxfpdu**
- ETH/ERC20<br />**0x8cbD3b158F604E9273Ac4887F90DaDCb254E9656**

If you're not familiar with Nano, [check it out](https://nano.org/en) - it's everything Bitcoin wanted to be. Instant, free and environmentally friendly.

