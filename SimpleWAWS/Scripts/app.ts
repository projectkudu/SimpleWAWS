angular.module("tryApp", ["ui.router", "angular.filter"]);
angular.module("tryApp")
    .controller("appController", ["$scope", "$http", "$timeout", "$rootScope", "$state", "$location", "staticDataFactory", appController]);

function appController($scope: IAppControllerScope, $http: ng.IHttpService, $timeout: ng.ITimeoutService, $rootScope: ITryRootScope, $state: ng.ui.IStateService, $location: ng.ILocationService, staticDataFactory: IStaticDataFactory) {

    $scope.getLanguage = (template) => {
        return template.language;
    };

    $scope.appServices = staticDataFactory.getAppServices();

    $scope.mobileClients = staticDataFactory.getMobileClients();

    var _globalDelayBindTracker = 0;
    function delayBind(f: Function) {

        var localDelayBindTracker = ++_globalDelayBindTracker;
        $timeout(() => {
            if (localDelayBindTracker === _globalDelayBindTracker) {
                f();
            }
        }, 350);
    }

    $scope.onAppServiceMouseOver = (appService: IAppService) => {
        delayBind(() => $scope.ngModels.hoverAppService = appService);
    };

    $scope.onAppServiceMouseLeave = () => {
        delayBind(() => delete $scope.ngModels.hoverAppService);
    };

    $scope.onTemplateMouseOver = (template: ITemplate) => {
        delayBind(() => $scope.ngModels.hoverTemplate = template);
    };

    $scope.onTemplateMouseLeave = () => {
        delayBind(() => delete $scope.ngModels.hoverTemplate);
    };

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




    $rootScope.$on('$stateChangeStart', (event, toState, toParams, fromState, fromParams) => {
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
        if ($scope.currentStep.nextText === Resources.Action_Create) {
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
        if ($scope.currentStep.confirmDelete) {
            $scope.confirmDelete = true;
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
        pullForStatus = true;
        var promise = $http({
            url: "api/resource"
            + "?appServiceName=" + encodeURIComponent($scope.currentAppService.name)
            + "&name=" + encodeURIComponent($scope.selectedTemplate.name)
            + ($scope.selectedTemplate.language ? "&language=" + encodeURIComponent($scope.selectedTemplate.language) : "")
            + ($scope.selectedTemplate.githubRepo ? "&githubRepo=" + encodeURIComponent($scope.selectedTemplate.githubRepo) : "")
            + "&autoCreate=true"
            + (method ? "&provider=" + method : ""),
            method: "POST",
            data: $scope.selectedTemplate
        })
        .success((data) => {
            $scope.resource = data;
            startCountDown($scope.resource.timeLeftString);
            $state.go($scope.nextStep.sref);
            $scope.running = false;
        })
        .error((err, status, headers) => {
            if (status === 403) {
                //show login options
                if (($scope.currentAppService.name === "Api" || $scope.currentAppService.name === "Logic" || method) && headers("LoginUrl")) {
                    (<any>window).location = headers("LoginUrl");
                } else {
                    $scope.loginOptions = true;
                }
            } else {
                if (err.Message === Resources.Information_YouCantHaveMoreThanOne) {
                    $scope.offerDeleteAndCreate = true;
                } else {
                    $scope.ngModels.errorMessage = err.Message;
                }
            }
            $scope.running = false;
        })
        .finally(() => {
            pullForStatus = false;
            delete $scope.ngModels.statusMessage;
            $timeout(() => { $scope.showFullBlackBlocker = false; });
        });
        //$timeout(startStatusPull, 1000);
    }

    var pullForStatus = false;
    var dots = "";
    function startStatusPull() {
        if (pullForStatus) {
            $http
                .get("/api/resource/status")
                .success(d => {
                    dots = (dots.length > 4 || $scope.ngModels.statusMessage !== d) ? "." : dots + ".";
                    $scope.ngModels.statusMessage = d + dots;
                    $timeout(startStatusPull, 2000);
                });
        } else {
            delete $scope.ngModels.statusMessage;
        }
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

}
