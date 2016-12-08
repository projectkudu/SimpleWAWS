angular.module("tryApp")
    .config(["$stateProvider", "$urlRouterProvider", "$locationProvider", ($stateProvider: ng.ui.IStateProvider, $urlRouterProvider: ng.ui.IUrlRouterProvider, $locationProvider: ng.ILocationProvider) => {
        var homeState: ng.ui.IState = {
            name: "home",
            templateUrl: "templates/steps.cshtml",
            controller: "appController"
        };

        var webApps: ng.ui.IState[] = [{
            name: "home.webapp",
            templateUrl: "templates/empty-shell.cshtml"
        }, {
            name: "home.webapp.templates",
            templateUrl: "templates/templates.cshtml"
        }, {
            name: "home.webapp.work",
            templateUrl: "templates/work.cshtml"
        }];

        var mobileApps: ng.ui.IState[] = [{
            name: "home.mobileapp",
            templateUrl: "templates/empty-shell.cshtml",
        }, {
            name: "home.mobileapp.templates",
            templateUrl: "templates/templates.cshtml",
        }, {
            name: "home.mobileapp.clients",
            templateUrl: "templates/clients.cshtml",
        }, {
            name: "home.mobileapp.work",
            templateUrl: "templates/work.cshtml",
        }];

        var apiApps: ng.ui.IState[] = [{
            name: "home.apiapp",
            templateUrl: "/templates/empty-shell.cshtml",
        }, {
            name: "home.apiapp.templates",
            templateUrl: "/templates/templates.cshtml",
        }, {
            name: "home.apiapp.work",
            templateUrl: "/templates/work.cshtml",
        }];

        var logicApps: ng.ui.IState[] = [{
            name: "home.logicapp",
            templateUrl: "templates/empty-shell.cshtml",
        }, {
            name: "home.logicapp.templates",
            templateUrl: "/templates/templates.cshtml",
        }, {
            name: "home.logicapp.work",
            templateUrl: "/templates/work.cshtml",
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
