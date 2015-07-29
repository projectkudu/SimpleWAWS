angular.module("tryApp")
    .factory("staticDataFactory", () => {
        return {
            getAppServices: () => [{
                name: "Web",
                sprite: "sprite-WebApp",
                title: Resources.Information_WebApp,
                steps: [{
                    id: 1,
                    title: Resources.Action_SelectAppType,
                    sref: "home",
                }, {
                    id: 2,
                    title: Resources.Action_SelectTemplate,
                    sref: "home.webapp.templates",
                    nextClass: "wa-button-primary",
                    nextText: Resources.Action_Create
                }, {
                    id: 3,
                    title: Resources.Action_GenericWorkWithYourApp,
                    sref: "home.webapp.work",
                    confirmDelete: true
                }],
                templates: []
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
                    title: Resources.Action_SelectTemplate,
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
                templates: []
            }, {
                name: "Api",
                sprite: "sprite-ApiApp",
                title: Resources.Information_APIApp,
                steps: [{
                    id: 1,
                    title: Resources.Action_SelectAppType,
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
                templates: []
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
                    title: Resources.Action_SelectTemplate,
                    sref: "home.logicapp.templates",
                    nextClass: "wa-button-primary",
                    nextText: Resources.Action_Create
                }, {
                    id: 3,
                    title: Resources.Action_GenericWorkWithYourApp,
                    sref: "home.logicapp.work",
                    confirmDelete: true
                }],
                templates: []
            }],
            getMobileClients: () => [{
                name: "Windows",
                icon_url: "/Content/images/Windows.png",
                sprite: "mobile-icons sprite-Windows",
                steps: {
                    preText: "Install Visual Studio Professional 2013 (Update 4)",
                    preHref: "https://go.microsoft.com/fwLink/?LinkID=391934&clcid=0x409",
                    clientText: "Download the Windows client app",
                    clientHref: "/api/resource/mobileclient/Windows"
                }
            }, {
                name: "Native iOS",
                icon_url: "/Content/images/ios.png",
                sprite: "mobile-icons sprite-ios",
                steps: {
                    preText: "Install Xcode (v4.4+)",
                    preHref: "https://go.microsoft.com/fwLink/?LinkID=266532&clcid=0x409",
                    clientText: "Download the iOS client app",
                    clientHref: "/api/resource/mobileclient/NativeiOS"
                }

            }, {
                name: "Xamarin iOS",
                icon_url: "/Content/images/xamarin.png",
                sprite: "mobile-icons sprite-Xamarin",
                steps: {
                    preText: "Install Xamarin Studio for Windows or OS X",
                    preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
                    clientText: "Download the Xamarin iOS client app",
                    clientHref: "/api/resource/mobileclient/XamariniOS"
                }

            }, {
                name: "Xamarin Android",
                icon_url: "/Content/images/xamarin.png",
                sprite: "mobile-icons sprite-Xamarin",
                steps: {
                    preText: "Install Xamarin Studio for Windows or OS X",
                    preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
                    clientText: "Download the Xamarin Android client app",
                    clientHref: "/api/resource/mobileclient/XamarinAndroid"
                }
            }, {
                name: "Web Client",
                sprite: "mobile-icons sprite-javascript",
                steps: {
                    clientText: "Visit the web based client",
                    clientHref: "webClient"
                }
            }]
        };
    });
