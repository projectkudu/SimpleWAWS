angular.module("tryApp", ["ui.router", "angular.filter"])
    .config(["$stateProvider", "$urlRouterProvider", "$locationProvider", ($stateProvider: ng.ui.IStateProvider, $urlRouterProvider: ng.ui.IUrlRouterProvider, $locationProvider: ng.ILocationProvider) => {        var homeState: ng.ui.IState = {
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
    .config(["$httpProvider", function ($httpProvider) {
        $httpProvider.defaults.headers.common["X-Requested-With"] = "XMLHttpRequest";
    }]);
