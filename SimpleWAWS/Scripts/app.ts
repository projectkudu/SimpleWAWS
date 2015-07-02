angular.module("tryApp", ["ui.router", "angular.filter"])
    //http://stackoverflow.com/a/14996261/3234163
    .directive("selectOnClick", function () {
    return {
        restrict: "A",
        link: function(scope, element, attrs) {
            element.on("click", function (s) {
                this.select();
            });
        }
    };
})
    .config(["$stateProvider", "$urlRouterProvider", "$locationProvider", ($stateProvider: ng.ui.IStateProvider, $urlRouterProvider: ng.ui.IUrlRouterProvider, $locationProvider: ng.ILocationProvider) => {
    var homeState: ng.ui.IState = {
        name: "home",
        templateUrl: "templates/steps.html",
        controller: "appController"
    };

    var webApps: ng.ui.IState[] = [{
        name: "home.webapp",
        templateUrl: "templates/empty-shell.html"
    }, {
        name: "home.webapp.templates",
        templateUrl: "templates/templates.html"
    }, {
        name: "home.webapp.work",
        templateUrl: "templates/work.html"
    }];

    var mobileApps: ng.ui.IState[] = [{
        name: "home.mobileapp",
        templateUrl: "templates/empty-shell.html",
    }, {
        name: "home.mobileapp.templates",
        templateUrl: "templates/templates.html",
    }, {
        name: "home.mobileapp.clients",
        templateUrl: "templates/clients.html",
    }, {
        name: "home.mobileapp.work",
        templateUrl: "templates/work.html",
    }];

    var apiApps: ng.ui.IState[] = [{
        name: "home.apiapp",
        templateUrl: "/templates/empty-shell.html",
    }, {
        name: "home.apiapp.templates",
        templateUrl: "/templates/templates.html",
    }, {
        name: "home.apiapp.work",
        templateUrl: "/templates/work.html",
    }, {
        name: "home.apiapp.comingsoon",
        templateUrl: "/templates/comingsoon.html",
    }];

    var logicApps: ng.ui.IState[] = [{
        name: "home.logicapp",
        templateUrl: "templates/empty-shell.html",
    }, {
        name: "home.logicapp.comingsoon",
        templateUrl: "templates/comingsoon.html",
    }];
    $stateProvider.state(homeState);
    webApps.forEach(s => $stateProvider.state(s));
    mobileApps.forEach(s => $stateProvider.state(s));
    apiApps.forEach(s => $stateProvider.state(s));
    logicApps.forEach(s => $stateProvider.state(s));
    $locationProvider.html5Mode(true);

}])
    .controller("appController", ["$scope", "$http", "$timeout", "$rootScope", "$state", "$location", function ($scope: IAppControllerScope, $http: ng.IHttpService, $timeout: ng.ITimeoutService, $rootScope: ITryRootScope, $state: ng.ui.IStateService, $location: ng.ILocationService) {

    $scope.getLanguage = (template) => {
        return template.language;
    };


    $scope.appServices = [{
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
            onPrevious: () => { $scope.confirmDelete = true; }
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
            onPrevious: () => { $scope.confirmDelete = true; }
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
            //    onPrevious: () => { $scope.confirmDelete = true; }
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
                title: "Coming soon",
                sref: "home.logicapp.comingsoon"
            }],
            templates: []
        }];

    $scope.mobileClients = [{
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
    }];

    $scope.selectAppService = (appService) => {
        $scope.currentAppService = appService;
        $scope.setNextAndPreviousSteps(0);
        //TODO: better way to get default language
        $scope.ngModels.selectedLanguage = $scope.currentAppService.templates[0].language ? "Default" : undefined;
        $scope.selectedTemplate = $scope.ngModels.selectedLanguage
            ? $scope.currentAppService.templates.find(t => t.language === $scope.ngModels.selectedLanguage)
            : $scope.currentAppService.templates[0];
    };

    $scope.selectMobileClient = (client) => {
        $scope.selectedMobileClient = client;
    };

    $scope.nextState = (index) => {
        if (index < $scope.currentAppService.steps.length) {
            return $scope.currentAppService.steps[index].sref;
        }
        return undefined;
    };

    $scope.setNextAndPreviousSteps = (index) => {
        $scope.currentStep = $scope.currentAppService.steps[index];
        $scope.nextStep = $scope.currentAppService.steps[index + 1];
        $scope.previousStep = $scope.currentAppService.steps[index - 1];
    };

    $scope.currentAppService = $scope.appServices[0];
    $scope.setNextAndPreviousSteps(0);




    $rootScope.$on('$stateChangeStart',(event, toState, toParams, fromState, fromParams) => {
        delete $scope.ngModels.errorMessage;
        var step = $scope.currentAppService.steps.find((s) => s.sref === toState.name);
        $scope.setNextAndPreviousSteps(step.id - 1);
    });

    $scope.getStateLink = (step) => {
        return $state.href(step.sref);
    };

    $scope.selectTemplate = (template) => {
        $scope.selectedTemplate = template;
    };

    $scope.changeLanguage = () => {
        $scope.selectedTemplate = $scope.currentAppService.templates.find(t => t.language === $scope.ngModels.selectedLanguage);
    };

    $scope.goToNextState = () => {
        if ($scope.currentStep.nextText === "Create") {
            createResource();
        } else {
            $state.go($scope.nextStep.sref);
        }
    };

    $scope.deleteResource = (dontGoBack) => {
        $rootScope.deleteResourceClick();
        $scope.confirmDelete = false;
        $scope.running = true;
        return $http({
            url: "api/resource",
            method: "DELETE"
        })
            .success(() => {
            delete $scope.resource;
            if (!dontGoBack)
                $state.go($scope.previousStep.sref)
        })
            .error((e) => $scope.ngModels.errorMessage = e.Message)
            .finally(() => $scope.running = false);
    };

    $scope.goToPreviousState = () => {
        if ($scope.currentStep.onPrevious) {
            $scope.currentStep.onPrevious();
        } else {
            $state.go($scope.previousStep.sref);
        }
    };

    $scope.handleLoginClick = (method) => {
        createResource(method);
        $scope.loginOptions = false;
        $scope.showFullBlackBlocker = true;
    };

    $scope.dismissSiteExpired = () => {
        delete $scope.siteExpired;
        delete $scope.resource;
        $state.go($scope.currentAppService.steps[1].sref);
    };

    initUser();
    initTemplates().finally(() => initState());

    function initUser() {
        if (Cookies.get("uinit")) return;

        $http({
            method: "POST",
            url: "api/telemetry/INIT_USER",
            data: {
                origin: document.referrer,
                cid: $location.search().cid,
                sv: $location.search().sv
            }
        });
        var now = new Date();
        now.setMinutes(now.getMinutes() + 30);
        Cookies.set("uinit", "1");
    }

    function initTemplates() {
        $scope.loginOptions = false;
        $scope.running = true;
        $scope.offerDeleteAndCreate = false;
        $scope.showFullBlackBlocker = false;
        $scope.ngModels = {};
        $scope.resource = {};
        $scope.selectedMobileClient = $scope.mobileClients[0];

        $state.go("home");

        return $http({
            method: "GET",
            url: "api/templates"
        })
            .success((data: ITemplate[]) => {
            $scope.appServices.forEach(a => {
                a.templates = data.filter(e => e.appService === a.name);
            });
            //TODO: better way to choose default language
            $scope.ngModels.selectedLanguage = "Default";//$scope.currentAppService.templates[0].language;
            $scope.selectedTemplate = $scope.currentAppService.templates.find(t => t.language === $scope.ngModels.selectedLanguage);
        });
    }

    function initState() {
        if ($location.search().autoCreate || $location.search().githubRepo) {
            handleNoResourceInitState();
        } else {
            $scope.initExistingState();
        }
    }

    $scope.initExistingState = () => {
        $http({
            url: "api/resource",
            method: "GET"
        }).success((data: any) => {
            if (!data) {
                handleNoResourceInitState();
            } else {
                $scope.resource = data;
                $scope.selectAppService($scope.appServices.find(a => a.name === data.AppService));
                $state.go("home." + data.AppService.toLowerCase() + "app.work");
                startCountDown(data.timeLeftString);
            }
        }).error((err) => {
            handleNoResourceInitState();
        }).finally(() => {
            $scope.running = false;
            $scope.offerDeleteAndCreate = false;
        });
    }

    $scope.deleteAndCreateResource = () => {
        $scope.offerDeleteAndCreate = false;
        $scope.deleteResource(true).finally(() => {
            createResource();
        });
    };

    function handleNoResourceInitState() {
        if ($location.search().githubRepo) {
            selectAppService("Web");
            $scope.ngModels.selectedLanguage = "Github Repo";
            $scope.selectTemplate({
                appService: "Web",
                language: "Github Repo",
                name: "Github Repo",
                githubRepo: $location.search().githubRepo
            });
            autoCreateIfRequired(true);
            clearQueryString();
        } else if ($location.search().appServiceName || $location.search().appservice) {
            selectAppService();
            selectLanguage();
            selectTemplate();
            autoCreateIfRequired();
            clearQueryString();
        } else if ($location.search().language) {
            selectAppService("Web");
            selectLanguage();
            selectTemplate();
            autoCreateIfRequired();
            clearQueryString();
        }
    }

    function autoCreateIfRequired(force?: boolean) {
        var autoCreate = $location.search().autoCreate || force;
        $state.go($scope.currentAppService.steps[1].sref).then(() => {
            if (autoCreate) {
                $scope.goToNextState();
            }
        });
    }

    function selectLanguage() {
        if ($location.search().language) {
            var searchLanguage = $location.search().language === "cs" ? "C#" : $location.search().language;
            var correctTemplate = $scope.currentAppService.templates.find(t => t.language.toUpperCase() === searchLanguage.toUpperCase());
            $scope.ngModels.selectedLanguage = correctTemplate ? correctTemplate.language : "Default";
        }
    }

    function selectAppService(appService?: string) {
        var appServiceQuery: string = appService || $location.search().appServiceName || $location.search().appservice;
        $scope.selectAppService($scope.appServices.find(a => a.name.toUpperCase() === appServiceQuery.toUpperCase()));
    }

    function selectTemplate() {
        if ($location.search().name)
            $scope.selectTemplate($scope.currentAppService.templates.find(t => t.name === $location.search().name) || $scope.currentAppService.templates[0]);
        else
            $scope.selectTemplate($scope.currentAppService.templates.find(t => t.language ? t.language.toUpperCase() === $scope.ngModels.selectedLanguage.toUpperCase() : true) || $scope.currentAppService.templates[0]);
    }

    function createResource(method?: string) {
        $scope.running = true;
        $rootScope.createAppType($scope.currentAppService.name);
        $http({
            url: "api/resource"
                + "?appServiceName=" + encodeURIComponent($scope.currentAppService.name)
                + "&name=" + encodeURIComponent($scope.selectedTemplate.name)
                + ($scope.selectedTemplate.language ? "&language=" + encodeURIComponent($scope.selectedTemplate.language) : "")
                + ($scope.selectedTemplate.githubRepo ? "&githubRepo=" + encodeURIComponent($scope.selectedTemplate.githubRepo) : "")
                + "&autoCreate=true"
                + (method ? "&provider=" + method : ""),
            method: "POST",
            data: $scope.selectedTemplate
        }).success((data) => {
                $scope.resource = data;
                startCountDown($scope.resource.timeLeftString);
                $state.go($scope.nextStep.sref);
                $scope.running = false;
        }).error((err, status, headers) => {
            if (status === 403) {
                //show login options
                if (($scope.currentAppService.name === "Api" || method) && headers("LoginUrl")) {
                    (<any>window).location = headers("LoginUrl");
                } else {
                    $scope.loginOptions = true;
                }
            } else {
                if (err.Message === "You can't have more than 1 free resource at a time") {
                    $scope.offerDeleteAndCreate = true;
                } else {
                    $scope.ngModels.errorMessage = err.Message;
                }
            }
            $scope.running = false;
        }).finally(() => {
            $timeout(() => { $scope.showFullBlackBlocker = false; });
        });
    }

    function clearQueryString() {
        if (history.replaceState) {
            var newurl = window.location.protocol + "//" + window.location.host + window.location.pathname;
            window.history.replaceState({}, document.title, newurl);
        }
    }


function startCountDown(init) {
    if (init !== undefined) {
        var reg = '(\\d+)(m)?(:)(\\d+)(s)?';
        var pattern = new RegExp(reg, "i");
        var match = pattern.exec(init);
        var expireDateTime = new Date();
        expireDateTime.setMinutes(expireDateTime.getMinutes() + parseInt(match[1]));
        expireDateTime.setSeconds(expireDateTime.getSeconds() + parseInt(match[4]));
        countDown(expireDateTime);
    }
}

function countDown(expireDateTime) {
    if ($scope.resource) {
        var now: any = new Date();
        var diff = expireDateTime - now;
        if (diff <= 0) {
            $scope.timeLeft = "00m:00s";
            $scope.siteExpired = true;
            return;
        }
        diff = diff / 1000;
        $scope.timeLeft = ("0" + Math.floor(diff / 60)).slice(-2) + "m:" + ("0" + Math.floor(diff % 60)).slice(-2) + "s";
        $timeout(() => countDown(expireDateTime), 1000);
    }
}

}])
    .run(["$rootScope", "$state", "$stateParams", "$http", "$templateCache", "$location", ($rootScope: ITryRootScope, $state: ng.ui.IStateService, $stateParams: ng.ui.IStateParamsService, $http: ng.IHttpService, $templateCache: ng.ITemplateCacheService, $location: ng.ILocationService) => {
    $rootScope.$state = $state;
    $rootScope.$stateParams = $stateParams;
    $rootScope.freeTrialClick = (place) => {
        uiTelemetry("FREE_TRIAL_CLICK", { pagePlace: place});
    };

    $rootScope.ibizaClick = () => {
        uiTelemetry("IBIZA_CLICK");
    };

    $rootScope.monacoClick = () => {
        uiTelemetry("MONACO_CLICK");
    };

    $rootScope.downloadContentClick = () => {
        uiTelemetry("DOWNLOAD_CONTENT_CLICK");
    };

    $rootScope.downloadPublishingProfileClick = () => {
        uiTelemetry("DOWNLOAD_PUBLISHING_PROFILE_CLICK");
    };

    $rootScope.gitLinkClick = () => {
        uiTelemetry("GIT_LINK_CLICK");
    };

    $rootScope.downloadMobileClient = (clientType) => {
        uiTelemetry("DOWNLOAD_MOBILE_CLIENT", {clientType : clientType});
    };

    $rootScope.deleteResourceClick = () => {
        uiTelemetry("DELETE_RESOURCE_CLICK");
    };

    $rootScope.logout = () => {
        function deleteAllCookies() {
            var cookies = document.cookie.split(";");
            for (var i = 0; i < cookies.length; i++) {
                var cookie = cookies[i];
                var eqPos = cookie.indexOf("=");
                var name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;
                if (name !== "uinit" && name !== "aus")
                    document.cookie = name + "=;expires=Thu, 01 Jan 1970 00:00:00 GMT";
            }
        }
        deleteAllCookies();
        window.location.replace('https://' + window.location.host + '/');
    };

    $rootScope.cachedQuery = "";
    $(document).ready(init);
    function init() {
        var referrer = getReferer();
        var sourceVariation = getSourceVariation();

        if (referrer && referrer === "aspnet" || sourceVariation === "develop-aspnet") {
            $rootScope.branding = "aspnet";
        } else if (sourceVariation === "mkt-b15.22") {
            $rootScope.branding = "mkt-b15.22";
        }

        $rootScope.experiment = Cookies.get("exp2");

        var cleanUp = (s: string) => s ? s.replace("_", "") : "-";
        $rootScope.cachedQuery = "try_websites_"
        + cleanUp(Cookies.get("exp1"))
        + "_"
        + cleanUp(getReferer())
        + "_"
        + cleanUp(getSourceVariation())
        + "_"
        + cleanUp(Cookies.get("type"));
    };

    $rootScope.createAppType = (appType) => {
        var value = Cookies.get("type");
        if (value && value !== appType.toLocaleLowerCase()) {
            value = "mix";
        } else {
            value = appType.toLocaleLowerCase();
        }
        Cookies.set("type", value);
        init();
    };

    var refererNameLookup = [
        { match: /http(s)?:\/\/azure\.microsoft\.com\/([a-z]){2}-([a-z]){2}\/services\/app-service\/.*/, name: "acomaslp"},
        { match: /http(s)?:\/\/azure\.microsoft\.com\/([a-z]){2}-([a-z]){2}\/documentation\/.*/, name: "acomasdoc"},
        { match: /http(s)?:\/\/azure\.microsoft\.com\/([a-z]){2}-([a-z]){2}\/develop\/net\/aspnet\/.*/, name: "aspnet"},
        { match: /http(s)?:\/\/[a-z]*(\.)?google\.com\/.*/, name: "search"},
        { match: /http(s)?:\/\/[a-z]*(\.)?bing\.com\/.*/, name: "search"},
        { match: /http(s)?:\/\/[a-z]*(\.)?yahoo\.com\/.*/, name: "search"},
        { match: /http(s)?:\/\/ad\.atdmt\.com\/.*/, name: "ad"},
        { match: /http(s)?:\/\/[a-z]*(\.)?doubleclick\.net\/.*/, name: "ad"},
        { match: /http(s)?:\/\/[a-z]*(\.)?chango\.com\/.*/, name: "ad"},
        { match: /http(s)?:\/\/[a-z]*(\.)?media6degrees\.com\/.*/, name: "ad"}
    ];

    function getReferer(): string {
        var storedOrigin = Cookies.get("origin");
        if (!document.referrer || document.referrer === "") return storedOrigin;
        var catagory = refererNameLookup.find(e => e.match.test(document.referrer));
        storedOrigin = catagory ? catagory.name : "unc";
        Cookies.set("origin", storedOrigin);
        return storedOrigin;
    }

    function getSourceVariation(): string {
        var sv = $location.search().sv;
        if (sv) {
            Cookies.set("sv", sv);
            return sv;
        }
        else {
            return Cookies.get("sv");
        }
    }

    //http://stackoverflow.com/a/23522925/3234163
    var url;
    $state.get().forEach((s) => {
        if (url = s.templateUrl) {
            $http.get(url, { cache: $templateCache });
        }
    });

    function uiTelemetry(event: string, properties?: any) {
        $http({
            url: "/api/telemetry/" + event,
            method: "POST",
            data: properties
        });
    }

    $state.go("home");
    }])
    .filter("filterBySelectedLanguage",() => {
        return (templates: ITemplate[], language: string): any => {
            if (language === undefined)
                return templates;
            else
                return templates.filter(t => t.language.toUpperCase() === language.toUpperCase());
        };
    })
    .config(["$httpProvider", function ($httpProvider) {
        $httpProvider.defaults.headers.common["X-Requested-With"] = "XMLHttpRequest";
    }]);
