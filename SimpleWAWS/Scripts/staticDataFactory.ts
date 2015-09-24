angular.module("tryApp")
    .factory("staticDataFactory", () => {
        return {
            getAppServices: (): IAppService[] => [{
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
            }, {
                name: "Api",
                sprite: "sprite-ApiApp",
                title: Resources.Information_APIApp,
                steps: [{
                    id: 1,
                    title: Resources.Information_SelectTemplate,
                    sref: "home"
                }, {
                    id: 2,
                    title: Resources.Information_ComingSoon,
                    sref: "home.apiapp.comingsoon"
                }],
                //steps: [{
                //    id: 1,
                //    title: "Select app type",
                //    sref: "home",
                //}, {
                //        id: 2,
                //        title: "Select template",
                //        sref: "home.apiapp.templates",
                //        nextClass: "wa-button-primary",
                //        nextText: "Create"
                //    }, {
                //        id: 3,
                //        title: "Work with your app",
                //        sref: "home.apiapp.work",
                //        confirmDelete: true
                //    }],
                templates: [],
                description: Resources.Information_ApiAppDescription
            }, {
                name: "Logic",
                sprite: "sprite-LogicApp",
                title: Resources.Information_LogicApp,
                //steps: [{
                //    id: 1,
                //    title: Resources.Action_SelectAppType,
                //    sref: "home"
                //}, {
                //    id: 2,
                //    title: Resources.Information_ComingSoon,
                //    sref: "home.logicapp.comingsoon"
                //}],
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
            }],
            getMobileClients: (sampleName: string) => {
                //TODO: get list of available clients from the server like we do with templates
                return sampleName === "Todo List"
                    ? [{
                        name: Resources.Information_Windows,
                        icon_url: "/Content/images/Windows.png",
                        sprite: "mobile-icons sprite-Windows",
                        steps: {
                            preText: Resources.Information_InstallVisualStudio,
                            preHref: "https://go.microsoft.com/fwLink/?LinkID=391934&clcid=0x409",
                            clientText: Resources.Information_DownloadWindowsClient,
                            clientHref: "/api/resource/mobileclient/Windows?templateName=TodoList"
                        }
                    }, {
                            name: Resources.Information_NativeiOS,
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
                            icon_url: "/Content/images/xamarin.png",
                            sprite: "mobile-icons sprite-Xamarin",
                            steps: {
                                preText: Resources.Information_InstallXamarinStudio,
                                preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
                                clientText: Resources.Information_DownloadXamariniOSClient,
                                clientHref: "/api/resource/mobileclient/XamariniOS?templateName=TodoList"
                            }

                        }, {
                            name: Resources.Information_XamarinAndroid,
                            icon_url: "/Content/images/xamarin.png",
                            sprite: "mobile-icons sprite-Xamarin",
                            steps: {
                                preText: Resources.Information_InstallXamarinStudio,
                                preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
                                clientText: Resources.Information_DownloadXamarinAndroidClient,
                                clientHref: "/api/resource/mobileclient/XamarinAndroid?templateName=TodoList"
                            }
                        }, {
                            name: Resources.Information_WebClient,
                            sprite: "mobile-icons sprite-javascript",
                            steps: {
                                clientText: Resources.Information_VisitWebClient,
                                clientHref: "webClient"
                            }
                        }]
                    : [{
                        name: Resources.Information_XamariniOS,
                        icon_url: "/Content/images/xamarin.png",
                        sprite: "mobile-icons sprite-Xamarin",
                        steps: {
                            preText: Resources.Information_InstallXamarinStudio,
                            preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
                            clientText: Resources.Information_DownloadXamariniOSClient,
                            clientHref: "/api/resource/mobileclient/XamariniOS?templateName=FieldEngineer"
                        }
                    }, {
                            name: Resources.Information_WebClient,
                            sprite: "mobile-icons sprite-javascript",
                            steps: {
                                clientText: Resources.Information_VisitWebClient,
                                clientHref: "webClient"
                            }
                        }];
            }
        };
    });
