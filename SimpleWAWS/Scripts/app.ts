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
        url: "/",
        //abstract: true,
        templateUrl: "templates/steps.html",
        controller: "appController"
    };

    var webApps: ng.ui.IState[] = [{
        name: "home.webapp",
        url: "webapp",
        templateUrl: "templates/empty-shell.html"
    }, {
            name: "home.webapp.templates",
            templateUrl: "templates/templates.html",
            url: "/templates?language&name"
        }, {
            name: "home.webapp.work",
            templateUrl: "templates/work.html",
            url: "/work"
        }];

    var mobileApps: ng.ui.IState[] = [{
        name: "home.mobileapp",
        templateUrl: "templates/empty-shell.html",
        url: "mobileapp",
    }, {
            name: "home.mobileapp.templates",
            templateUrl: "templates/templates.html",
            url: "/templates?language&name"
        }, {
            name: "home.mobileapp.clients",
            templateUrl: "templates/clients.html",
            url: "/clients"
        }, {
            name: "home.mobileapp.work",
            templateUrl: "templates/work.html",
            url: "/work"
        }];

    var apiApps: ng.ui.IState[] = [{
        name: "home.apiapp",
        templateUrl: "/templates/empty-shell.html",
        url: "apiapp",
    }, {
            name: "home.apiapp.templates",
            templateUrl: "/templates/templates.html",
            url: "/templates?language&name"
        }, {
            name: "home.apiapp.work",
            templateUrl: "/templates/work.html",
            url: "/work"
        }];

    var logicApps: ng.ui.IState[] = [{
        name: "home.logicapp",
        templateUrl: "templates/empty-shell.html",
        url: "logicapp"
    }, {
            name: "home.logicapp.comingsoon",
            templateUrl: "templates/comingsoon.html",
            url: "logicapp/comingsoon"
        }];
    $stateProvider.state(homeState);
    webApps.forEach(s => $stateProvider.state(s));
    mobileApps.forEach(s => $stateProvider.state(s));
    apiApps.forEach(s => $stateProvider.state(s));
    logicApps.forEach(s => $stateProvider.state(s));

    $urlRouterProvider.otherwise("/webapp");
    $locationProvider.html5Mode(true);

}])
    .controller("appController", ["$scope", "$http", "$timeout", "$rootScope", "$state", "$location", function ($scope: IAppControllerScope, $http: ng.IHttpService, $timeout: ng.ITimeoutService, $rootScope: ng.IRootScopeService, $state: ng.ui.IStateService, $location: ng.ILocationService) {


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
                sref: "home",
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
                onPrevious: () => { $scope.confirmDelete = true; }
                }],
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
        sprite: "sprite-Windows",
        steps: {
            preText: "Install Visual Studio Professional 2013 (Update 4)",
            preHref: "https://go.microsoft.com/fwLink/?LinkID=391934&clcid=0x409",
            clientText: "Download the Windows client app",
            clientHref: "/api/resource/mobileclient/Windows"
        }
    }, {
        name: "Native iOS",
        icon_url: "/Content/images/ios.png",
        sprite: "sprite-ios",
        steps: {
            preText: "Install Xcode (v4.4+)",
            preHref: "https://go.microsoft.com/fwLink/?LinkID=266532&clcid=0x409",
            clientText: "Download the iOS client app",
            clientHref: "/api/resource/mobileclient/NativeiOS"
        }

    }, {
        name: "Xamarin iOS",
        icon_url: "/Content/images/xamarin.png",
        sprite: "sprite-Xamarin",
        steps: {
            preText: "Install Xamarin Studio for Windows or OS X",
            preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
            clientText: "Download the Xamarin iOS client app",
            clientHref: "/api/resource/mobileclient/XamariniOS"
        }

    }, {
        name: "Xamarin Android",
        icon_url: "/Content/images/xamarin.png",
        sprite: "sprite-Xamarin",
        steps: {
            preText: "Install Xamarin Studio for Windows or OS X",
            preHref: "https://go.microsoft.com/fwLink/?LinkID=330242&clcid=0x409",
            clientText: "Download the Xamarin Android client app",
            clientHref: "/api/resource/mobileclient/XamarinAndroid"
        }
    }]

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

    $scope.deleteResource = () => {
        $scope.confirmDelete = false;
        $scope.running = true;
        $http({
            url: "api/resource",
            method: "DELETE"
        })
            .success(() => $state.go($scope.previousStep.sref))
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
    };

    $scope.dismissSiteExpired = () => {
        delete $scope.siteExpired;
        delete $scope.resource;
        $state.go($scope.currentAppService.steps[1].sref);
    };

    initTemplates().finally(() => initState());

    function initTemplates() {

        $scope.loginOptions = false;
        $scope.running = true;
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
        if ($location.search().appServiceName) {
            $scope.selectAppService($scope.appServices.find(a => a.name === $location.search().appServiceName));
            $scope.selectTemplate($scope.currentAppService.templates.find(t => t.name === $location.search().name));
            if ($location.search().language) {
                $scope.ngModels.selectedLanguage = $location.search().language;
            }
            var autoCreate = $location.search().autoCreate;
            $state.go($scope.currentAppService.steps[1].sref).then(() => {
                if (autoCreate) {
                    $scope.goToNextState();
                }
            });
            $scope.running = false;
        } else {
            $http({
                url: "api/resource",
                method: "GET"
            }).success((data: any) => {
                if (!data) return;
                $scope.resource = data;
                $scope.selectAppService($scope.appServices.find(a => a.name === data.AppService));
                $state.go("home." + data.AppService.toLowerCase() + "app.work");
                startCountDown(data.timeLeftString);
            }).error((err) => {
            }).finally(() => {
                $scope.running = false;
            });
        }
    }

    function createResource(method?: string) {
        $scope.running = true;
        $http({
            url: "api/resource" + "?appServiceName=" + $scope.currentAppService.name + "&name=" + $scope.selectedTemplate.name + ($scope.selectedTemplate.language ? "&language=" + $scope.selectedTemplate.language : "") + "&autoCreate=true" + (method ? "&provider=" + method : ""),
            method: "POST",
            data: $scope.selectedTemplate
        }).success((data) => {
            var waitMoreBecauseAADAndIbiza = $scope.currentAppService.name == "Api" ? 10000 : 1000;
            $timeout(() => {
                $scope.resource = data;
                startCountDown($scope.resource.timeLeftString);
                $state.go($scope.nextStep.sref);
                $scope.running = false;
            }, waitMoreBecauseAADAndIbiza);
        }).error((err, status, headers) => {
            if (status === 403) {
                //show login options
                if (($scope.currentAppService.name === "Api" || method) && headers("LoginUrl")) {
                    (<any>window).location = headers("LoginUrl");
                } else {
                    $scope.loginOptions = true;
                }
            } else {
                $scope.ngModels.errorMessage = err.Message;
            }
            $scope.running = false;
        });
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
    .run(($rootScope, $state: ng.ui.IStateService, $stateParams: ng.ui.IStateParamsService) => {
    $rootScope.$state = $state;
    $rootScope.$stateParams = $stateParams;
    $state.go("home");
})
    .filter("filterBySelectedLanguage",() => {
    return (templates: ITemplate[], language: string): any => {
        if (language === undefined)
            return templates;
        else
            return templates.filter(t => t.language === language);
    };
    }).config(["$httpProvider", function ($httpProvider) {
    $httpProvider.defaults.headers.common["X-Requested-With"] = "XMLHttpRequest";
}]);
//.filter("orderByDefaultFirstSort",() => {
//    return (languages: string[]): string[]=> {

//    };
//});
