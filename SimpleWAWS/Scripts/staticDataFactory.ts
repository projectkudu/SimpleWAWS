angular.module("tryApp")
    .factory("staticDataFactory", () => {
        return {
            getAppServices: (sv?: string): IAppService[] => [{
                name: "Web",
                sprite: "sprite-WebApp",
                title: Resources.Information_WebApp,
                steps: [{
                    id: 1,
                    title: Resources.Action_SelectAppType,
                    sref: "home",
                }, {
                    id: 2,
                    title: Resources.Information_SelectTemplate,
                    sref: "home.webapp.templates",
                    nextClass: "wa-button-primary",
                    nextText: Resources.Action_Create
                }, {
                    id: 3,
                    title: Resources.Action_GenericWorkWithYourApp,
                    sref: "home.webapp.work",
                    confirmDelete: true
                }],
                templates: [],
                description: Resources.Information_WebAppDescription
            }, {
                name: "Mobile",
                sprite: "sprite-MobileApp",
                title: Resources.Information_MobileApp,
                steps: [{
                    id: 1,
                    title: Resources.Action_SelectAppType,
                    sref: "home",
                }, {
                    id: 2,
                    title: Resources.Information_SelectTemplate,
                    sref: "home.mobileapp.templates",
                    nextClass: "wa-button-primary",
                    nextText: Resources.Action_Create
                }, {
                    id: 3,
                    title: Resources.Information_DownloadClient,
                    sref: "home.mobileapp.clients",
                    confirmDelete: true
                }, {
                    id: 4,
                    title: Resources.Action_GenericWorkWithYourApp,
                    sref: "home.mobileapp.work"
                }],
                templates: [],
                description: Resources.Information_MobileAppDescription
                },
                {
                name: "FunctionApp",
                sprite: "sprite-FunctionApp",
                title: Resources.Information_FunctionApp,
                steps: [{
                        id: 1,
                        title: Resources.Action_SelectAppType,
                        sref: "home"
                }],
                templates: [],
                description: Resources.Information_FunctionAppDescription
                },
                {
                name: "Api",
                sprite: "sprite-ApiApp",
                title: Resources.Information_APIApp,
                steps: [{
                    id: 1,
                    title: "Select app type",
                    sref: "home"
                }, {
                        id: 2,
                        title: "Select template",
                        sref: "home.apiapp.templates",
                        nextClass: "wa-button-primary",
                        nextText: "Create"
                    }, {
                        id: 3,
                        title: "Work with your app",
                        sref: "home.apiapp.work",
                        confirmDelete: true
                    }],
                templates: [],
                description: Resources.Information_ApiAppDescription
            }, {
                name: "Logic",
                sprite: "sprite-LogicApp",
                title: Resources.Information_LogicApp,
                steps: [{
                    id: 1,
                    title: Resources.Action_SelectAppType,
                    sref: "home"
                }, {
                    id: 2,
                    title: Resources.Information_SelectTemplate,
                    sref: "home.logicapp.templates",
                    nextClass: "wa-button-primary",
                    nextText: Resources.Action_Create
                }, {
                    id: 3,
                    title: Resources.Action_GenericWorkWithYourApp,
                    sref: "home.logicapp.work",
                    confirmDelete: true
                }],
                templates: [],
                description: Resources.Information_LogicAppDescription
                }, {
                    name: "Linux",
                    sprite: "sprite-LinuxApp",
                    title: "Linux App",
                    steps: [{
                        id: 1,
                        title: Resources.Action_SelectAppType,
                        sref: "home"
                    }, {
                            id: 2,
                            title: Resources.Information_SelectTemplate,
                            sref: "home.linuxapp.templates",
                            nextClass: "wa-button-primary",
                            nextText: Resources.Action_Create
                        }, {
                            id: 3,
                            title: Resources.Action_GenericWorkWithYourApp,
                            sref: "home.linuxapp.work",
                            confirmDelete: true
                        }],
                    templates: [],
                    description: "Linux App"
                }].filter((e) => { // HACK: This is a hack to filter App Type Selection for bdc campaign
                    if (sv && sv === "bdc") {
                        return e.name === "Web" || e.name === "Mobile";
                    } else {
                        return true;
                    }
            }),
            getMobileClients: (sampleName: string) => {
                //TODO: get list of available clients from the server like we do with templates
                if (sampleName === "Todo List") {
                    return [{
                        name: Resources.Information_NativeiOS,
                        internal_name: "Native iOS",
                        icon_url: "/Content/images/ios.png",
                        sprite: "mobile-icons sprite-ios",
                        steps: {
                            preText: Resources.Information_InstallXcode,
                            preHref: "https://go.microsoft.com/fwLink/?LinkID=266532&clcid=0x409",
                            clientText: Resources.Information_DownloadiOSClient,
                            clientHref: "/api/resource/mobileclient/NativeiOS?templateName=TodoList"
                        }

                    }, {
                       name: Resources.Information_XamariniOS,
                       internal_name: "Xamarin iOS",
                       icon_url: "/Content/images/xamarin.png",
                       sprite: "mobile-icons sprite-xamarin",
                       steps: {
                           preText: Resources.Information_InstallXamarinStudio,
                           preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
                           clientText: Resources.Information_DownloadXamariniOSClient,
                           clientHref: "/api/resource/mobileclient/XamariniOS?templateName=TodoList"
                       }

                   }, {
                       name: Resources.Information_XamarinAndroid,
                       internal_name: "Xamarin Android",
                       icon_url: "/Content/images/xamarin.png",
                       sprite: "mobile-icons sprite-xamarin",
                       steps: {
                           preText: Resources.Information_InstallVisualStudio,
                           preHref: "https://go.microsoft.com/fwLink/?LinkID=391934&clcid=0x409",
                           clientText: Resources.Information_DownloadXamarinAndroidClient,
                           clientHref: "/api/resource/mobileclient/XamarinAndroid?templateName=TodoList"
                       }
                   }, {
                       name: Resources.Information_XamarinForms,
                       internal_name: "Xamarin.Forms",
                       sprite: "mobile-icons sprite-xamarin",
                       steps: {
                           preText: Resources.Information_InstallVisualStudio,
                           preHref: "https://go.microsoft.com/fwLink/?LinkID=391934&clcid=0x409",
                           clientText: Resources.Information_DownloadXamarinFormsClient,
                           clientHref: "/api/resource/mobileclient/XamarinForms?templateName=TodoList"
                       }
                   }, {
                       name: Resources.Information_Windows,
                       internal_name: "Windows",
                       icon_url: "/Content/images/Windows.png",
                       sprite: "mobile-icons sprite-windows",
                       steps: {
                           preText: Resources.Information_InstallVisualStudio,
                           preHref: "https://go.microsoft.com/fwLink/?LinkID=391934&clcid=0x409",
                           clientText: Resources.Information_DownloadWindowsClient,
                           clientHref: "/api/resource/mobileclient/Windows?templateName=TodoList"
                       }
                   }, {
                       name: Resources.Information_UniversalWindowsPlatform,
                       internal_name: "UWP",
                       sprite: "mobile-icons sprite-uwp",
                       steps: {
                           preText: Resources.Information_InstallVisualStudio,
                           preHref: "https://go.microsoft.com/fwLink/?LinkID=391934&clcid=0x409",
                           clientText: Resources.Information_DownloadWUniversalWindowsPlatformClient,
                           clientHref: "/api/resource/mobileclient/UWP?templateName=TodoList"
                       }
                   }, {
                       name: Resources.Information_Android,
                       internal_name: "Andorid",
                       sprite: "mobile-icons sprite-android",
                       steps: {
                           preText: Resources.Information_InstallAndroidStudio,
                           preHref: "http://go.microsoft.com/fwlink/?LinkID=708403&clcid=0x409",
                           clientText: Resources.Information_DownloadAndroidClient,
                           clientHref: "/api/resource/mobileclient/Android?templateName=TodoList"
                       }
                   }];
                } else if (sampleName === "Field Engineer") {
                    return [{
                        name: Resources.Information_XamarinForms,
                        internal_name: "Xamarin Forms",
                        icon_url: "/Content/images/xamarin.png",
                        sprite: "mobile-icons sprite-xamarin",
                        steps: {
                            preText: Resources.Information_InstallXamarinStudio,
                            preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
                            clientText: Resources.Information_DownloadXamarinFormsClient,
                            clientHref: "/api/resource/mobileclient/XamarinForms?templateName=FieldEngineer"
                        }
                    }, {
                        name: Resources.Information_WebClient,
                        internal_name: "Web Client",
                        sprite: "mobile-icons sprite-javascript",
                        steps: {
                            preText: undefined,
                            preHref: undefined,
                            clientText: Resources.Information_VisitWebClient,
                            clientHref: "webClient"
                        }
                    }];
                } else if (sampleName === "Xamarin CRM") {
                    return [{
                        name: Resources.Information_XamariniOS,
                        internal_name: "Xamarin iOS",
                        icon_url: "/Content/images/xamarin.png",
                        sprite: "mobile-icons sprite-xamarin",
                        steps: {
                            preText: Resources.Information_InstallXamarinStudio,
                            preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
                            clientText: Resources.Information_DownloadXamariniOSClient,
                            clientHref: "/api/resource/mobileclient/XamariniOS?templateName=XamarinCRM"
                        }
                    }, {
                        name: Resources.Information_XamarinAndroid,
                        internal_name: "Xamarin Android",
                        icon_url: "/Content/images/xamarin.png",
                        sprite: "mobile-icons sprite-xamarin",
                        steps: {
                            preText: Resources.Information_InstallVisualStudio,
                            preHref: "https://go.microsoft.com/fwLink/?LinkID=391934&clcid=0x409",
                            clientText: Resources.Information_DownloadXamarinAndroidClient,
                            clientHref: "/api/resource/mobileclient/XamarinAndroid?templateName=XamarinCRM"
                        }
                    }];
                }
            }
        };
    });
