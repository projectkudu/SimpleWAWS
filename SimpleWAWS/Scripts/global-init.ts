angular.module("tryApp")
    .run(["$rootScope", "$state", "$stateParams", "$http", "$templateCache", "$location", ($rootScope: ITryRootScope, $state: ng.ui.IStateService, $stateParams: ng.ui.IStateParamsService, $http: ng.IHttpService, $templateCache: ng.ITemplateCacheService, $location: ng.ILocationService) => {
        $rootScope.$state = $state;
        $rootScope.$stateParams = $stateParams;
        $rootScope.showFeedback = false;
        $rootScope.submittedFeedback = false;
        $rootScope.comment = "";
        $rootScope.contactMe = false;
        $rootScope.currentCulture = CurrentCulture;

        $rootScope.showShareFeedback = () => {
            $rootScope.showFeedback = true;
        };

        $rootScope.submitFeedback = () => {
            $rootScope.feedbackResponse = Resources.Information_SubmittingFeedback;
            $http
                .post("/api/feedback", { comment: $rootScope.comment, contactMe: $rootScope.contactMe })
                .success((d) => {
                    $rootScope.feedbackResponse = Resources.Information_ThanksForFeedback;
                    $rootScope.submittedFeedback = true;
                })
                .error(() => $rootScope.feedbackResponse = Resources.Error_SubmittingFeedback);
        };

        $rootScope.cancelFeedback = () => {
            $rootScope.submittedFeedback = false;
            $rootScope.showFeedback = false;
            $rootScope.feedbackResponse = "";
        };

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

        $rootScope.downloadMobileClient = (clientType, templateName) => {
            uiTelemetry("DOWNLOAD_MOBILE_CLIENT", { clientType: clientType, templateName: templateName });
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
        $rootScope.freeTrialTopCachedQuery = "";
        $rootScope.freeTrialBottomCachedQuery = "";
        $rootScope.freeTrialExpireCachedQuery = "";
        $(document).ready(init);
        function init() {
            var referrer = getReferer();
            var sourceVariation = getSourceVariation();
            $rootScope.sourceVariation = sourceVariation;

            if (referrer && referrer === "aspnet" || sourceVariation === "develop-aspnet" || sourceVariation === "aspnetdirect") {
                $rootScope.branding = "aspnet";
            } else if (sourceVariation === "mkt-b15.22") {
                $rootScope.branding = "mkt-b15.22";
            } else if (sourceVariation === "azurecon") {
                $rootScope.branding = "azurecon";
            } else if (sourceVariation === "zend") {
                $rootScope.branding = "zend";
            } else if (sourceVariation === "azureplatform") {
                $rootScope.branding = "azureplatform";
            }

            $rootScope.experiment = Cookies.get("exp2");

            var cleanUp = (s: string) => s ? s.replace("_", "") : "-";
            var postfix = cleanUp(Cookies.get("exp1"))
                + "_"
                + cleanUp(getReferer())
                + "_"
                + cleanUp(getSourceVariation())
                + "_"
                + cleanUp(Cookies.get("type"));
            $rootScope.cachedQuery = "try_websites_" + postfix;
            $rootScope.freeTrialTopCachedQuery = "try_websitestop" + postfix;
            $rootScope.freeTrialBottomCachedQuery = "try_websitesbottom" + postfix;
            $rootScope.freeTrialExpireCachedQuery = "try_websitesexpire" + postfix;
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

        var n = navigator.appVersion.indexOf("MSIE") != -1 ? "click" : "mousedown";
        document.body.addEventListener(n, (e) => {
            var cleanUp = (s: string) => s ? s.replace("_", "") : "-";
            var wedcsCE = ["A", "IMG", "AREA", "INPUT"];
            var MscomIsInList = function (n) {
                for (var t in wedcsCE)
                    if (wedcsCE[t] == n.toUpperCase()) return 1;
                return 0
            }
            var t;
            for (t = e.srcElement || e.target; t.tagName && MscomIsInList(t.tagName) == 0;) t = t.parentElement || t.parentNode;
            $(t).attr({
                "ms.pagetype": cleanUp(Cookies.get("exp1")),
                "ms.pagearea": "",
                "ms.title": "Free Trial",
                "ms.sitename": "TryAppService",
                "ms.referrercontentid": cleanUp(getReferer()),
                "ms.referrerexitlinkid": cleanUp(getSourceVariation()),
                "ms.interactiontype": cleanUp(Cookies.get("type")),
                "ms.verbatim": $rootScope.selectedTemplate ? $rootScope.selectedTemplate.name : "none",
                "ms.controlname": "none"
            });
        });

        var refererNameLookup = [
            { match: /http(s)?:\/\/azure\.microsoft\.com\/([a-z]){2}-([a-z]){2}\/services\/app-service\//, name: "acomaslp"},
            { match: /http(s)?:\/\/azure\.microsoft\.com\/([a-z]){2}-([a-z]){2}\/services\/app-service\/\?.*/, name: "acomaslp"},
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
