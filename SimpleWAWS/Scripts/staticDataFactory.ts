angular.module("tryApp")
    .factory("staticDataFactory", () => {
        return {
            getAppServices: () => [{
                name: "Web",
                sprite: "sprite-WebApp",
                title: "Web App",
                steps: [{
                    id: 1,
                    title: "Select app type",
                    sref: "home",
                }, {
                    id: 2,
                    title: "Select template",
                    sref: "home.webapp.templates",
                    nextClass: "wa-button-primary",
                    nextText: "Create"
                }, {
                    id: 3,
                    title: "Work with your app",
                    sref: "home.webapp.work",
                    confirmDelete: true
                }],
                templates: []
            }, {
                name: "Mobile",
                sprite: "sprite-MobileApp",
                title: "Mobile App",
                steps: [{
                    id: 1,
                    title: "Select app type",
                    sref: "home",
                }, {
                    id: 2,
                    title: "Select template",
                    sref: "home.mobileapp.templates",
                    nextClass: "wa-button-primary",
                    nextText: "Create"
                }, {
                    id: 3,
                    title: "Download client",
                    sref: "home.mobileapp.clients",
                    confirmDelete: true
                }, {
                    id: 4,
                    title: "Work with your app",
                    sref: "home.mobileapp.work"
                }],
                templates: []
            }, {
                name: "Api",
                sprite: "sprite-ApiApp",
                title: "API App",
                steps: [{
                    id: 1,
                    title: "Select app type",
                    sref: "home"
                }, {
                    id: 2,
                    title: "Coming soon",
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
                title: "Logic App",
                steps: [{
                    id: 1,
                    title: "Select app type",
                    sref: "home"
                }, {
                    id: 2,
                    title: "Select template",
                    sref: "home.logicapp.templates",
                    nextClass: "wa-button-primary",
                    nextText: "Create"
                }, {
                    id: 3,
                    title: "Work with your app",
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
