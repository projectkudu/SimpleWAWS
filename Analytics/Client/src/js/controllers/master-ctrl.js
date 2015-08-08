/**
 * Master Controller
 */

angular.module('RDash')
    .controller('MasterCtrl', ['$scope', '$cookieStore', '$rootScope', '$http', '$q', MasterCtrl]);

function MasterCtrl($scope, $cookieStore, $rootScope, $http, $q) {
    /**
     * Sidebar Toggle & Cookie Control
     */
     $scope.model = {};
     init();
     function init() {
         var now = new Date();
         $scope.model.endTime = now.getFullYear() + '-' + (now.getMonth() + 1) + '-' + now.getDate();
         var twoWeeksAgo = get2SundaysBack();
         $scope.model.startTime = twoWeeksAgo.getFullYear() + '-' + (twoWeeksAgo.getMonth() + 1) + '-' + twoWeeksAgo.getDate();
         $scope.model.aggregate = 'Week';
     }

     function get2SundaysBack() {
         var d = new Date();
         if (d.getDay() === 0) d.setDate(d.getDate() -1);
        d.setDate((d.getDate() - d.getDay()));
        d.setDate(d.getDate() - 1);
        d.setDate((d.getDate() - d.getDay()));
        return d;
     }

     function getBarChartConfig (title, xAxis, yAxis, tooltipSuffix, data) {
         return {
             chart: {
                height: 650,
                type: 'bar'
            },
            title: {
                 text: title
             },
             subtitle: {
                text: $scope.model.startTime + ' - ' + $scope.model.endTime
             },
             xAxis: {
                 categories: xAxis,
                 title: {
                     text: null
                 }
             },
             yAxis: {
                 min: 0,
                 title: {
                     text: yAxis,
                     align: 'high'
                 },
                 labels: {
                     overflow: 'justify'
                 }
             },
             tooltip: {
                 valueSuffix: ' ' + tooltipSuffix
             },
             plotOptions: {
                 bar: {
                     dataLabels: {
                         enabled: true
                     }
                 }
             },
             legend: {
                layout: 'vertical',
                align: 'right',
                verticalAlign: 'top',
                x: -40,
                y: 100,
                floating: false,
                borderWidth: 1,
                backgroundColor: ((Highcharts.theme && Highcharts.theme.legendBackgroundColor) || '#FFFFFF'),
                shadow: true,
                reversed: true
             },
             series: data
         };
     }

     function getColumnChartConfig (title, xAxis, yAxis, tooltipSuffix, data, stack) {
         return {
             chart: {
                 height: 650,
                 type: 'column'
             },
             title: {
                 text: title
             },
             subtitle: {
                text: $scope.model.startTime + ' - ' + $scope.model.endTime
             },
             xAxis: {
                 categories: xAxis,
                 crosshair: true
             },
             yAxis: {
                 min: 0,
                 title: {
                     text: yAxis
                 }
             },
             tooltip: {
                 headerFormat: '<span style="font-size:10px">{point.key}</span><table>',
                 pointFormat: '<tr><td style="color:{series.color};padding:0">{series.name}: </td>' +
                     '<td style="padding:0"><b>{point.y:.1f} ' + tooltipSuffix + '</b></td></tr>',
                 footerFormat: '</table>',
                 shared: true,
                 useHTML: true
             },
             plotOptions: {
                 column: {
                     pointPadding: 0.2,
                     borderWidth: 0,
                     stacking: stack ? 'normal' : undefined
                 }
             },
             series: data
         };
     }

     $rootScope.$on('$stateChangeStart', function (event, toState, toParams, fromState, fromParams) {
         $scope.currentState = toState.name;
         $scope.model.viewTitle = toState.name;
         handleState(toState.name);
    });

    function clearLoading() {
        $scope.model.loading = false;
        $scope.model.loading2 = false;
        $scope.model.loading3 = false;
    }

    function clearAll() {
        $('#graph').empty();
        $('#graph2').empty();
        $('#graph3').empty();
        clearLoading();
    }

    var globalEpisode = 0;
    function handleState(name) {
        clearAll();
        var promises = [];
        var localEpisode = ++globalEpisode;
        switch (name)
        {
            case 'kpis':
                $scope.model.loading = true;
                promises = [
                    $http({
                        method: 'GET',
                        url: '/api/kpis?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime + '&aggregate=' + $scope.model.aggregate
                    })
                    .success(function(data) {
                        if (localEpisode !== globalEpisode) return;
                        var title = 'Try App Service KPIs Weekly Summary';
                        var xAxis = ['Users Visiting Page', 'Users Creating Apps', 'Users Initiating Free Trial'];
                        var yAxis = 'Number of Users';
                        var tooltip = 'Users';
                        var graphData = data.map(function(e) {
                            return {
                                name: e.startTime + ' - ' + e.endTime,
                                data: [ e.value.Visits, e.value.Logins, e.value.FreeTrialClicks ]
                            };
                        }).reverse();
                        $('#graph').highcharts(getBarChartConfig(title, xAxis, yAxis, tooltip, graphData));
                    })
                ];
            break;
            case 'appCreates':
                $scope.model.loading2 = true;
                $scope.model.loading3 = true;
                promises = [
                    $http({
                        method: 'GET',
                        url: '/api/appCreates?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime + '&aggregate=' + $scope.model.aggregate
                    })
                    .success(function(data) {
                        if (localEpisode !== globalEpisode) return;
                        $scope.model.loading2 = false;
                        var title = 'App Creation by App Type';
                        var xAxis = data.map(function(e) { return e.startTime + ' - ' + e.endTime; });
                        var yAxis = 'Number of Users';
                        var tooltip = 'Users';
                        var graphData = [{
                            name: 'Users Web Only',
                            data: data.map(function(e) { return e.value.WebApps; })
                        }, {
                            name: 'Users Mobile Only',
                            data: data.map(function(e) { return e.value.MobileApps; })
                        }, {
                            name: 'Users Mix Only',
                            data: data.map(function(e) { return e.value.Mix; })
                        }];

                        $('#graph2').highcharts(getColumnChartConfig(title, xAxis, yAxis, tooltip, graphData, /*stack*/ false));
                    }),
                    $http({
                        method: 'GET',
                        url: '/api/appCreatesFreeTrialClicks?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime + '&aggregate=' + $scope.model.aggregate
                    })
                    .success(function(data) {
                        if (localEpisode !== globalEpisode) return;
                        $scope.model.loading3 = false;
                        var title = 'Free Azure Subscription Initiation by App Type';
                        var xAxis = data.map(function(e) { return e.startTime + ' - ' + e.endTime; });
                        var yAxis = 'Number of Users';
                        var tooltip = 'Users';
                        var graphData = [{
                            name: 'Web Only',
                            data: data.map(function(e) { return e.value.WebApps; })
                        }, {
                            name: 'Mobile Only',
                            data: data.map(function(e) { return e.value.MobileApps; })
                        }, {
                            name: 'Mix Only',
                            data: data.map(function(e) { return e.value.Mix; })
                        }];

                        $('#graph3').highcharts(getColumnChartConfig(title, xAxis, yAxis, tooltip, graphData, /*stack*/ false));
                    })
                ];
            break;
            case 'accountTypes':
                $scope.model.loading2 = true;
                $scope.model.loading3 = true;
                promises = [
                    $http({
                        method: 'GET',
                        url: '/api/accountTypes?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime + '&aggregate=' + $scope.model.aggregate
                    })
                    .success(function(data) {
                        if (localEpisode !== globalEpisode) return;
                        $scope.model.loading2 = false;
                        var title = 'Try App Service Accounts';
                        var xAxis = data.map(function(e) { return e.startTime + ' - ' + e.endTime; });
                        var yAxis = 'Number of Users';
                        var tooltip = 'Users';
                        var graphData = [{
                            name: 'MSA',
                            data: data.map(function(e) { return e.value.MSA; }),
                            stack: 'Microsoft'
                        }, {
                            name: 'OrgId',
                            data: data.map(function(e) { return e.value.OrgId; }),
                            stack: 'Microsoft'
                        }, {
                            name: 'AAD',
                            data: data.map(function(e) { return e.value.AAD; }),
                            stack: 'Microsoft'
                        }, {
                            name: 'Google',
                            data: data.map(function(e) { return e.value.Google; }),
                            stack: 'Google'
                        }, {
                            name: 'Facebook',
                            data: data.map(function(e) { return e.value.Facebook; }),
                            stack: 'Facebook'
                        }];

                        $('#graph2').highcharts(getColumnChartConfig(title, xAxis, yAxis, tooltip, graphData, /*stack*/ true));
                    }),
                    $http({
                        method: 'GET',
                        url: '/api/accountTypesFreeTrialClicks?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime + '&aggregate=' + $scope.model.aggregate
                    })
                    .success(function(data) {
                        if (localEpisode !== globalEpisode) return;
                        $scope.model.loading3 = false;
                        var title = 'Free Azure Subscription Initiation by Login Type ';
                        var xAxis = data.map(function(e) { return e.startTime + ' - ' + e.endTime; });
                        var yAxis = 'Number of Users';
                        var tooltip = 'Users';
                        var graphData = [{
                            name: 'MSA',
                            data: data.map(function(e) { return e.value.MSA; }),
                            stack: 'Microsoft'
                        }, {
                            name: 'OrgId',
                            data: data.map(function(e) { return e.value.OrgId; }),
                            stack: 'Microsoft'
                        }, {
                            name: 'AAD',
                            data: data.map(function(e) { return e.value.AAD; }),
                            stack: 'Microsoft'
                        }, {
                            name: 'Google',
                            data: data.map(function(e) { return e.value.Google; }),
                            stack: 'Google'
                        }, {
                            name: 'Facebook',
                            data: data.map(function(e) { return e.value.Facebook; }),
                            stack: 'Facebook'
                        }, {
                            name: 'Anonymous',
                            data: data.map(function(e) { return e.value.Anonymous; }),
                            stack: 'Anonymous'
                        }];

                        $('#graph3').highcharts(getColumnChartConfig(title, xAxis, yAxis, tooltip, graphData, /*stack*/ true));
                    })
                ];
            break;
            case 'referrers':
                $scope.model.loading = true;
                promises = [
                    $http({
                        method: 'GET',
                        url: '/api/referrersCatagories?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime + '&aggregate=' + $scope.model.aggregate
                    })
                    .success(function(data) {
                        if (localEpisode !== globalEpisode) return;
                        $scope.model.loading = false;
                        var title = 'Referrers';
                        var xAxis = ['AppService', 'AzureDocumentation', 'AspNetDevelop', 'AzureSearch', 'Search', 'Ads', 'Uncategorized', 'Empty'];
                        var yAxis = 'Number of Users';
                        var tooltip = 'Users';
                        var graphData = [{
                            name: "Total Users",
                            data: [ data.Totals.AppService, data.Totals.AzureDocumentation, data.Totals.AspNetDevelop, data.Totals.AzureSearch, data.Totals.Search, data.Totals.Ads, data.Totals.Uncaterorized, data.Totals.Empty ]
                        }, {
                            name: "Creates Apps",
                            data: [ data.Created.AppService, data.Created.AzureDocumentation, data.Created.AspNetDevelop, data.Created.AzureSearch, data.Created.Search, data.Created.Ads, data.Created.Uncaterorized, data.Created.Empty ]
                        }, {
                            name: "Clicked on Free Trial",
                            data: [ data.FreeTrial.AppService, data.FreeTrial.AzureDocumentation, data.FreeTrial.AspNetDevelop, data.FreeTrial.AzureSearch, data.FreeTrial.Search, data.FreeTrial.Ads, data.FreeTrial.Uncaterorized, data.FreeTrial.Empty ]
                        }].reverse();
                        $('#graph').highcharts(getBarChartConfig(title, xAxis, yAxis, tooltip, graphData));
                    })
                ];
            break;
            case 'templates':
                $scope.model.loading = true;
                promises = [
                    $http({
                        method: 'GET',
                        url: '/api/templates?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime + '&aggregate=' + $scope.model.aggregate
                    })
                    .success(function(data) {
                        if (localEpisode !== globalEpisode) return;
                        $scope.model.loading = false;
                        var languages = data.reduce(function(previousValue, currentValue) {
                            if (previousValue[currentValue.Language]) {
                                previousValue[currentValue.Language] += currentValue.Count;
                            } else {
                                previousValue[currentValue.Language] = currentValue.Count;
                            }
                            return previousValue;
                        }, {});
                        var languagesData = [];
                        for (var e in languages) {
                            if (languages.hasOwnProperty(e)) {
                                languagesData.push({
                                    drilldown: e,
                                    name: e,
                                    y: languages[e]
                                });
                            }
                        }
                        var drilldownSeries = languagesData.map(function (l) {
                            return {
                                id: l.name,
                                name: l.name,
                                data: data.filter(function (e) {
                                    return e.Language === l.name;
                                }).map(function (d) {
                                    return [d.Name, d.Count];
                                })
                            };
                        });
                        $('#graph').highcharts({
                            chart: {
                                height: 650,
                                type: 'column'
                            },
                            title: {
                                text: 'Web Templates Breakdown'
                            },
                            subtitle: {
                                text: $scope.model.startTime + ' - ' + $scope.model.endTime
                            },
                            xAxis: {
                                type: 'category'
                            },
                            yAxis: {
                                title: {
                                    text: 'Number of installations'
                                }
                            },
                            legend: {
                                enabled: false
                            },
                            plotOptions: {
                                series: {
                                    borderWidth: 0,
                                    dataLabels: {
                                        enabled: true,
                                        format: '{point.y}'
                                    }
                                }
                            },
                            tooltip: {
                                headerFormat: '<span style="font-size:11px">{series.name}</span><br>',
                                pointFormat: '<span style="color:{point.color}">{point.name}</span>: <b>{point.y}</b> times<br/>'
                            },
                            series: [{
                                name: 'Languages',
                                colorByPoint: true,
                                data: languagesData
                            }],
                            drilldown: {
                                series: drilldownSeries
                            }
                        });
                    })
                ];
            break;
            case 'experiments':
                if ($scope.model.experiments) {
                    $scope.model.loading = true;
                    promises = [
                        $http.get('/api/experimentResult?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime + '&a=' + $scope.model.experimentA + '&b=' + $scope.model.experimentB)
                            .success(function (data) {
                                if (localEpisode !== globalEpisode) return;
                                var title = 'Try App Service A/B Experiments';
                                var xAxis = ['Total Number of Users', 'Users Creating Apps', 'Users Initiating Free Trial'];
                                var yAxis = 'Number of Users';
                                var tooltip = 'Users';
                                var graphData = data.map(function(e) {
                                    return {
                                        name: e.Name,
                                        data: [ e.TotalUsers, e.LoggedInUsers, e.FreeTrialUsers ]
                                    };
                                }).reverse();
                                $('#graph').highcharts(getBarChartConfig(title, xAxis, yAxis, tooltip, graphData));
                            })
                    ];
                } else {
                    $http.get('/api/sourceVariations?startTime=' + $scope.model.startTime)
                        .success(function(data) {
                            if (localEpisode !== globalEpisode) return;
                            $scope.model.experiments = data;
                            $scope.model.experimentA = data[0];
                            $scope.model.experimentB = data[0];
                        });
                }
            break;
            case 'sourceVariations':
                if ($scope.model.referrers) {
                    $scope.model.loading = true;
                    promises = [
                        $http.get('/api/sourceVariationResult?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime + '&referrer=' + $scope.model.selectedReferrer)
                            .success(function (data) {
                                if (localEpisode !== globalEpisode) return;
                                var title = 'Try App Service Source Variations';
                                var xAxis = ['Total Number of Users', 'Users Creating Apps', 'Users Initiating Free Trial'];
                                var yAxis = 'Number of Users';
                                var tooltip = 'Users';
                                var graphData = data.map(function(e) {
                                    return {
                                        name: e.Name,
                                        data: [ e.TotalUsers, e.LoggedInUsers, e.FreeTrialUsers ]
                                    };
                                }).reverse();
                                $('#graph').highcharts(getBarChartConfig(title, xAxis, yAxis, tooltip, graphData));
                            })
                    ];
                } else {
                    $http.get('/api/sourceVariations')
                        .success(function(data) {
                            if (localEpisode !== globalEpisode) return;
                            $scope.model.referrers = data;
                            $scope.model.selectedReferrer = data[0];
                        });
                }
            break;
            case 'userFeedback':
                $scope.model.loading = true;
                promises = [
                    $http({
                        method: 'GET',
                        url: '/api/userFeedback?startTime=' + $scope.model.startTime + '&endTime=' + $scope.model.endTime
                    })
                    .success(function(data) {
                        if (localEpisode !== globalEpisode) return;
                        $scope.model.loading = false;
                        $scope.model.userFeedback = data;
                    })
                ];
            break;

        }

        $q.all(promises).finally(function() {
            if (localEpisode !== globalEpisode) return;
            clearLoading();
        });
    }

    $scope.submit = function() {
        handleState($scope.currentState);
    };

    var mobileView = 992;

    $scope.getWidth = function() {
        return window.innerWidth;
    };

    $scope.$watch($scope.getWidth, function(newValue, oldValue) {
        if (newValue >= mobileView) {
            if (angular.isDefined($cookieStore.get('toggle'))) {
                $scope.toggle = ! $cookieStore.get('toggle') ? false : true;
            } else {
                $scope.toggle = true;
            }
        } else {
            $scope.toggle = false;
        }

    });

    $scope.toggleSidebar = function() {
        $scope.toggle = !$scope.toggle;
        $cookieStore.put('toggle', $scope.toggle);
    };

    window.onresize = function() {
        $scope.$apply();
    };
}
