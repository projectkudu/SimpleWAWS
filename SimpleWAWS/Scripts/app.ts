angular.module("tryApp", ["ui.router"])
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
        templateUrl: "templates/select.html",
        url: "/mobileapp",
    }, {
            name: "home.mobileapp.templates",
            templateUrl: "templates/templates.html",
            url: "/templates?language&name"
        }, {
            name: "home.mobile.clients",
            templateUrl: "templates/clients.html",
            url: "/clients"
        }, {
            name: "home.mobile.work",
            templateUrl: "templates/work.html",
            url: "/work"
        }];

    var apiApps: ng.ui.IState[] = [{
        name: "home.apiapp",
        templateUrl: "/templates/select.html",
        url: "/apiapp",
    }, {
            name: "home.apiapp.templates",
            templateUrl: "/templates/templates.html",
            url: "/templates?language&name"
        }, {
            name: "home.apiapp.work",
            templateUrl: "/templates/work.html",
            url: "/work"
        }];

    $stateProvider.state(homeState);
    webApps.forEach(s => $stateProvider.state(s));
    mobileApps.forEach(s => $stateProvider.state(s));
    apiApps.forEach(s => $stateProvider.state(s));

    $urlRouterProvider.otherwise("/");
    $locationProvider.html5Mode(true);

}])
    .controller("appController", ["$scope", "$http", "$timeout", function ($scope: IAppControllerScope, $http: ng.IHttpService, $timeout) {


    var appServices: IAppService[] = [{
        name: "webapp",
        sprite: "",
        steps: [{
            id: 1,
            title: "Select your App Service",
            sref: "home.webapp",
            previous: undefined,
            next: "home.webapp.templates",
            onNext: () => { console.log("onNext home.webapp"); },
            onPrevious: () => { console.log("can't happen"); }
        }, {
            id: 2,
            title: "Select template and create",
            sref: "home.webapp.templates",
            previous: "home.webapp",
            next: "home.webapp.work",
            onNext: () => { console.log("onNext from home.webapp.templates"); },
            onPrevious: () => { }
        }, {
            id: 3,
            title: "Work with your app",
            sref: "home.webapp.work",
            previous: "home.webapp.work",
            next: undefined,
            onNext: () => { },
            onPrevious: () => { }
        }],
        templates: []
    }, {
        name: "webapp",
        sprite: "",
        steps: [{
            id: 1,
            title: "Select your App Service",
            sref: "home.webapp",
            previous: undefined,
            next: "home.webapp.templates",
            onNext: () => { console.log("onNext home.webapp"); },
            onPrevious: () => { console.log("can't happen"); }
        }, {
            id: 2,
            title: "Select template and create",
            sref: "home.webapp.templates",
            previous: "home.webapp",
            next: "home.webapp.work",
            onNext: () => { console.log("onNext from home.webapp.templates"); },
            onPrevious: () => { }
            }, {
                id: 3,
                title: "download your client",
                sref: "home.mobileapp.client",
                previous: undefined,
                next: undefined,
                onNext: () => { },
                onPrevious: () => { }
            }, {
            id: 4,
            title: "Work with your app",
            sref: "home.webapp.work",
            previous: "home.webapp.work",
            next: undefined,
            onNext: () => { },
            onPrevious: () => { }
        }],
        templates: []
    }];
    $scope.currentAppService = appServices[0];

    $scope.nextState = (index) => {
        if (index < $scope.currentAppService.steps.length) {
            return $scope.currentAppService.steps[index].sref;
        }
        return undefined;
    };
    var flag = true;
    $scope.swap = () => {
        $timeout(() => {
            $scope.currentAppService = flag ? appServices[1] : appServices[0];
            flag = !flag;
        });
    };
}]);
